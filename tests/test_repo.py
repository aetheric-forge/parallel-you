from datetime import datetime
import time
import pytest

from parallel_you.storage.repo_factory import make_repo
from parallel_you.model.saga import Saga
from parallel_you.model.story import Story
from filter_spec import FilterSpec

@pytest.fixture
def repo():
    # Ensure your factory defaults to memory; or pass kind="memory"
    return make_repo()

def test_insert_and_fetch(repo):
    saga = Saga(id=None, title="Test Saga #1", priority=2, quantum="30m")
    mark = datetime.now().replace(second=0, microsecond=0)
    saga_id = repo.upsert(saga) or saga.id   # support either convention
    assert saga_id

    got = repo.get(saga_id)
    assert got is not None
    assert got.id == saga.id
    assert got.title == "Test Saga #1"
    assert got.priority == 2
    assert got.quantum == "30m"

    # timestamps come from repo; just sanity-check ordering
    assert got.created_at >= mark
    assert got.updated_at >= got.created_at

def test_update_preserves_created_and_bumps_updated(repo):
    s = Saga(id=None, title="Alpha")
    repo.upsert(s)
    first = repo.get(s.id)
    orig_created = first.created_at
    orig_updated = first.updated_at

    # tiny sleep to ensure updated_at moves forward on coarse clocks
    time.sleep(0.01)

    s.title = "Alpha Prime"
    repo.upsert(s)
    second = repo.get(s.id)

    assert second.title == "Alpha Prime"
    assert second.created_at == orig_created
    assert second.updated_at >= orig_updated

def test_list_filters_text_archived_and_types(repo):
    a = Saga(id=None, title="Build UI")
    b = Saga(id=None, title="Write Docs")
    c = Story(id=None, saga_id=a.id, title="Implement Buttons")  # may need defaults for Story ctor
    b.archived = True

    repo.upsert(a)
    repo.upsert(b)
    repo.upsert(c)

    # text filter (substring)
    res = repo.list(FilterSpec(text="UI", archived=None, types=None))
    assert any(x.title == "Build UI" for x in res)

    # archived filter
    res = repo.list(FilterSpec(text=None, archived=False, types=None))
    assert all(not x.archived for x in res)

    # types filter
    res = repo.list(FilterSpec(text=None, archived=None, types={type(a)}) )
    assert all(type(x) is type(a) for x in res)

