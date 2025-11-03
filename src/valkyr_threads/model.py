from __future__ import annotations
from dataclasses import dataclass, field
from enum import Enum
from typing import List, Optional


class ThreadState(str, Enum):
    BACKLOG = "Backlog"
    READY = "Ready"
    RUNNING = "Running"
    BLOCKED = "Blocked"
    PARKED = "Parked"
    DONE = "Done"


class EnergyBand(str, Enum):
    DEEP = "Deep"
    MEDIUM = "Medium"
    LIGHT = "Light"


@dataclass
class Thread:
    id: str
    title: str
    state: ThreadState = ThreadState.READY
    priority: int = 3
    quantum: str = "50m" # human string; UI parses
    energy_band: EnergyBand = EnergyBand.MEDIUM
    next_3: List[str] = field(default_factory=list)
    tls: Optional[str] = None
    deps: List[str] = field(default_factory=list)
    blockers: List[str] = field(default_factory=list)


@dataclass
class Workspace:
    wip_limit: int = 3
    default_quantum: str = "50m"
    threads: List[Thread] = field(default_factory=list)

    def running_count(self) -> int:
        return sum(1 for t in self.threads if t.state == ThreadState.RUNNING)


    def get(self, thread_id: str) -> Optional[Thread]:
        return next((t for t in self.threads if t.id == thread_id), None)
