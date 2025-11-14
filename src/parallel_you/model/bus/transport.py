from abc import ABC, abstractmethod
from collections.abc import Awaitable, Callable
from typing import Protocol

from .message import Message

Handler = Callable[[Message], Awaitable[None]]  # or sync version if your core is sync

class Transport(ABC):
    @abstractmethod
    async def publish(self, msg: Message) -> None: ...
    @abstractmethod
    async def subscribe(self, pattern: str, handler: Handler) -> None: ...
    @abstractmethod
    async def start(self) -> None: ...
    @abstractmethod
    async def stop(self) -> None: ...
