# src/valkyr_threads/repo_mongo.py
from __future__ import annotations
from datetime import datetime
from typing import Iterable, Optional
from pymongo import MongoClient, ReturnDocument
from ..model import Workspace, Thread, ThreadState, EnergyBand

def _to_doc(t: Thread) -> dict:
    return {
        "_id": t.id,
        "title": t.title,
        "state": t.state.value,
        "priority": t.priority,
        "quantum": t.quantum,
        "energy_band": t.energy_band.value,
        "next_3": t.next_3,
        "tls": t.tls,
        "deps": t.deps,
        "blockers": t.blockers,
        "archived": t.archived,
        "created_at": t.created_at,
        "updated_at": t.updated_at,
    }

def _from_doc(d: dict) -> Thread:
    return Thread(
        id=d["_id"],
        title=d["title"],
        state=ThreadState(d["state"]),
        priority=int(d["priority"]),
        quantum=d["quantum"],
        energy_band=EnergyBand(d["energy_band"]),
        next_3=list(d.get("next_3") or []),
        tls=d.get("tls"),
        deps=list(d.get("deps") or []),
        blockers=list(d.get("blockers") or []),
        created_at=d["created_at"],
        updated_at=d["updated_at"],
        archived=bool(d.get("archived", False)),
    )

class MongoThreadRepo:
    def __init__(self, uri: str, db_name: str):
        self.db = MongoClient(uri)[db_name]
        self.c_threads = self.db["threads"]
        self.c_ws = self.db["workspace"]

    def get(self, thread_id: str):
        d = self.c_threads.find_one({"_id": thread_id})
        return _from_doc(d) if d else None

    def list(self, include_archived: bool = False):
        q = {} if include_archived else {"archived": False}
        for d in self.c_threads.find(q):
            yield _from_doc(d)

    def upsert(self, t: Thread) -> None:
        doc = _to_doc(t)
        # ensure updated_at is always fresh; preserve created_at on updates
        doc["updated_at"] = datetime.now().replace(microsecond=0)
        update = {"$set": doc}
        self.c_threads.update_one(
            {"_id": t.id},
            update,
            upsert=True,
        )

    def archive(self, thread_id: str, archived: bool = True) -> None:
        self.c_threads.update_one(
            {"_id": thread_id},
            {"$set": {"archived": archived, "updated_at": datetime.now().replace(microsecond=0)}}
        )

    def save_workspace(self, ws: Workspace) -> None:
        self.c_ws.update_one(
            {"_id": "default"},
            {"$set": {
                "wip_limit": ws.wip_limit,
                "default_quantum": ws.default_quantum,
                "updated_at": datetime.now().replace(microsecond=0)
            }},
            upsert=True,
        )

    def load_workspace(self) -> Workspace:
        d = self.c_ws.find_one({"_id": "default"}) or {}
        ws =  Workspace(
            wip_limit=int(d.get("wip_limit", 3)),
            default_quantum=d.get("default_quantum", "50m"),
        )
        ws.threads = [ _from_doc(doc) for doc in self.c_threads.find({}) ]
        return ws
