from textual.app import ComposeResult
from textual.binding import Binding
from textual.containers import Horizontal
from textual.widgets import TabPane

from parallel_you.storage import Repo
from parallel_you.messages.thread_tab import ApplyFilter, ThreadSelected, SagaCreated, SagaUpdated, EditSagaCancelled, StoryCreated, StoryUpdated, EditStoryCancelled
from parallel_you.messages.app import RequestRefresh
from .thread_tree import ThreadTree
from .thread_details import ThreadDetails
from filter_spec import FilterSpec

class ThreadTab(TabPane):
    _tree: ThreadTree
    _details: ThreadDetails
    _repo: Repo

    BINDINGS = [
        Binding("t", "focus_tree", "Threads"),
        Binding("d", "focus_details", "Details"),
    ]

    def __init__(self, repo, **kwargs):
        super().__init__("Threads", id="thread-tab", **kwargs)
        self._repo = repo
        self._tree = ThreadTree(self._repo, FilterSpec(), id="thread-tree")
        self._details = ThreadDetails(self._repo, id="thread-details")
    
    def compose(self) -> ComposeResult:
        with Horizontal(id="thread-tab-pane"):
            yield self._tree
            yield self._details

    def reload(self) -> None:
        self._tree.reload()
        self._tree.focus(True)

    def action_focus_tree(self) -> None:
        self._tree.focus(True)

    def action_focus_details(self) -> None:
        self._details.focus(True)

    def on_apply_filter(self, msg: ApplyFilter):
        self._tree.filter(FilterSpec(text=msg.text, archived=msg.archived, types=msg.types))

    def on_thread_selected(self, msg: ThreadSelected):
        self._details.show_thread(msg.thread_id)

    def on_saga_created(self, msg: SagaCreated):
        self._repo.upsert(msg.saga)
        self.reload()

    def on_saga_updated(self, msg: SagaUpdated):
        self._repo.upsert(msg.saga)
        self.reload()

    def on_story_created(self, msg: StoryCreated):
        self._repo.upsert(msg.story)
        self.reload()

    def on_story_updated(self, msg: StoryUpdated):
        self._repo.upsert(msg.story)
        self.reload()
