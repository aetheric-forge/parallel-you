from util import gen_id
from datetime import datetime

from .energy_band import EnergyBand

class Thread:
    _id: str | None
    _title: str
    _priority: int
    _quantum: str
    _archived: bool
    _created_at: datetime | None
    _updated_at: datetime | None
    _archived_at: datetime | None

    def __init__(
            self, 
            id: str | None,
            title: str, 
            priority: int = 1, 
            quantum: str = "50m",
            archived: bool = False,
            created_at: datetime | None = None,
            updated_at: datetime | None = None,
            archived_at: datetime | None = None,
        ):
        self._id = gen_id() if id is None else id
        self._title = title
        self._priority = priority
        self._quantum = quantum
        self._archived = archived
        self._created_at = created_at
        self._updated_at = updated_at
        self._archived_at = archived_at
    
    @property
    def id(self) -> str:
        return self._id

    @id.setter
    def id(self, id: str | None) -> None:
        if id is not None and id != "":
            self._id = id
        else:
            raise ValueError("id is required")

    @property
    def title(self) -> str:
        return self._title

    @title.setter
    def title(self, t: str | None) -> None:
        if t is not None:
            self._title = t
        else:
            raise ValueError("title is required")
        
    @property
    def priority(self) -> int:
        return self._priority

    @priority.setter
    def priority(self, p: int | None) -> None:
        if p is not None:
            self._priority = p
        else:
            raise ValueError("priority is required")

    @property
    def quantum(self) -> str:
        return self._quantum

    @quantum.setter
    def quantum(self, q: str | None) -> None:
        if q is not None:
            self._quantum = q
        else:
            raise ValueError("quantum is required")

    @property
    def archived(self) -> bool:
        return self._archived

    @archived.setter
    def archived(self, a: bool | None) -> None:
        if a is not None:
            self._archived = a
        else:
            raise ValueError("archived is required")

    @property
    def created_at(self) -> datetime:
        return self._created_at

    @created_at.setter
    def created_at(self, d: datetime | None) -> None:
        if d is not None:
            self._created_at = d
        else:
            raise ValueError("created_at is required")

    @property
    def updated_at(self) -> datetime:
        return self._updated_at

    @updated_at.setter
    def updated_at(self, d: datetime | None) -> None:
        if d is not None:
            self._updated_at = d
        else:
            raise ValueError("updated_at is required")

    @property
    def archived_at(self) -> datetime | None:
        return self._archived_at

    @archived_at.setter
    def archived_at(self, d: datetime | None):
        self._archived_at = d
