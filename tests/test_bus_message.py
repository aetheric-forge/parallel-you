import re
from datetime import datetime, timedelta, timezone
from uuid import UUID

from parallel_you.model.bus import Message


def test_message_defaults_are_populated():
    msg = Message(type="test.event", payload={"foo": "bar"})

    # id is a UUID
    assert isinstance(msg.id, UUID)

    # type and payload are preserved
    assert msg.type == "test.event"
    assert msg.payload == {"foo": "bar"}

    # meta defaults to empty mapping
    assert isinstance(msg.meta, dict)
    assert msg.meta == {}

    # timestamp is "recent"
    now = datetime.now(timezone.utc)
    # allow a small skew in either direction
    assert now - timedelta(seconds=5) <= msg.timestamp <= now + timedelta(seconds=5)

    # correlation/causation default to None
    assert msg.causation_id is None
    assert msg.correlation_id is None


def test_message_explicit_meta_and_correlation():
    cid = UUID(int=123)
    msg = Message(
        type="saga.started",
        payload={"saga_id": "abc"},
        meta={"actor_id": "xyz"},
        correlation_id=cid,
    )

    assert msg.meta == {"actor_id": "xyz"}
    assert msg.correlation_id == cid
    assert msg.causation_id is None


def test_message_repr_contains_type_and_id():
    msg = Message(type="story.updated", payload={})
    text = repr(msg)
    # Very loose sanity checks; repr implementation can vary
    assert "story.updated" in text
    # UUID-ish substring
    assert re.search(r"[0-9a-fA-F-]{8,}", text)
