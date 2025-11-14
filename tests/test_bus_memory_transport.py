import asyncio

import pytest

from parallel_you.bus.transports import InMemoryTransport
from parallel_you.model.bus import Message


@pytest.mark.asyncio
async def test_inmemory_transport_delivers_to_exact_pattern():
    transport = InMemoryTransport()
    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await transport.subscribe("story.created", handler)
    await transport.start()

    msg = Message(type="story.created", payload={"id": "s1"})
    await transport.publish(msg)

    # Let the worker task process the queue
    await asyncio.sleep(0.01)

    await transport.stop()

    assert len(received) == 1
    assert received[0].type == "story.created"
    assert received[0].payload == {"id": "s1"}


@pytest.mark.asyncio
async def test_inmemory_transport_does_not_deliver_to_nonmatching_pattern():
    transport = InMemoryTransport()
    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await transport.subscribe("story.created", handler)
    await transport.start()

    msg = Message(type="story.deleted", payload={"id": "s1"})
    await transport.publish(msg)

    await asyncio.sleep(0.01)
    await transport.stop()

    assert received == []


@pytest.mark.asyncio
async def test_inmemory_transport_supports_wildcard_patterns():
    transport = InMemoryTransport()
    received: list[str] = []

    async def handler(msg: Message) -> None:
        received.append(msg.type)

    # Using shell-style wildcards, e.g. "story.*"
    await transport.subscribe("story.*", handler)
    await transport.start()

    await transport.publish(Message(type="story.created", payload={}))
    await transport.publish(Message(type="story.updated", payload={}))
    await transport.publish(Message(type="saga.started", payload={}))

    await asyncio.sleep(0.01)
    await transport.stop()

    assert "story.created" in received
    assert "story.updated" in received
    # Should not match saga.started
    assert "saga.started" not in received


@pytest.mark.asyncio
async def test_inmemory_transport_supports_multiple_handlers():
    transport = InMemoryTransport()
    calls_a: list[Message] = []
    calls_b: list[Message] = []

    async def handler_a(msg: Message) -> None:
        calls_a.append(msg)

    async def handler_b(msg: Message) -> None:
        calls_b.append(msg)

    await transport.subscribe("story.created", handler_a)
    await transport.subscribe("story.created", handler_b)
    await transport.start()

    msg = Message(type="story.created", payload={"id": "s1"})
    await transport.publish(msg)

    await asyncio.sleep(0.01)
    await transport.stop()

    assert len(calls_a) == 1
    assert len(calls_b) == 1
    assert calls_a[0] is calls_b[0]  # same object propagated
