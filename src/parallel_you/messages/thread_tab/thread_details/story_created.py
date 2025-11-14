from textual.message import Message

from parallel_you.model import Story

class StoryCreated(Message):
    story: Story

    def __init__(self, story: Story):
        super().__init__()
        self.story = story
