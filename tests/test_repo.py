from datetime import datetime
import time
import pytest
import os

from parallel_you.storage.repo_factory import make_repo
from parallel_you.model.saga import Saga
from parallel_you.model.story import Story
from filter_spec import FilterSpec

def all_items(repo):
    return repo.list(FilterSpec(text=None, archived=None, types=None))

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
    # Arrange
    a = Saga(id=None, title="Build UI")
    b = Saga(id=None, title="Write Docs")
    repo.upsert(a)
    repo.upsert(b)

    # Story must use a real saga_id, so do it after upserting `a`
    c = Story(id=None, saga_id=a.id, title="Implement Buttons")
    repo.upsert(c)

    # Archive `b`
    b.archived = True
    repo.upsert(b)

    # 1) text filter (substring, case-insensitive)
    res = repo.list(FilterSpec(text="UI", archived=None, types=None))
    titles = {x.title for x in res}
    assert "Build UI" in titles
    assert "Write Docs" not in titles  # sanity check

    # 2) archived filter
    res = repo.list(FilterSpec(text=None, archived=False, types=None))
    assert all(not x.archived for x in res)          # only non-archived
    res_arch = repo.list(FilterSpec(text=None, archived=True, types=None))
    assert all(x.archived for x in res_arch)         # only archived
    assert any(x.title == "Write Docs" for x in res_arch)

    # 3) types filter
    res_saga = repo.list(FilterSpec(text=None, archived=None, types={Saga}))
    assert res_saga and all(isinstance(x, Saga) for x in res_saga)
    res_story = repo.list(FilterSpec(text=None, archived=None, types={Story}))
    assert res_story and all(isinstance(x, Story) for x in res_story)

def test_archive_flip_sets_archived_at(repo):
    s = Saga(id=None, title="Alpha")
    repo.upsert(s)
    x = repo.get(s.id)
    assert x.archived is False and x.archived_at is None

    s.archived = True
    repo.upsert(s)
    y = repo.get(s.id)
    assert y.archived is True and y.archived_at is not None

    mark = y.archived_at
    repo.upsert(s)  # no flip
    z = repo.get(s.id)
    assert z.archived_at == mark

    s.archived = False
    repo.upsert(s)
    u = repo.get(s.id)
    assert u.archived is False and u.archived_at is None

def test_delete_existing_and_missing(repo):
    s = Saga(id=None, title="To Delete")
    repo.upsert(s)
    assert repo.get(s.id) is not None

    # delete existing
    assert repo.delete(s.id) is True
    assert repo.get(s.id) is None

    # idempotent delete (should return False / no error)
    assert repo.delete(s.id) is False

def test_clear(repo):
    a = Saga(id=None, title="A")
    b = Saga(id=None, title="B")
    c = Story(id=None, saga_id=a.id, title="C")
    repo.upsert(a); repo.upsert(b); repo.upsert(c)

    assert len(all_items(repo)) >= 3
    repo.clear()
    assert all_items(repo) == []
