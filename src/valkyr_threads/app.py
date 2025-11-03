from __future__ import annotations
import asyncio
from pathlib import Path
from typing import Optional
from textual.app import App, ComposeResult
from textual.widgets import Header, Footer, Static, Input, DataTable
from textual.containers import Horizontal, Vertical
from textual import on
from textual.message import Message
from textual.reactive import reactive

from .model import Thread, ThreadState
from .storage import load_workspace, save_workspace
from .scheduler import Scheduler, WipLimitError

YAML_PATH = Path("threads.yaml")


class ThreadSelected:
    def __init__(self, thread: Thread):
        self.thread = thread
        super().__init__()

class ThreadsView(DataTable):
    def on_mount(self):
        self.cursor_type = "row"
        self.add_columns("id", "state", "prio", "quantum", "title")

    def load(self, items: list[Thread]):
        self.clear()
        for t in sorted(
            items, key=lambda x: (x.state != ThreadState.RUNNING, x.priority)
        ):
            self.add_row(
                t.id, t.state.value, str(t.priority), t.quantum, t.title, key=t.id
            )
        if items:
            self.focus()
            self.cursor_coordinate = (0, 0)  # type: ignore[attr-defined] # Injected by Textual at runtime, basedpyright can ignore

class DetailView(Static):
    thread: reactive[Optional[Thread]] = reactive(None)

    def watch_thread(self, t: Optional[Thread]) -> None:
        if not t:
            self.update("<no selection>")
            return
        md = f"""
[#] {t.title}
[bold]id:[/bold] {t.id}  [bold]state:[/bold] {t.state.value}  [bold]prio:[/bold] {t.priority}\n
[bold]quantum:[/bold]  [bold]energy:[/bold] {t.energy_band.value} \n
[bold]next_3:[bold]\n- + "\n- ".join(t.next_3 or ["<none>"]) + "\n" + \
        {f"\n[bold]tls:[/bold] {t.tls}\n" if t.tls else ""}
        """
        self.update(md)

class ParalellYou(App):
    CSS = """
    Screen { layout: vertical; }
    #body { layout: horizontal; }
    ThreadsView { width: 60%; }
    DetailView { width: 40%; border: round $accent; }
    """

    BINDINGS = [
        ("q", "quit", "Quit"),
        ("s", "start", "Start"),
        ("y", "yield", "Yield"),
        ("p", "park", "Park"),
        ("d", "done", "Done"),
        ("r", "ready", "Ready"),
        ("n", "new", "New"),
    ]

    def compose(self) -> ComposeResult:
        yield Header(show_clock=True)
        with Horizontal(id="body"):
            self.table = ThreadsView()
            self.detail = DetailView()
            yield self.table
            yield self.detail
        yield Footer()

    def on_mount(self) -> None:
        self.sched = Scheduler(YAML_PATH)
        self.refresh_data()

    def refresh_data(self) -> None:
        ws = load_workspace(YAML_PATH)
        self.ws = ws
        self.table.load(ws.threads)
        self._select_current()

    def _current_row_id(self) -> Optional[str]:
        if not self.table.row_count:
            return None
        row = self.table.get_row_at(self.table.cursor_row)
        return row[0] if row else None

    def _select_current(self) -> None:
        tid = self._current_row_id()
        self.detail.thread = self.ws.get(tid)

    
    @on(ThreadsView.RowHighlighted)
    def on_row(self, _: ThreadsView.RowHighlighted) -> None:
        self._select_current()

    def action_start(self) -> None:
        tid = self._current_row_id()
        if not tid:
            return
        try:
            self.sched.start(tid)
        except WipLimitError as ex:
            self.bell()
            self.status.update(str(ex)) if hasattr("self", "status") else None
        self.refresh_data()

    def action_park(self) -> None:
        self._set_state(ThreadState.PARKED)

    def action_done(self) -> None:
        self._set_state()

    def action_yield(self) -> None:
        tid = self._current_row_id()
        if not tid:
            return

        # quick prompt for next hint
        def _cb(value: str) -> None:
            self.sched.yield_(tid, next_hint=value or None)
            self.refresh_data()
        self.push_screen(_Prompt("Next hint:"), _cb)

    def action_ready(self) -> None:
        self._set_state(ThreadState.READY)

    def _set_state(self, state: ThreadState) -> None:
        tid = self._current_row_id()
        if not tid:
            return
        self.sched.set_state(tid, state)
        self.refresh_data()

    def action_new(self) -> None:
        def _cb(value: str) -> None:
            if not value:
                return
            ws = load_workspace(YAML_PATH)
            ws.threads.append(
                Thread(id=value.strip(), title=value.strip())
            )
            save_workspace(YAML_PATH, ws)
            self.refresh_data()
        self.push_screen(_Prompt("New thread id:"), _cb)

    def action_filter(self) -> None:
        def _cb(value: str) -> None:
            patt = (value or "").lower()
            items = [t for t in self.ws.threads if patt in t.id.lower() or patt in t.title.lower()]
            self.table.load(items)
            self._select_current()
        self.push_screen(_Prompt("Filter:"), _cb)

class _Prompt(Static):
    def __init__(self, label: str):
        super().__init__()
        self.label = label

    def compose(self) -> ComposeResult:
        yield Vertical(
            Static(self.label),
            Input(placeholder="type here and press Enter"),
        )

    @on(Input.Submitted)
    def _on_submit(self, event: Input.Submitted) -> None:
        self.remove()

def run() -> None:
    ParalellYou().run()

if __name__ == "__main__":
    run()
