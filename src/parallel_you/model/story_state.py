from enum import Enum

class StoryState(Enum):
    RUNNING = 0
    READY = 1
    PARKED = 2
    BLOCKED = 3
    DONE = 4