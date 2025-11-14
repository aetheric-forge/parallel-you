from textual.message import Message

from parallel_you.model import Saga

class EditSagaCancelled(Message):
    orig_saga: Saga | None = None

    def __init__(self, orig_saga = None):
        self.orig_saga = orig_saga

