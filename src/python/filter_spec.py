from dataclasses import dataclass

@dataclass
class FilterSpec:
    text: str | None = None
    archived: bool | None = None
    types: list[type] | None = None
