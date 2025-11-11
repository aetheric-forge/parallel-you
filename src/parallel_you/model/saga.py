from datetime import datetime

from .thread import Thread
from .story import Story

class Saga(Thread):
    _stories: list[Story]

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
        stories: list[Story] = []
    ):
        super().__init__(
            id=id,
            title=title,
            priority=priority,
            quantum=quantum,
            archived=archived,
            created_at=created_at,
            updated_at=updated_at,
            archived_at=archived_at,
        )
        self._stories = stories

    @property
    def stories(self) -> list[Story]:
        return self._stories

    @stories.setter
    def stories(self, s: list[Story] | None):
        if s:
            self._stories = s
        else:
            raise ValueError("stories is required")

