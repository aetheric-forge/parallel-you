import asyncio
from typing import Any

import pytest

from parallel_you.bus import MessageBroker
from parallel_you.bus.transports import InMemoryTransport
from parallel_you.model.bus import Transport, Handler, Message


class FakeTransport(Transport):
    def __init__(self) -> None:
        self.published: list[Message] = []
        self.subscriptions: list[tuple[str, Handler]] = []
        self.started = False
        self.stopped = False

    async def publish(self, msg: Message) -> None:
        self.published.append(msg)

    async def subscribe(self, pattern: str, handler: Handler) -> None:
        self.subscriptions.append((pattern, handler))

    async def start(self) -> None:
        self.started = True

    async def stop(self) -> None:
        self.stopped = True


@pytest.mark.asyncio
async def test_broker_start_and_stop_delegate_to_transport():
    t = FakeTransport()
    broker = MessageBroker(t)

    await broker.start()
    await broker.stop()

    assert t.started is True
    assert t.stopped is True


@pytest.mark.asyncio
async def test_broker_emit_constructs_message_and_publishes():
    t = FakeTransport()
    broker = MessageBroker(t)

    await broker.emit(
        type="story.created",
        payload={"id": "s1"},
        meta={"actor_id": "a1"},
    )

    assert len(t.published) == 1
    msg = t.published[0]

    assert isinstance(msg, Message)
    assert msg.type == "story.created"
    assert msg.payload == {"id": "s1"}
    assert msg.meta == {"actor_id": "a1"}


@pytest.mark.asyncio
async def test_broker_subscribe_delegates_to_transport():
    t = FakeTransport()
    broker = MessageBroker(t)

    async def handler(msg: Message) -> None:
        ...

    await broker.subscribe("story.*", handler)

    assert len(t.subscriptions) == 1
    pattern, registered = t.subscriptions[0]
    assert pattern == "story.*"
    assert registered is handler

@pytest.mark.asyncio
async def test_broker_with_inmemory_transport_end_to_end():

    transport = InMemoryTransport()
    broker = MessageBroker(transport)

    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await broker.subscribe("saga.started", handler)
    await broker.start()

    await broker.emit("saga.started", {"saga_id": "s1"}, {"actor_id": "a1"})

    await asyncio.sleep(0.01)
    await broker.stop()

    assert len(received) == 1
    msg = received[0]
    assert msg.type == "saga.started"
    assert msg.payload == {"saga_id": "s1"}
    assert msg.meta == {"actor_id": "a1"}
