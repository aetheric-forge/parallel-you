from __future__ import annotations
from datetime import datetime
from pathlib import Path
from .model import Workspace, ThreadState, Thread
from .storage import load_workspace, save_workspace


class WipLimitError(RuntimeError):
    pass


class Scheduler:
    def __init__(self, yaml_path: Path):
        self.path = yaml_path
        self.ws: Workspace = load_workspace(self.path)

    def save(self) -> None:
        save_workspace(self.path, self.ws)

    def set_state(self, thread_id: str, state: ThreadState) -> Thread:
        t = self.ws.get(thread_id)
        if not t:
            raise KeyError(thread_id)
        if state == ThreadState.RUNNING and self.ws.running_count() >= self.ws.wip_limit:
            raise WipLimitError(f"WIP limit reached ({self.ws.running_count()}/{self.ws.wip_limit})")
        t.state = state
        self.save()
        return t

    def start(self, thread_id: str) -> Thread:
        return self.set_state(thread_id, ThreadState.RUNNING)

    def yield_(self, thread_id: str, next_hint: str | None = None) -> Thread:
        t = self.ws.get(thread_id)
        if not t:
            raise KeyError(thread_id)
        if t.tls is None:
            t.tls = f"notes/{t.id}-context.md"
            # ensure it persists for next run
            self.save()       
        if next_hint and t.tls:
            hint = next_hint.strip()
            if hint:
                existing = [x for x in (t.next_3 or []) if x.strip().lower() != hint.lower()]
                t.next_3 = [hint] + existing[:2]
            ts = datetime.now().isoformat(timespec="seconds")
            tls: str = t.tls
            p = Path(tls)
            p.parent.mkdir(parents=True, exist_ok=True)
            with p.open("a") as fh:
                fh.write(f"\n— yield @ {ts}\nnext_hint: {hint}\n")
        t.state = ThreadState.READY
        self.save()
        return t
