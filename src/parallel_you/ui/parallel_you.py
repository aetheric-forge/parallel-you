from textual.app import App, ComposeResult
from textual.binding import Binding
from textual.widgets import Footer, Input, Header, TabbedContent
from parallel_you.storage.repo_factory import make_repo
from filter_spec import FilterSpec
from parallel_you.ui.components import ThreadTree, ThreadDetails, ThreadTab
from parallel_you.messages.thread_tab import ThreadSelected, ApplyFilter
from parallel_you.messages.app import RequestRefresh

class ParallelYou(App):
    CSS_PATH = "parallel_you.css"
    BINDINGS = [
        Binding("r", "refresh", "Refresh"),
        Binding("q", "quit", "Quit"),
    ]

    def __init__(self, repo=None):
        super().__init__()
        self._repo = repo or make_repo()
        self._filter = FilterSpec(text=None, archived=False, types=None)
        self._thread_tab = ThreadTab(self._repo)
        self._tabs = TabbedContent("Threads", id="tab-threads")

    def compose(self) -> ComposeResult:
        yield Header()
        with self._tabs:
            yield self._thread_tab
        yield Footer()

    def action_refresh(self):
        self.post_message(RequestRefresh())

    def on_mount(self):
        from parallel_you.model import Saga
        if not self._repo.list(self._filter):
            a = Saga(id=None, title="Build UI")
            b = Saga(id=None, title="Write Docs")
            self._repo.upsert(a); self._repo.upsert(b)
        self._tabs.focus()
        self._thread_tab.reload()

    def on_request_refresh(self, msg: RequestRefresh):
        self._thread_tab.reload()
    