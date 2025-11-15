from datetime import datetime

from .story_state import StoryState
from .energy_band import EnergyBand
from .thread import Thread

class Story(Thread):
    _saga_id: str
    _quantum_overriden: bool
    _state: StoryState
    _energy: EnergyBand

    def __init__(
        self,
        id: str | None,
        saga_id: str,
        title: str, 
        priority: int = 1, 
        quantum: str = "50m",
        quantum_overridden: bool = False,
        energy: EnergyBand = EnergyBand.MODERATE,
        state: StoryState = StoryState.READY,
        archived: bool = False,
        created_at: datetime | None = None,
        updated_at: datetime | None = None,
        archived_at: datetime | None = None,
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
        self._saga_id = saga_id
        self._quantum_overridden = quantum_overridden
        self._state = state
        self._energy = energy

    @property
    def id(self) -> str | None:
        return self._id

    @id.setter
    def id(self, id: str | None):
        self._id = id

    @property
    def saga_id(self) -> str:
        return self._saga_id

    @saga_id.setter
    def saga_id(self, sid: str | None) -> None:
        if sid is not None:
            self._saga_id = sid
        else:
            raise ValueError("saga_id is required")

    @property
    def quantum_overridden(self) -> bool:
        return self._quantum_overridden

    @quantum_overridden.setter
    def quantum_overridden(self, o: bool | None) -> None:
        if o is not None:
            self._quantum_overridden = o
        else:
            raise ValueError("quantum_overridden is required")

    @property
    def state(self) -> StoryState:
        return self._state

    @state.setter
    def state(self, s: StoryState | None):
        if s is not None:
            self._state = s
        else:
            raise ValueError("state is required")

    @property
    def energy(self) -> EnergyBand:
        return self._energy

    @energy.setter
    def energy(self, e: EnergyBand | None):
        if e is not None:
            self._energy = e
        else:
            raise ValueError("energy is required")
