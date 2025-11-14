from textual.message import Message

from parallel_you.model import Story

class StoryUpdated(Message):
    story: Story

    def __init__(self, story: Story):
        super().__init__()
        self.story = story
