from parallel_you.model.bus import Transport, Message

class MessageBroker:
    def __init__(self, transport: Transport):
        self._transport = transport

    async def start(self) -> None:
        await self._transport.start()

    async def stop(self) -> None:
        await self._transport.stop()

    async def emit(self, type: str, payload: dict, meta: dict | None = None) -> None:
        msg = Message(type=type, payload=payload, meta=meta or {})
        await self._transport.publish(msg)

    async def subscribe(self, pattern: str, handler) -> None:
        await self._transport.subscribe(pattern, handler)
