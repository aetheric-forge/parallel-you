import re
from datetime import datetime
from pymongo import MongoClient, ReturnDocument

from parallel_you.model import Story, Saga, Thread
from parallel_you.storage import Repo
from filter_spec import FilterSpec

class MongoRepo(Repo):
    def __init__(self, uri: str = "mongodb://127.0.0.1:27017/parallel_you"):
        if not uri:
            raise ValueError("Mongo URI must be provided")
        client = MongoClient(uri)
        db = client.get_default_database()
        if db is None:
            raise ValueError("Mongo URI must include a database name, e.g. .../parallel_you")
        self._c = db.get_collection("threads")
        # Helpful indexes
        self._c.create_index("title")
        self._c.create_index("archived")
        self._c.create_index("kind")

    # ---------- mapping ----------
    def _to_doc(self, t: Thread) -> dict:
        # Persist a discriminator for queries
        if isinstance(t, Saga):
            kind = "saga"
            extra = {"stories": []}  # or omit if not embedding
        elif isinstance(t, Story):
            kind = "story"
            extra = {
                "saga_id": t.saga_id,
                "state": t.state.name,
                "energy_band": t.energy.name,
                "quantum_overridden": getattr(t, "quantum_overridden", False),
            }
        else:
            kind = "thread"
            extra = {}

        return {
            "_id": t.id,                      # string id
            "kind": kind,
            "title": t.title,
            "priority": t.priority,
            "quantum": t.quantum,
            "archived": t.archived,
            "created_at": t.created_at,
            "updated_at": t.updated_at,
            "archived_at": t.archived_at,
            **extra,
        }

    def _from_doc(self, d: dict) -> Thread:
        kind = d.get("kind", "thread")
        base = dict(
            id=d.get("_id"),
            title=d["title"],
            priority=int(d.get("priority", 1)),
            quantum=d.get("quantum", "50m"),
            archived=bool(d.get("archived", False)),
            created_at=d.get("created_at"),
            updated_at=d.get("updated_at"),
            archived_at=d.get("archived_at"),
        )
        if kind == "saga":
            return Saga(**base, stories=[])
        if kind == "story":
            # adapt state/energy parsing if you store names vs values
            from parallel_you.model.story_state import StoryState
            from parallel_you.model.energy_band import EnergyBand
            state = d.get("state")
            energy = d.get("energy_band")
            return Story(
                saga_id=d["saga_id"],
                state=StoryState[state] if isinstance(state, str) else state,
                energy=EnergyBand[energy] if isinstance(energy, str) else energy,
                **base,
            )
        return Thread(**base)

    # ---------- repo API ----------
    def list(self, spec: FilterSpec) -> list[Thread]:
        q: dict = {}
        if spec.archived is not None:
            q["archived"] = spec.archived
        if spec.text:
            q["title"] = {"$regex": re.escape(spec.text), "$options": "i"}
        if spec.types:
            q["kind"] = {"$in": [cls.__name__.lower() for cls in spec.types]}
        return [self._from_doc(d) for d in self._c.find(q)]

    def get(self, id: str) -> Thread | None:
        d = self._c.find_one({"_id": id})
        return self._from_doc(d) if d else None

    def upsert(self, t: Thread) -> str:
        # Read existing to preserve created_at and handle archived_at transitions
        existing = self._c.find_one({"_id": t.id})
        now = datetime.now().replace(second=0, microsecond=0)

        created_at = (existing or {}).get("created_at") or getattr(t, "created_at", None) or now

        prev_arch = bool((existing or {}).get("archived", False))
        new_arch = bool(getattr(t, "archived", prev_arch))
        if not prev_arch and new_arch:
            archived_at = now
        elif prev_arch and not new_arch:
            archived_at = None
        else:
            archived_at = (existing or {}).get("archived_at", getattr(t, "archived_at", None))

        # Build doc snapshot
        doc = self._to_doc(t)
        doc["created_at"] = created_at
        doc["updated_at"] = now
        doc["archived_at"] = archived_at

        self._c.replace_one({"_id": t.id}, doc, upsert=True)
        return t.id

    def delete(self, id: str) -> bool:
        return self._c.delete_one({"_id": id}).deleted_count == 1

    def clear(self) -> None:
        self._c.delete_many({})

