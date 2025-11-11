from parallel_you.storage import Repo
from parallel_you.model import Story, Saga, Thread 
from filter_spec import FilterSpec
from util import gen_id
from datetime import datetime
import re

class MemoryRepo(Repo):
    _threads: dict[str, Thread]

    def __init__(self):
        self._threads = {}

    def list(self, spec: FilterSpec) -> list[Thread]:
        result = []
        for v in self._threads.values():
            if spec.text and not re.search(spec.text, v.title, flags=re.IGNORECASE):
                continue
            if spec.archived is not None and v.archived != spec.archived:
                continue
            if spec.types and not isinstance(v, tuple(spec.types)):
                continue
            result.append(v)
        return result

    def get(self, id: str) -> Thread | None:
        return self._threads.get(id, None)

    def upsert(self, t: Thread) -> str:
        if not getattr(t, "id", None):
            t.id = gen_id()
        thread = self._threads.get(t.id, None)
        if thread is None:
            thread = t
        now = datetime.now().replace(second=0, microsecond=0)
        thread.created_at = now if not thread.created_at else thread.created_at
        thread.updated_at = now
        for f in ["id", "title", "priority", "quantum", "archived", "archived_at"]:
            v = getattr(t, f)
            setattr(thread, f, v)
        self._threads[t.id] = thread
        return thread.id

    def delete(self, id: str) -> None:
        t = self._threads.get("id", id)
        if t is not None:
            del self._threads[t.id]

    def clear(self) -> None:
        self._threads.clear()
