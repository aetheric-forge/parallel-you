from __future__ import annotations
from dataclasses import dataclass
from typing import Iterable, List, Optional, Sequence
from .model import Thread, ThreadState, EnergyBand

_ENERGY_ORDER = {EnergyBand.LIGHT: 0, EnergyBand.MEDIUM: 1, EnergyBand.DEEP: 2}
_STATE_ORDER = {"Running": 0, "Ready": 1, "Parked": 2, "Done": 3}

@dataclass
class FilterSpec:
    states: Optional[Sequence[ThreadState]] = None
    min_priority: Optional[int] = None
    max_priority: Optional[int] = None
    energy: Optional[Sequence[EnergyBand]] = None
    text: Optional[str] = None
    include_archived: bool = False

def apply_filters(items: Iterable[Thread], spec: FilterSpec) -> List[Thread]:
    q = (spec.text or "").strip().lower()
    out: List[Thread] = []
    for t in items:
        if not spec.include_archived and t.archived:
            continue
        if spec.states and t.state not in spec.states:
            continue
        if spec.energy and t.energy_band not in spec.energy:
            continue
        if spec.min_priority is not None and t.priority < spec.min_priority:
            continue
        if spec.max_priority is not None and t.priority > spec.max_priority:
            continue
        if q and (q not in t.title.lower()) and all(q not in s.lower() for s in (t.next_3 or [])):
            continue
        out.append(t)
    return out

def sort_threads(items: Iterable[Thread], reverse: bool = False) -> List[Thread]:
    def _key(t: Thread):
        return (
            _STATE_ORDER.get(t.state, 99),
            t.priority,
            _ENERGY_ORDER.get(t.energy_band, 99),
            t.created_at,
        )
    return sorted(list(items), key=_key, reverse=reverse)
