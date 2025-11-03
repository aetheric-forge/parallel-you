from textual.screen import Screen
from textual.widgets import Static, Input
from textual.containers import Vertical
from textual.app import ComposeResult
from textual import on

class PromptScreen(Screen[str | None]):
    BINDINGS = [("escape", "cancel", "Cancel")]

    DEFAULT_CSS = """
    PromptScreen { align: center middle; layer: overlay; }
    #prompt-box { width: 60%; max-width: 80; border: round $accent; padding: 1 2; background: $panel; }
    """

    def __init__(self, label: str, placeholder: str = "type and press Enter"):
        super().__init__()
        self.label = label
        self.placeholder = placeholder
        self._input: Input | None = None

    def compose(self) -> ComposeResult:
        self._input = Input("", self.placeholder, id="prompt-input")
        yield (Vertical(
            Static(self.label),
            self._input,
            id="prompt-box",
        ))

    @on(Input.Submitted)
    def _on_submit(self, event: Input.Submitted) -> None:
        self.dismiss(event.value.strip() or None)

    def action_cancel(self) -> None:
        self.dismiss(None)       

    def on_mount(self) -> None:
        self.call_after_refresh(lambda: self.query_one("#prompt-input", Input).focus())

    def on_screen_resume(self) -> None:
        if self._input is not None:
            self.set_focus(self._input)
