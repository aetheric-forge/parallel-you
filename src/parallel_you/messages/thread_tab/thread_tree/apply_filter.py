from textual.message import Message

class ApplyFilter(Message):
    text: str | None = None
    archived: bool | None = None
    types: set[type] | None = None

    def __init__(self, text: str | None = None, archived: bool | None = None, types: set[type] | None = None):
        self.text = text
        self.archived = archived
        self.types = types
