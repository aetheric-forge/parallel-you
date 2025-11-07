from __future__ import annotations
from logging import Filter
from pathlib import Path
from types import CellType
from typing import Optional, Text
from rich.segment import Segment
from rich.style import Style
from rich.text import Text
from textual.app import App, ComposeResult
from textual.coordinate import Coordinate
from textual.geometry import Size
from textual.strip import Strip
from textual.widgets import Header, Footer, Static, Input, DataTable
from textual.screen import Screen
from textual.containers import Horizontal, Vertical
from textual import on
from textual.message import Message
from textual.reactive import reactive

from .storage import repo

from .ui.modal_prompt import ModalPrompt
from .model import Thread, ThreadState, EnergyBand
from .scheduler import Scheduler, WipLimitError
from .ui.edit_screen import EditThreadScreen
from .storage.repo_factory import make_repo
from .sort_filter import FilterSpec, apply_filters, sort_threads

class ThreadSelected(Message):
    def __init__(self, thread: Thread):
        self.thread = thread
        super().__init__()

PRIO_COLOR: dict[int, str] = {
    1: "red3",           # critical
    2: "dark_orange3",   # high
    3: "gold3",          # medium
    4: "dodger_blue2",   # low
    5: "grey54",         # someday
}

ENERGY_STYLE = {"DEEP":"bold", "MEDIUM":"none", "LIGHT":"dim"}

# Primary icon set (requires basic Unicode; looks great with Nerd Fonts)
STATE_ICON: dict[str, str] = {
    "READY":    "⏵",
    "RUNNING":  "▶",
    "PARKED":   "⏸",
    "BLOCKED":  "✖",
    "DONE":     "✔",
    "ARCHIVED": "☰",
}

STATE_COLOR: dict[str, str] = {
    "READY":    "chartreuse3",
    "RUNNING":  "cyan3",
    "PARKED":   "grey62",
    "BLOCKED":  "red3",
    "DONE":     "spring_green2",
    "ARCHIVED": "grey42",
}


class ThreadsView(DataTable):
    def __init__(self, threads: list[Thread], *, selected_row_key: str | None = None):
        super().__init__()
        self.border_title = "Threads"
        self.threads = threads
        self._by_id: dict[str, Thread] = {}
        self.selected_row_key = selected_row_key
        self.cursor_type = "row"

    def on_mount(self) -> None:
        # define columns once
        key = self.add_column("title")
        # optional fixed widths (or use .ratio for flex)
        self.columns[key].auto_width = True
        key = self.add_column("quantum")
        self.columns[key].width = 4
        key = self.add_column("updated")
        self.columns[key].width = 11
        key = self.add_column("created")
        self.columns[key].width = 11

        self.show_header = True
        self.zebra_stripes = True
        self.cursor_type = "row"

        # populate
        self.refresh_rows()

    def _title_cell(self, t: Thread) -> Text:
        icon = STATE_ICON[t.state.name]
        icon_style = STATE_COLOR.get(t.state.name, "yellow3")
        title_style = ENERGY_STYLE[t.energy_band.name]
        return Text.assemble((icon + " ", icon_style), (t.title, title_style))
    
    def _q_cell(self, t: Thread) -> Text:
        return Text(t.quantum, style=Style(color=PRIO_COLOR.get(t.priority, "yellow3")))

    def _ts(self, dt) -> Text:
        return Text(dt.strftime("%m-%d %H:%M"), style=Style(dim=True))

    def refresh_rows(self) -> None:
        self.clear()
        self._by_id.clear()
        for t in self.threads:
            self._by_id[t.id] = t
            self.add_row(
                self._title_cell(t),
                self._q_cell(t),
                self._ts(t.updated_at),
                self._ts(t.created_at),
                key=t.id,
            )
        # restore selection if present
        if self.selected_row_key is not None:
            try:
                row_index = self.get_row_index(self.selected_row_key)  # available on recent Textual
                self.cursor_coordinate = Coordinate(row_index, 0)
            except Exception:
                pass
        elif self.row_count > 0:
            self.cursor_coordinate = Coordinate(0, 0)
            self.selected_row_key = self.threads[0].id
        self.focus()

    @on(DataTable.RowHighlighted)
    def _on_row_highlighted(self, event: DataTable.RowHighlighted) -> None:
        key = str(event.row_key.value)
        self.selected_row_key = key
        t = self._by_id.get(key)
        if t:
            self.post_message(ThreadSelected(t))

    @on(DataTable.RowSelected)
    def _on_row_selected(self, event: DataTable.RowSelected) -> None:
        key = str(event.row_key.value)
        self.selected_row_key = key
        t = self._by_id.get(key)
        if t:
            self.post_message(ThreadSelected(t))

    # helper for external refreshes
    def set_threads(self, threads: list[Thread]) -> None:
        self.threads = threads
        self.refresh_rows()


