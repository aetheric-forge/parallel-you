from __future__ import annotations
from pathlib import Path
from typing import Iterable, Optional
from .repo import ThreadRepo
from ..model import EnergyBand, ThreadState, Workspace, Thread
import yaml

def _load_workspace(path: Path) -> Workspace:
    if not path.exists():
        return Workspace()
    raw = yaml.safe_load(path.read_text()) or {}
    ws = Workspace(
        wip_limit=raw.get("wip_limit", 3),
        default_quantum=raw.get("default_quantum", "50m"),
        threads=[],
    )
    for t in (raw.get("threads") or []):
        ws.threads.append(
        Thread(
            id=t["id"],
            title=t.get("title", t["id"]),
            state=ThreadState(t.get("state", "Ready")),
            priority=int(t.get("priority", 3)),
            quantum=t.get("quantum", ws.default_quantum),
            energy_band=EnergyBand(t.get("energy_band", "Medium")),
            next_3=list(t.get("next_3", []) or []),
            tls=t.get("tls"),
            deps=list(t.get("deps", []) or []),
            blockers=list(t.get("blockers", []) or []),
        )
    )
    return ws

def _save_workspace(path: Path, ws: Workspace) -> None:
    data = {
        "wip_limit": ws.wip_limit,
        "default_quantum": ws.default_quantum,
        "threads": [
            {
                "id": t.id,
                "title": t.title,
                "state": t.state.value,
                "priority": t.priority,
                "quantum": t.quantum,
                "energy_band": t.energy_band.value,
                "next_3": t.next_3,
                "tls": t.tls,
                "deps": t.deps,
                "blockers": t.blockers,
            }
            for t in ws.threads
        ],
    }
    path.write_text(yaml.safe_dump(data, sort_keys=False))

class YamlThreadRepo(ThreadRepo):
    def __init__(self, path: Path):
        self.path = path
        self.ws = _load_workspace(self.path)

    def _save(self) -> None:
        _save_workspace(self.path, self.ws)

    def get(self, thread_id: str) -> Optional[Thread]:
        return self.ws.get(thread_id)

    def list(self, include_archived: bool = False) -> Iterable[Thread]:
        threads = self.ws.threads
        return threads if include_archived else [t for t in threads if not t.archived]

    def upsert(self, t: Thread) -> None:
        existing = self.ws.get(t.id)
        if existing:
            idx = next(i for i, x in enumerate(self.ws.threads) if x.id == t.id)
            self.ws.threads[idx] = t
        else:
            self.ws.threads.append(t)
        self._save()

    def archive(self, thread_id: str, archived: bool = True) -> None:
        t = self.ws.get(thread_id)
        if not t:
            return
        t.archived = archived
        self._save()

    def save_workspace(self, ws: Workspace) -> None:
        self.ws = ws
        self._save()

    def load_workspace(self) -> Workspace:
        # refresh from disk to pick up external edits
        self.ws = _load_workspace(self.path)
        return self.ws
