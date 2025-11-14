from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Mapping
from uuid import UUID, uuid4

def _ts():
    return datetime.now(timezone.utc)

@dataclass(frozen=True)
class Message:
    id: UUID = field(default_factory=uuid4)
    type: str = ""             # "saga.started", "story.updated", etc.
    payload: Mapping[str, Any] = field(default_factory=dict)
    meta: Mapping[str, Any] = field(default_factory=dict)
    timestamp: datetime = field(default_factory=_ts)
    causation_id: UUID | None = None
    correlation_id: UUID | None = None
