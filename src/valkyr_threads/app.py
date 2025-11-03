from __future__ import annotations
import asyncio
from pathlib import Path
from typing import Optional
from textual.app import App, ComposeResult
from textual.widgets import Header, Footer, Static, Input, DataTable
from textual.screen import Screen
from textual.containers import Horizontal, Vertical
from textual import on
from textual.message import Message
from textual.reactive import reactive

from .model import Thread, ThreadState, EnergyBand
from .storage import load_workspace, save_workspace
from .scheduler import Scheduler, WipLimitError
from .prompt import PromptScreen
from .edit_screen import EditThreadScreen

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
[bold]quantum:[/bold]  {t.quantum} [bold]energy:[/bold] {t.energy_band.value} \n
[bold]next_3:[bold]\n- {"  \n-".join(t.next_3 or ['<none>'])} \n 
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
        ("y", "yield_", "Yield"),
        ("p", "park", "Park"),
        ("d", "done", "Done"),
        ("r", "ready", "Ready"),
        ("n", "new", "New"),
        ("/", "filter", "Filter"),
        ("E", "edit_details", "Edit details"),
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
        if not tid:
            return
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
        self.refresh_data()

    def action_park(self) -> None:
        self._set_state(ThreadState.PARKED)

    def action_done(self) -> None:
        self._set_state(ThreadState.DONE)

    def action_ready(self) -> None:
        self._set_state(ThreadState.READY)

    def _set_state(self, state: ThreadState) -> None:
        tid = self._current_row_id()
        if not tid:
            return
        self.sched.set_state(tid, state)
        self.refresh_data()

    def action_new(self) -> None:
        self.push_screen(PromptScreen("New thread id:"), callback=lambda dism: self._handle_prompt("new", dism))

    def action_yield_(self) -> None:
        if not self._current_row_id():
            return
        self.push_screen(PromptScreen("Next hint:"), callback=lambda dism: self._handle_prompt("yield", dism))

    def action_filter(self) -> None:
        self.push_screen(PromptScreen("Filter:"), callback=lambda dism: self._handle_prompt("filter", dism))

    def action_edit_details(self) -> None:
        tid = self._current_row_id()
        if not tid:
            return
        t = self.ws.get(tid)
        if not t:
            return
        self.push_screen(
            EditThreadScreen(t),
            callback=lambda dism: self._apply_edit_details(tid, dism),
        )

    def _apply_edit_details(self, tid: str, data: dict | None) -> None:
        if not data:
            return
        ws = load_workspace(YAML_PATH)
        t = ws.get(tid)
        if not t:
            return
        
        # Title
        t.title = (data.get("title") or t.title).strip()

        # Priority (coerce int safely)
        pr = data.get("priority")
        try:
            if pr is not None and str(pr).strip() != "":
                t.priority = int(str(pr).strip())
        except ValueError as e:
            pass # ignore bad input

        # Quantum (keep string format like "50m")
        qv = data.get("quantum")
        if qv is not None and str(qv).strip() != "":
            t.quantum = str(qv).strip()
        
        # Energy Band / State (case-insensitive)
        eb = (data.get("energy_band") or t.energy_band.value).strip()
        st = (data.get("state") or t.state.value).strip()
        try:
            t.energy_band = EnergyBand(eb)
        except Exception:
            # try casefold lookup
            m = {e.value.lower(): e for e in EnergyBand}
            if eb.lower() in m:
                t.energy_band = m[eb.lower()]

        try:
            t.state = ThreadState(st)
        except Exception:
            m = {s.value.lower(): s for s in ThreadState}
            if st.lower() in m:
                t.state = m[st.lower()]

        # TLS
        raw_tls = data.get("tls")
        if raw_tls is not None:
            tlss = str(raw_tls).strip()
            t.tls = tlss or None
        
        save_workspace(YAML_PATH, ws)
        self.refresh_data()

    def _handle_prompt(self, kind: str, value: str | None) -> None:
        if kind == "new":
            if value:
                ws = load_workspace(YAML_PATH)
                ws.threads.append(Thread(id=value, title=value))
                save_workspace(YAML_PATH, ws)
                self.refresh_data()
        elif kind == "yield":
            tid = self._current_row_id()
            if tid:
                self.sched.yield_(tid, next_hint=value or None)
                self.refresh_data()
        elif kind == "filter":
            patt = (value or "").lower()
            items = [t for t in self.ws.threads if patt in t.id.lower() or patt in t.title.lower()]
            self.table.load(items)
            self.refresh_data()


def run() -> None:
    ParalellYou().run()

if __name__ == "__main__":
    run()

