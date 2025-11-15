from textual.message import Message

from parallel_you.model import Story

class EditStoryCancelled(Message):
    story: Story | None = None

    def __init__(self, story: Story | None = None):
        super.__init__()
        self.story = story
