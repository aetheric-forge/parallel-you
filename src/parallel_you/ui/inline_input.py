from typing import Type, Callable, Optional
from textual.app import ComposeResult
from textual.message import Message
from textual.widget import Widget
from textual.widgets import Input
from textual.reactive import reactive
from textual import on

def _default_validator(title: str) -> tuple[bool, str | None]:
    return (bool(title.strip()), "Title is required")

class InlineInput(Widget):
    """Ephemeral inline input that spawns under the cursor."""

    DEFAULT_CSS = """
    InlineInput Input {
      width: 100%;
    }

    InlineInput Input.invalid {
      border: heavy red;
    }
    """

    # Public knobs
    placeholder: str = reactive("New title...")
    parent_id: str | None = reactive(None)
    default_text: str = reactive("")

    # The type of message to send, accepting a single argument of the type produced by model_factory, below
    message_class: Type[Message]

    # model_factory, produces model_class instances given input
    model_factory: Callable[[str, Optional[str]], object]

    # optional validator, returns (ok, error_msg or None)
    validator: Optional[Callable[[str], tuple[bool, Optional[str]]]] = _default_validator

    def __init__(self, *, 
        # reactable changes
        placeholder: str = "New title...",
        default_text: str = "",
        parent_id: Optional[str] = None,

        # set only at construction
        message_class: Type[Message], 
        model_factory: Callable[[str, Optional[str]], object], 
        validator: Optional[Callable[[str], tuple[bool, Optional[str]]]] = None,

        **kwargs
    ):
        super().__init__(**kwargs)
        self.parent_id = parent_id
        self.model_factory = model_factory
        self.message_class = message_class
        self.default_text = default_text
        self.placeholder = placeholder
        self.validator = validator if validator is not None else _default_validator

        self._input: Optional[Input] = None

    def compose(self) -> ComposeResult:
        self._input = Input(placeholder=self.placeholder)
        if self.default_text:
            self._input.value = self.default_text
            self._input.cursor_position = len(self.default_text)
        yield self._input

    def on_mount(self) -> None:
        self._input.focus()

    @on(Input.Submitted)
    def _submit(self, ev: Input.Submitted):
        title = ev.value.strip()
        if not title:
            self.remove()
            return

        # inline validate
        if self.validator is not None:
            ok, err = self.validator(title) 
            if not ok:
                self._input.tooltip = err or "Invalid"
                self._input.add_class("invalid")
                return

        # create the model instance to construct event
        msg = self.model_factory(title, self.parent_id)

        # construct the concrete event
        ev = self.message_class(msg)

        # fire-and-forget so UI stays snappy
        self.app.post_message(ev)
        self.remove()

    async def on_input_changed(self, ev: Input.Changed) -> None:
        # live-clear error state once user types
        if "invalid" in self._input.classes:
            self._input.remove_class("invalid")
            self._input.tooltip = None

    async def on_key(self, ev) -> None:
        if ev.key == "escape":
            self.remove()
