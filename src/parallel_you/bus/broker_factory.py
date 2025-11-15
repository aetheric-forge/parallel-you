import os

import pytest

from .message_broker import MessageBroker
from .transports import RabbitMQTransport, InMemoryTransport

RABBITMQ_ENV_VAR="RABBITMQ_URL"

def _rabbitmq_url() -> str:
    url = os.getenv(RABBITMQ_ENV_VAR, None)
    if url is None:
        raise RuntimeError(f"Cannot find RabbitMQ URL in environment {RABBITMQ_ENV_VAR}")
    return url

async def make_broker(kind: str = "memory"):
    if kind == "memory":
        transport = InMemoryTransport()
    elif kind == "rabbitmq":
        transport = RabbitMQTransport(url=_rabbitmq_url())
    try:
        await transport.start()
    except Exception as ex:
        raise RuntimeError(f"Connection to message broker failed: ", ex)

    return MessageBroker(transport)
