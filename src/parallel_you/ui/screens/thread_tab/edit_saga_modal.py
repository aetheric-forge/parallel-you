from typing import Literal
from textual.app import ComposeResult
from textual.containers import Vertical, Horizontal
from textual.widgets import Button, Static
from textual.screen import ModalScreen

from parallel_you.model import Saga
from parallel_you.messages.thread_tab import SagaCreated, SagaUpdated

class EditSagaModal(ModalScreen):
    _saga: Saga | None = None
    _mode: str = "create"

    def __init__(self, saga: Saga | None = None, *, mode: Literal["create", "update"] = "create"):
        super().__init__(f"{"Edit" if mode == "update" else "Create"} Saga", id="edit-saga-modal")
        self._mode = mode
        self._saga = saga

    def compose(self) -> ComposeResult:
        with Vertical(id="esm-hello"):
            yield Static("Hello from Textual")
            with Horizontal(id="esm-hello-buttons"):
                yield Button(f"{"Update" if self._mode == "update" else "Create"}", variant="primary", id="esm-hello-ok-button")
                yield Button("Cancel", variant="default", id="esm-hello-cancel-button")

    def on_button_pressed(self, msg: Button.Pressed):
        if msg.button.id == "esm-hello-ok-button":
            self._saga = Saga("test-saga", "Test Saga") if self._saga is None else self._saga
        self.dismiss(self._saga)
