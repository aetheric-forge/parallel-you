from __future__ import annotations
from pathlib import Path
from typing import Iterable, Optional
from .repo import ThreadRepo
from ..model import Workspace, Thread
from .workspace import load_workspace, save_workspace  # your existing functions

class YamlThreadRepo(ThreadRepo):
    def __init__(self, path: Path):
        self.path = path
        self.ws = load_workspace(self.path)

    def _save(self) -> None:
        save_workspace(self.path, self.ws)

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
        self.ws = load_workspace(self.path)
        return self.ws
