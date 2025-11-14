from textual.app import ComposeResult
from textual.containers import Vertical
from textual.widget import Widget
from textual.widgets import Input, Static
from parallel_you.model import Thread
from reactive import reactive
from rich.text import Text

class ThreadDetails(Widget):
    can_focus: bool = True
    _thread: Thread | None = None

    def __init__(self, repo, **kwargs):
        super().__init__(**kwargs)
        self._repo = repo
        self._content = Static(self._render_content())

    def show_thread(self, thread_id: str):
        self._thread = self._repo.get(thread_id)
        self._content.content = self._render_content()

    def compose(self) -> ComposeResult:
        yield self._content

    def _render_content(self):
        t = self._thread
        if not t:
            return Text("Select a thread…")
        return Text(
            f"{t.title}\n"
            f"priority: {t.priority}\n"
            f"quantum:   {t.quantum}\n"
            f"archived:  {t.archived}\n"
            f"created:   {t.created_at}\n"
            f"updated:   {t.updated_at}\n"
        )
