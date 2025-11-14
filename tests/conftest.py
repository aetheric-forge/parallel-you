import os
import pytest
from parallel_you.storage import make_repo

def _has_mongo():
    return bool(os.getenv("MONGO_URI"))

@pytest.fixture(params=[
    "memory",
    pytest.param("mongo", marks=pytest.mark.skipif(not _has_mongo(), reason="MONGO_URI not set")),
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
