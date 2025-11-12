from dataclasses import dataclass
from textual.app import ComposeResult
from textual.containers import Vertical
from textual.widgets import Tree, Input
from textual.binding import Binding
from textual.widget import Widget
from rich.text import Text
from textual.reactive import reactive
from textual import events, on
from filter_spec import FilterSpec
from parallel_you.messages.thread_tab.thread_tree import ThreadSelected
from parallel_you.messages.app import RequestRefresh
from parallel_you.model import Saga, Story, Thread
from parallel_you.ui.screens.thread_tab import EditSagaModal, EditStoryModal
from parallel_you.messages.thread_tab import SagaCreated, SagaUpdated, EditSagaCancelled, ApplyFilter, ThreadSelected, StoryCreated, StoryUpdated

SAGA_GLYPH  = "⚙"
STORY_GLYPH = "✦"
THREAD_GLYPH = "∙"

@dataclass(frozen=True)
class Ref:
    id: str
    kind: str

class ThreadTree(Widget):
    can_focus: bool = True
    _filter: reactive[FilterSpec]
    _by_id: dict[str, "Tree.Node"] = {}
    _selected_id = reactive[str | None](None)

    BINDINGS = [
        Binding("left", "collapse", "Collapse"),
        Binding("right", "expand", "Expand"),
        Binding("s", "add_saga", "Add Saga"),
        Binding("S", "add_story", "Add Story"),
    ]

    def __init__(self, repo, initial_filter: FilterSpec, **kwargs):
        super().__init__(**kwargs)
        self._repo = repo
        self._tree = Tree[Ref]("Threads", id="tree")
        self._tree.auto_expand = False
        self._tree.show_root = False
        self._filter = initial_filter

    def compose(self) -> ComposeResult:
        yield self._tree

    def filter(self, spec: FilterSpec):
        self._filter = spec
        self.reload()

    def reload(self) -> None:
        self._by_id.clear()
        self._tree.clear()
        items = self._repo.list(self._filter)
        sagas = [t for t in items if isinstance(t, Saga)]
        stories = [t for t in items if isinstance(t, Story)]

        for s in sorted(sagas, key=lambda x: x.title.lower()):
            self._add_saga(s)
        for st in sorted(stories, key=lambda x: x.title.lower()):
            self._add_story(st)

        self._tree.root.expand()

        # restore cursor if possible
        if self._selected_id and self._selected_id in self._by_id:
            self._tree.select_node(self._by_id[self._selected_id])
        else:
            first = next(iter(self._tree.root.children), None)
            if first:
                self._tree.select_node(first)

    def focus(self, scroll_visible: bool = True) -> None:
        self._tree.focus(scroll_visible=scroll_visible)

    def action_expand(self):
        node = self._tree.cursor_node
        if node is not None and not node.is_expanded:
            node.expand()
    
    def action_collapse(self):
        node = self._tree.cursor_node
        if node is not None and not node.is_collapsed:
            node.collapse()

    def action_add_saga(self) -> None:
        self.run_worker(self._add_saga_flow(), exclusive=True)

    async def _add_saga_flow(self):
        saga = Saga(id=None, title="Test Saga")
        saga = await self.app.push_screen_wait(EditSagaModal(saga))
        if saga is not None:
            self.post_message(SagaCreated(saga))

    def action_add_story(self) -> None:
        self.run_worker(self._add_story_flow(), exclusive=True)

    async def _add_story_flow(self):
        curr = self._tree.cursor_node
        if not isinstance(curr.data, Ref):
            return
        thread = self._repo.get(curr.data.id)
        if not isinstance(thread, Saga):
            return
        story = Story("test-story", thread.id, "Test Story")
        story = await self.app.push_screen_wait(EditStoryModal(story)) 
        if story is not None:
            self.post_message(StoryCreated(story))

    def _label(self, obj: Thread) -> Text:
        glyph = SAGA_GLYPH if isinstance(obj, Saga) else (STORY_GLYPH if isinstance(obj, Story) else THREAD_GLYPH)
        return Text(f"{glyph} {obj.title}")

    def _add_saga(self, s: Saga):
        node = self._tree.root.add_leaf(self._label(s), data=Ref(id=s.id, kind="saga"))
        node.allow_expand = True
        self._by_id[s.id] = node
        self._tree.select_node(node)

    def _add_story(self, s: Story):
        saga = self._repo.get(s.saga_id)
        node = None
        if saga is None:
            return
        for n in self._tree.root.children:
            if n.data.id == s.saga_id:
                node = n
        if node is None:
            return
        story_node = node.add_leaf(self._label(s), data=Ref(id=s.id, kind="story"))
        self._by_id[s.id] = node
        self._tree.select_node(story_node)

    def on_tree_node_expanded(self, event: Tree.NodeExpanded):
        node = event.node
        ref = node.data
        if not isinstance(ref, Ref) or ref.kind != "saga":
            return
        # don't repopulate children
        if node.children:
            return
        # fetch stories for this saga only
        spec = FilterSpec(text=None, archived=self._filter.archived, types={Story})
        stories = [t for t in self._repo.list(spec) if getattr(t, "saga_id", None) == ref.id]
        for st in sorted(stories, key=lambda x: x.title.lower()):
            self._add_story(st)

    def on_tree_node_selected(self, event: Tree.NodeSelected):
        node = event.node
        ref = node.data
        if not isinstance(ref, Ref):
            return
        self._tree.move_cursor(node)
        self._tree.scroll_to_node(node)
        self._selected_id = ref.id
        self.post_message(ThreadSelected(ref.id))

    def on_tree_node_highlighted(self, event: Tree.NodeHighlighted):
        node = event.node
        self._tree.select_node(node)

    def on_request_refresh(self, msg: RequestRefresh):
        self.reload()
