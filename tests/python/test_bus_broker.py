import asyncio
from typing import Any

import pytest

from parallel_you.bus import MessageBroker
from parallel_you.bus.transports import InMemoryTransport
from parallel_you.model.bus import Transport, Handler, Message



@pytest.mark.asyncio
async def test_broker_start_and_stop_delegate_to_transport(broker):
    await broker.start()
    assert broker.started is True
    await broker.stop()
    assert broker.stopped is True


@pytest.mark.asyncio
async def test_broker_subscribe_delegates_to_transport(broker):
    async def handler(msg: Message) -> None:
        ...

    await broker.subscribe("story.*", handler)

    t = broker.transport
    assert "story.*" in t.subscriptions
    assert len(t.subscriptions["story.*"]) == 1

@pytest.mark.asyncio
async def test_broker_with_transport_end_to_end(broker):

    received: list[Message] = []

    async def handler(msg: Message) -> None:
        received.append(msg)

    await broker.subscribe("saga.started", handler)
    await broker.start()

    await broker.emit(Message(type="saga.started", payload={"saga_id": "s1"}, meta={"actor_id": "a1"}))

    await asyncio.sleep(0.01)
    await broker.stop()

    assert len(received) == 1
    msg = received[0]
    assert msg.type == "saga.started"
    assert msg.payload == {"saga_id": "s1"}
    assert msg.meta == {"actor_id": "a1"}
