from __future__ import annotations
from typing import Optional, Dict, Any
from textual.app import ComposeResult
from textual.screen import Screen
from textual.containers import Vertical, Horizontal
from textual.widgets import Static, Input, Select, Button

from .model import Thread, ThreadState, EnergyBand

class EditThreadScreen(Screen[Optional[Dict[str, Any]]]):
    """
    Modal-like screen to edit Thread fields.
    Returns a dict of updated values, or None if cancelled.
    """

    DEFAULT_CSS = """
    EditThreadScreen { align: center middle; layer: overlay; }
    #box { width: 70%; max-width: 90; border: round $accent; padding: 1 2; background: $panel; }
    .row { layout: horizontal; }
    .row > * { width: 1fr; }
    #buttons { layout: horizontal; content-align: right middle; }
    """

    def __init__(self, thread: Thread):
        super().__init__()
        self.thread = thread
        self._title: Input | None = None
        self._prio: Input | None = None
        self._quantum: Input | None = None
        self._energy: Select[tuple[str, str]] | None = None
        self._state: Select[tuple[str, str]] | None = None
        self._tls: Input | None = None

    def compose(self) -> ComposeResult:
        t = self.thread
        self._title = Input(value=t.title, placeholder="title", id="title")
        self._prio = Input(value=str(t.priority), placeholder="priority (int)", id="prio")
        self._quantum = Input(value=t.quantum, placeholder="quantum (e.g. 50m)", id="quantum")
        self._energy = Select(
            options=[(e.value, e.value) for e in EnergyBand],
            value=t.energy_band.value,
            id="energy"
        )
        self._state = Select(
            options=[(s.value, s.value) for s in ThreadState],
            value=t.state.value,
            id="state"
        )
        self._tls = Input(value=t.tls or "", placeholder="tls path (optional)", id="tls")

        yield Vertical(
            Static(f"Edit: [b]t.id[/b]", id="hdr"),
            Vertical(
                self._title,
                Horizontal(self._prio, self._quantum, classes="row"),
                Horizontal(self._energy, self._state, classes="row"),
                self._tls,
                id="fields",
            ),
            Horizontal(
                Button("Cancel", id="cancel"),
                Button("Save", variant="primary", id="save"),
                id="buttons",
            ),
            id="box",
        )

    def on_screen_resume(self) -> None:
        if self._title is not None:
            self.set_focus(self._title)

    def on_button_pressed(self, event: Button.Pressed) -> None:
        if event.button.id == "cancel":
            self.dismiss(None)
            return
        if event.button.id == "save":
            result: Dict[str, Any] = {
                "title": (self._title.value if self._title else self.thread.title),
                "priority": (self._prio.value if self._prio else self.thread.priority),
                "quantum": (self._quantum.value if self._quantum else self.thread.quantum),
                "energy_band": (self._energy.value if self._energy else self.thread.energy_band),
                "state": (self._state.value if self._state else self.thread.state),
                "tls": (self._tls.value if self._tls else self.thread.tls),
            }
            self.dismiss(result)
