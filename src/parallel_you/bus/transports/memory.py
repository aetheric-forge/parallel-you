import asyncio
from collections import defaultdict
from fnmatch import fnmatch

from parallel_you.model.bus import Transport, Handler, Message

class InMemoryTransport(Transport):
    def __init__(self) -> None:
        self._subs: dict[str, list[Handler]] = defaultdict(list)
        self._queue: "asyncio.Queue[Message]" = asyncio.Queue()
        self._task: asyncio.Task | None = None

    async def publish(self, msg: Message) -> None:
        await self._queue.put(msg)

    async def subscribe(self, pattern: str, handler: Handler) -> None:
        self._subs[pattern].append(handler)

    async def _worker(self) -> None:
        while True:
            msg = await self._queue.get()
            for pattern, handlers in self._subs.items():
                if fnmatch(msg.type, pattern):
                    for h in handlers:
                        await h(msg)

    async def start(self) -> None:
        if self._task is None:
            self._task = asyncio.create_task(self._worker())

    async def stop(self) -> None:
        if self._task:
            self._task.cancel()
            self._task = None
