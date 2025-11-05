from __future__ import annotations
from ..model import Workspace, Thread, ThreadState, EnergyBand
from pathlib import Path
import yaml


def load_workspace(path: Path) -> Workspace:
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




def save_workspace(path: Path, ws: Workspace) -> None:
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
