from asyncio import Task, create_task, CancelledError
import json
from collections import defaultdict
from fnmatch import fnmatch
from typing import Any

import aio_pika
from aio_pika import ExchangeType, RobustConnection
from aio_pika.abc import AbstractChannel, AbstractExchange, AbstractQueue

from parallel_you.model.bus import Transport, Handler, Message

class RabbitMQTransport(Transport):
    """
    RabbitMQ-backed implementation of the message bus Transport.

    Semantics are intentionally close to InMemoryTransport:
    - publish() enqueues messages based on msg.type as the routing key
    - subscribe() registers handlers keyed by fnmatch pattern
    - start() / stop() control a single background consumer task
    """

    def __init__(
        self,
        url: str,
        exchange_name: str = "parallel_you.bus",
        queue_name: str | None = None,
        prefetch_count: int = 50,
    ) -> None:
        self._started = False
        self._stopped = True
        self._url = url
        self._exchange_name = exchange_name
        self._queue_name = queue_name
        self._prefetch_count = prefetch_count

        self._subs: dict[str, list[Handler]] = defaultdict(list)

        self._connection: RobustConnection | None = None
        self._channel: AbstractChannel | None = None
        self._exchange: AbstractExchange | None = None
        self._queue: AbstractQueue | None = None

        self._task: Task | None = None

    # ---
    # Transport Interface
    # ---
    async def publish(self, msg: Message) -> None:
        """
        Publish a Message to the topic exchange.

        Routing key is msg.type, e.g. "saga.created".
        """
        if self._exchange is None:
            raise RuntimeError("RabbitMQTransport not started (exchange is None)")
        
        payload = {
            "type": msg.type,
            "payload": msg.payload,
            "meta": getattr(msg, "meta", {}) or {},
        }
        body = json.dumps(payload).encode("utf-8")

        message = aio_pika.Message(
            body=body,
            content_type="application/json",
            delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
        )

        await self._exchange.publish(message, routing_key=msg.type)

    async def subscribe(self, pattern: str, handler: Handler) -> None:
        """Register a handler for messages whose type matches pattern."""
        self._subs[pattern].append(handler)

    async def start(self) -> None:
        """Connect to RabbitMQ, declare exchange/queue, and start consumer."""
        if self._task is not None:
            # already running
            return
        
        # Robust connection handles reconnects
        self._connection = await aio_pika.connect_robust(self._url)
        self._channel = await self._connection.channel()
        await self._channel.set_qos(prefetch_count=self._prefetch_count)

        # Topic exchange for message types like "saga.created"
        self._exchange = await self._channel.declare_exchange(
            self._exchange_name,
            ExchangeType.TOPIC,
            durable=True,
        )

        # One queue per process; receives *all* messages via "#"
        # We still use fnmatch to fan out to handlers, like InMemoryTransport
        queue_name = self._queue_name or ""
        self._queue = await self._channel.declare_queue(
            name=queue_name,
            durable=False,
            exclusive=self._queue_name is None,
            auto_delete=self._queue_name is None,
        )

        await self._queue.bind(self._exchange, routing_key="#")

        self._task = create_task(self._worker(), name="rabbitmq-transport-worker")

        self._started = True
        self._stopped = False

    async def stop(self) -> None:
        """Stop consumer and close connection."""
        if self._task:
            self._task.cancel()
            try:
                await self._task
            except CancelledError:
                pass
            self._task = None
        
        # Close channel / connection
        if self._channel and not self._channel.is_closed:
            await self._channel.close()
        self._channel = None

        if self._connection and not self._connection.is_closed:
            await self._connection.close()
        self._connection = None
        self._exchange = None
        self._queue = None
        self._stopped = True
        self._started = True

    # ---
    # Internal worker
    # ---
    async def _worker(self) -> None:
        if self._queue is None:
            raise RuntimeError("RabbitMQTransport.start() must be called before worker")
        
        async with self._queue.iterator() as queue_iter:
            async for rmq_message in queue_iter:
                async with rmq_message.process():
                    try:
                        data = json.loads(rmq_message.body.decode("utf-8"))
                    except Exception:
                        # If something goes badly wrong with decoding, just nack
                        rmq_message.reject(requeue=False)
                        continue

                    msg = self._to_message(data)

                    # fan-out to all matching handlers
                    handlers = self._matching_handlers(msg.type)
                    for h in handlers:
                        # Keep handlers from kill the consumer loop
                        try:
                            await h(msg)
                        except Exception:
                            # TODO: logging hook here
                            # e.g. logger.exception("Handler failed for %s", msg.type)
                            continue

    def _matching_handlers(self, msg_type: str) -> list[Handler]:
        matches: list[Handler] = []
        for pattern, handlers in self._subs.items():
            if fnmatch(msg_type, pattern):
                matches.extend(handlers)
        return matches

    def _to_message(self, data: dict[str, Any]) -> Message:
        # Adjust if your Message has a different constructor
        return Message(
            type=data["type"],
            payload=data.get("payload", {}),
            meta=data.get("meta", {})
        )

    @property
    def started(self) -> bool:
        return self._started

    @property
    def stopped(self) -> bool:
        return self._stopped

    @property
    def subscriptions(self) -> dict[str, list[Handler]]:
        return self._subs

    