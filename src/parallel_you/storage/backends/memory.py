from parallel_you.storage import Repo
from parallel_you.model.threads import Story, Saga, Thread 
from filter_spec import FilterSpec
from util import gen_id
from datetime import datetime
import re
import copy

class MemoryRepo(Repo):
    _threads: dict[str, Thread]

    def __init__(self):
        self._threads = {}

    def list(self, spec: FilterSpec) -> list[Thread]:
        result = []
        for v in self._threads.values():
            if spec.text and not re.search(spec.text, v.title, flags=re.IGNORECASE):
                continue
            if spec.archived is not None and v.archived != spec.archived:
                continue
            if spec.types and not isinstance(v, tuple(spec.types)):
                continue
            result.append(copy.deepcopy(v))
        return result

    def get(self, id: str) -> Thread | None:
        obj = self._threads.get(id, None)
        return copy.deepcopy(obj) if obj else None

    def upsert(self, t: Thread) -> str:
        incoming = copy.deepcopy(t)

        if not getattr(incoming, "id", None):
            incoming.id = gen_id()

        now = datetime.now().replace(second=0, microsecond=0)
        existing = self._threads.get(incoming.id)

        # choose target object to mutate
        if existing is None:
            stored = copy.deepcopy(incoming)
            # first write: set created_at if missing
            stored.created_at = incoming.created_at or now
        else:
            stored = copy.deepcopy(existing)

            # copy mutable fields from incoming (do NOT copy archived_at here)
            stored.title = incoming.title
            stored.priority = incoming.priority
            stored.quantum = incoming.quantum
            stored.archived = incoming.archived

        prev_arch = bool(getattr(existing, "archived", False)) if existing else False
        new_arch = bool(stored.archived)

        # archived_at transitions
        if not prev_arch and new_arch:
            stored.archived_at = now           # just archived
        elif prev_arch and not new_arch:
            stored.archived_at = None          # just unarchived
        # else: leave archived_at as-is

        # timestamps
        stored.updated_at = now

        self._threads[t.id] = stored
        return stored.id

    def delete(self, id: str) -> bool:
        return self._threads.pop(id, None) is not None
       
    def clear(self) -> None:
        self._threads.clear()
