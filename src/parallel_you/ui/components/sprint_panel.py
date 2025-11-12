from textual.app import ComposeResult
from textual.containers import Vertical
from textual.widget import Widget
from textual.widgets import Input, Static

class SprintPanel(Widget):
    can_focus: bool = True

    def __init__(self, repo, **kwargs):
        super().__init__(**kwargs)
        self._repo = repo
        self._content = Static(self._render_content())
        self._prompt = Input(id="sprint-prompt", classes="hidden")
        self.refresh()

    def compose(self) -> ComposeResult:
        with Vertical(id="sprint-panel"):
            yield self._content
            yield self._prompt

    def _render_content(self):
        # placeholder; fill with metrics soon
        return "Sprint: WIP, velocity, blockers\n(press 's' to toggle)"
