from __future__ import annotations
from pathlib import Path
import typer
from valkyr_threads.scheduler import Scheduler
from valkyr_threads.model import ThreadState

app = typer.Typer(add_completion=False)
YAML = Path("threads.yaml")

@app.command()
def start(thread_id: str):
    Scheduler(YAML).start(thread_id)
    typer.echo(f"Started: {thread_id}")

@app.command(name="yield")
def yield_(thread_id: str, next: str = typer.Option("", help="one-line next-hint")):
    Scheduler(YAML).yield_(thread_id, next_hint=next or None)
    typer.echo(f"Yielded: {thread_id}")

@app.command()
def set(thread_id: str, state: ThreadState):
    Scheduler(YAML).set_state(thread_id, state)
    typer.echo(f"{thread_id} -> {state.value}")

def main() -> None:  # <-- entry point calls this
    app()
