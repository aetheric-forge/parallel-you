import os
import pytest
import pytest_asyncio

from parallel_you.storage import make_repo
from parallel_you.bus import MessageBroker, make_broker
from parallel_you.bus.transports import RabbitMQTransport

MONGO_ENV_VAR = "MONGO_URI"
RABBITMQ_ENV_VAR = "RABBITMQ_URL"

def _has_mongo():
    return bool(os.getenv(MONGO_ENV_VAR))

def _has_rabbitmq():
    return bool(os.getenv(RABBITMQ_ENV_VAR))

@pytest.fixture(params=[
    "memory",
    pytest.param("mongo", marks=pytest.mark.skipif(not _has_mongo(), reason="{MONGO_ENV_VAR} not set")),
])
def repo(request):
    r = make_repo(kind=request.param)
    yield r
    # best-effort cleanup for persistent backends
    if request.param == "mongo":
        try:
            # adjust if your repo exposes client/db differently
            r.clear()
        except Exception:
            pass

@pytest.fixture(scope="session")
def rabbitmq_url() -> str:
    url = os.getenv(RABBITMQ_ENV_VAR)
    if not url:
        pytest.skip(f"{RABBITMQ_ENV_VAR}")

@pytest.fixture
async def rabbitmq_transport(rmq_url: str) -> RabbitMQTransport:
    """RabbitMQ-backed transport, started/stopped per test."""
    transport = RabbitMQTransport(url=rmq_url)
    await transport.start()
    try:
        yield transport
    finally:
        await transport.stop()

@pytest.fixture
async def rabbitmq_broker(rabbitmq_transport) -> MessageBroker:
    """MessageBroker wired to RabbitMQ transport."""
    broker = MessageBroker(rabbitmq_transport)
    return broker

@pytest_asyncio.fixture(params=[
    "memory",
    pytest.param("rabbitmq", marks=pytest.mark.skipif(not _has_rabbitmq(), reason="{RABBITMQ_ENV_VAR} not set")),
])
async def broker(request):
    kind = getattr(request, "param", "memory") 
    return await make_broker(kind=kind)
