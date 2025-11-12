from textual.message import Message

class ThreadSelected(Message):
    thread_id: str | None = None

    def __init__(self, thread_id: str | None):
        super().__init__()
        self.thread_id = thread_id
