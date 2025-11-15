import asyncio

import pytest

from parallel_you.bus.transports import InMemoryTransport
from parallel_you.model.bus import Message


@pytest.mark.asyncio
async def test_transport_delivers_to_exact_pattern(broker):
    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await broker.subscribe("story.created", handler)

    msg = Message(type="story.created", payload={"id": "s1"})
    await broker.emit(msg)

    # Let the worker task process the queue
    await asyncio.sleep(0.01)

    await broker.stop()

    assert len(received) == 1
    assert received[0].type == "story.created"
    assert received[0].payload == {"id": "s1"}


@pytest.mark.asyncio
async def test_transport_does_not_deliver_to_nonmatching_pattern(broker):
    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await broker.subscribe("story.created", handler)
    await broker.start()

    msg = Message(type="story.deleted", payload={"id": "s1"})
    await broker.emit(msg)

    await asyncio.sleep(0.01)
    await broker.stop()

    assert received == []


@pytest.mark.asyncio
async def test_transport_supports_wildcard_patterns(broker):
    received: list[str] = []

    async def handler(msg: Message) -> None:
        received.append(msg.type)

    # Using shell-style wildcards, e.g. "story.*"
    await broker.subscribe("story.*", handler)

    await broker.emit(Message(type="story.created", payload={}))
    await broker.emit(Message(type="story.updated", payload={}))
    await broker.emit(Message(type="saga.started", payload={}))

    await asyncio.sleep(0.01)
    await broker.stop()

    assert "story.created" in received
    assert "story.updated" in received
    # Should not match saga.started
    assert "saga.started" not in received


@pytest.mark.asyncio
async def test_transport_supports_multiple_handlers(broker):
    calls_a: list[Message] = []
    calls_b: list[Message] = []

    async def handler_a(msg: Message) -> None:
        calls_a.append(msg)

    async def handler_b(msg: Message) -> None:
        calls_b.append(msg)

    await broker.subscribe("story.created", handler_a)
    await broker.subscribe("story.created", handler_b)

    msg = Message(type="story.created", payload={"id": "s1"})
    await broker.emit(msg)

    await asyncio.sleep(0.01)
    await broker.stop()

    assert len(calls_a) == 1
    assert len(calls_b) == 1
    assert calls_a[0] is calls_b[0]  # same object propagated
