from textual.app import App, ComposeResult
from textual.binding import Binding
from textual.widgets import Footer, Input, Header, TabbedContent
from parallel_you.storage.repo_factory import make_repo
from filter_spec import FilterSpec
from parallel_you.ui.components import ThreadTree, ThreadDetails, ThreadTab
from parallel_you.messages.thread_tab import ThreadSelected, ApplyFilter, SagaCreated, StoryCreated
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
        self._tabs.focus()
        self._thread_tab.reload()

    def on_request_refresh(self, msg: RequestRefresh):
        self._thread_tab.reload()
    
    def on_saga_created(self, msg: SagaCreated):
        self._thread_tab.on_saga_created(msg)

    def on_story_created(self, msg: StoryCreated):
        self._thread_tab.on_story_created(msg)

    