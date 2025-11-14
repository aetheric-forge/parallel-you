from textual.message import Message

from parallel_you.model import Saga

class SagaCreated(Message):
    saga: Saga

    def __init__(self, saga: Saga):
        super().__init__()
        self.saga = saga
