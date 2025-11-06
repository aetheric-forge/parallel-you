from __future__ import annotations
from datetime import datetime
from pathlib import Path
from .model import Workspace, ThreadState, Thread
from .storage.repo import ThreadRepo


class WipLimitError(RuntimeError):
    pass


class Scheduler:
    def __init__(self, repo: ThreadRepo):
        self.repo = repo
        self.ws: Workspace = repo.load_workspace()

    def save(self) -> None:
        self.repo.save_workspace(self.ws)

    def set_state(self, thread_id: str, state: ThreadState) -> Thread:
        t = self.repo.get(thread_id)
        if not t:
            raise KeyError(thread_id)
        if state == ThreadState.RUNNING and self.ws.running_count() >= self.ws.wip_limit:
            raise WipLimitError(f"WIP limit reached ({self.ws.running_count()}/{self.ws.wip_limit})")
        t.state = state
        self.repo.upsert(t)
        return t

    def start(self, thread_id: str) -> Thread:
        return self.set_state(thread_id, ThreadState.RUNNING)

    def yield_(self, thread_id: str, next_hint: str | None = None) -> Thread:
        t = self.repo.get(thread_id)
        if not t:
            raise KeyError(thread_id)
        if t.tls is None:
            t.tls = f"notes/{t.id}-context.md"
            # ensure it persists for next run
            self.repo.upsert(t)
        if next_hint and t.tls:
            hint = next_hint.strip()
            if hint:
                existing = [x for x in (t.next_3 or []) if x.strip().lower() != hint.lower()]
                t.next_3 = [hint] + existing[:2]
            ts = datetime.now().isoformat(timespec="seconds")
            p = Path(t.tls)
            p.parent.mkdir(parents=True, exist_ok=True)
            with p.open("a") as fh:
                fh.write(f"\n— yield @ {ts}\nnext_hint: {hint}\n")
        t.state = ThreadState.READY
        self.repo.upsert(t)
        return t
