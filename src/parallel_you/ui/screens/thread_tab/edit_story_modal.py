from typing import Literal
from textual.app import ComposeResult
from textual.containers import Vertical, Horizontal
from textual.widgets import Button, Static
from textual.screen import ModalScreen

from parallel_you.model import Story

class EditStoryModal(ModalScreen):
    _story: Story | None = None
    _mode: str = "create"

    def __init__(self, story: Story | None = None, *, mode: Literal["create", "update"] = "create"):
        super().__init__(f"{"Edit" if mode == "update" else "Create"} Story", id="edit-story-modal")
        self._mode = mode
        self._story = story

    def compose(self) -> ComposeResult:
        with Vertical(id="estm-hello"):
            yield Static("Hello from Textual")
            with Horizontal(id="estm-hello-buttons"):
                yield Button(f"{"Update" if self._story is not None else "Create"}", variant="primary", id="estm-hello-ok-button")
                yield Button("Cancel", variant="default", id="estm-hello-cancel-button")

    def on_button_pressed(self, msg: Button.Pressed):
        if msg.button.id == "estm-hello-ok-button":
            self._story = Story("test-story", "Test Story") if self._story is None else self._story
        self.dismiss(self._story)
