from parallel_you.model.bus import Transport, Message

class MessageBroker:
    def __init__(self, transport: Transport):
        self._started = False
        self._stopped = True
        self._transport = transport

    async def start(self) -> None:
        await self._transport.start()
        self._started = True
        self._stopped = False

    async def stop(self) -> None:
        await self._transport.stop()
        self._stopped = True
        self._started = False

    async def emit(self, msg: Message) -> None:
        await self._transport.publish(msg)

    async def subscribe(self, pattern: str, handler) -> None:
        await self._transport.subscribe(pattern, handler)

    @property
    def started(self) -> bool:
        return self._started

    @property
    def stopped(self) -> bool:
        return self._stopped

    @property
    def transport(self) -> Transport:
        return self._transport
