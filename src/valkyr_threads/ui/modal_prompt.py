# modal.py
from __future__ import annotations
from pathlib import Path
from types import CoroutineType
from typing import Any
from textual.app import ComposeResult
from textual.screen import Screen
from textual.widgets import Static, Input, Button, Label
from textual.containers import Vertical

class ModalPrompt(Screen[str | None]):
    """Centered prompt with dimmed backdrop."""
    BINDINGS = [("escape", "dismiss", "Close")]
    CSS_PATH = "modal_prompt.css"

    def __init__(self, title: str = "Prompt", placeholder: str = "", *, id: str = "prompt"):
        super().__init__(id=id)
        self._title = title
        self._placeholder = placeholder

    def compose(self) -> ComposeResult:
        yield Static("", id="backdrop")   # the dim layer
        with Vertical(id="prompt-box"):
            yield Label(self._title, id="prompt-title")
            yield Input(placeholder=self._placeholder, id="prompt-input")
            yield Button("OK", id="ok")
            yield Button("Cancel", id="cancel")

    def on_button_pressed(self, event: Button.Pressed) -> None:
        if event.button.id == "ok":
            value = self.query_one("#prompt-input", Input).value.strip()
            self.dismiss(value or None)
        else:
            self.dismiss(None)

    async def action_dismiss(self, result: str | None = None) -> None:
        self.dismiss(result)