class DetailView(Static):
    thread: reactive[Optional[Thread]] = reactive(None)

    def __init__(self):
        super().__init__()
        self.border_title = "Details"

    def watch_thread(self, thread: Optional[Thread]) -> None:
        if not thread:
            self.update("<no selection>")
            return
        md = f"""
[#] {thread.title}
[bold]id:[/bold] {thread.id}  [bold]state:[/bold] {thread.state.value}  [bold]prio:[/bold] {thread.priority}\n
[bold]quantum:[/bold]  {thread.quantum} [bold]energy:[/bold] {thread.energy_band.value} \n
[bold]next_3:[bold]\n- {"  \n- ".join(thread.next_3 or ['<none>'])} \n 
        {f"\n[bold]tls:[/bold] {thread.tls}\n" if thread.tls else ""}
        """
        self.update(md)

class ParallelYou(App):
    CSS_PATH = "app.css" 

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

    def __init__(self):
        super().__init__()
        self.repo: repo.ThreadRepo = make_repo()
        self.sched: Scheduler = Scheduler(self.repo)
        self.table: ThreadsView = ThreadsView([])
        self.filter_spec: FilterSpec = FilterSpec()
        self.sort_key: str = "created_at"
        self.sort_asc: bool = False

    def compose(self) -> ComposeResult:
        threads = [t for t in self.sched.repo.list(include_archived=True)]
        yield Header(show_clock=True)
        with Horizontal(id="body"):
            self.table = ThreadsView(threads)
            self.detail = DetailView()
            yield self.table
            yield self.detail
        yield Footer()

    def on_mount(self) -> None:
        self.refresh_data()


    def refresh_data(self) -> None:
        # reload workspace defaults
        self.sched.ws = self.sched.repo.load_workspace()
        # fetch live threads from backend
        threads = list(self.sched.repo.list(include_archived=self.filter_spec.include_archived))

        items = sort_threads(apply_filters(threads, self.filter_spec),
                            key=self.sort_key,
                            reverse=(not self.sort_asc if self.sort_key == "created_at" else False))

        self.table.clear()
        self.table.set_threads(items)

    def action_start(self) -> None:
        tid = self.table.selected_row_key
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
        tid = self.table.selected_row_key
        if not tid:
            return
        t = self.sched.ws.get(tid)
        if t:
            self.repo.upsert(t)
        self.sched.set_state(tid, state)
        self.refresh_data()

    def action_new(self) -> None:
        self.push_screen(ModalPrompt("New thread id:"), callback=lambda dism: self._handle_prompt("new", dism))

    def action_yield_(self) -> None:
        if not self.table.selected_row_key:
            return
        self.push_screen(ModalPrompt("Next hint:"), callback=lambda dism: self._handle_prompt("yield", dism))

    def action_filter(self) -> None:
        self.push_screen(ModalPrompt("Filter:"), callback=lambda dism: self._handle_prompt("filter", dism))

    def action_edit_details(self) -> None:
        tid = self.table.selected_row_key
        if not tid:
            return
        t = self.sched.ws.get(tid)
        if not t:
            return
        self.push_screen(
            EditThreadScreen(t),
            callback=lambda dism: self._apply_edit_details(tid, dism),
        )

    async def action_add_hint(self) -> None:
        tid = self.table.selected_row_key
        if not tid:
            return
        t = self.sched.ws.get(tid)
        if not t:
            return
        self.push_screen(ModalPrompt("Add hint:"), callback=lambda dism: self._handle_prompt("add_hint", dism))

    def _apply_edit_details(self, tid: str, data: dict | None) -> None:
        if not data:
            return
        self.sched.ws = self.repo.load_workspace()
        t = self.sched.ws.get(tid)
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
        
        self.repo.upsert(t)
        self.repo.save_workspace(self.sched.ws)
        self.refresh_data()

    def _handle_prompt(self, kind: str, value: str | None) -> None:
        if kind == "new":
            if value:
                self.sched.ws = self.repo.load_workspace()
                t = Thread(id=value, title=value)
                self.repo.upsert(t)
                # we do not have to append to the workspace thread array; it has already been added after refresh following upsert
                self.repo.save_workspace(self.sched.ws)
                self.refresh_data()
        elif kind == "yield":
            tid = self.table.selected_row_key
            if tid:
                self.sched.yield_(tid, next_hint=value or None)
                t = self.repo.get(tid)
                if t:
                    self.repo.upsert(t)
                self.refresh_data()
        elif kind == "filter":
            patt = (value or "").lower()
            self.filter_spec = FilterSpec(text=patt)
            self.refresh_data()

    @on(ThreadSelected)
    def on_thread_selected(self, msg: ThreadSelected) -> None:
        self.detail.thread = msg.thread

def run() -> None:
    ParallelYou().run()

if __name__ == "__main__":
    run()
