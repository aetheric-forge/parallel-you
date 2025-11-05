# src/valkyr_threads/cli.py
from __future__ import annotations
import os
from pathlib import Path
from typing import Optional, Dict, Any
import typer
from .storage.repo_factory import make_repo
from .scheduler import Scheduler
from .model import ThreadState, EnergyBand
from .sort_filter import FilterSpec, apply_filters, sort_threads

app = typer.Typer(no_args_is_help=True)

# ---- Root callback wires shared state into ctx.obj ---------------------------
@app.callback()
def callback(
    ctx: typer.Context,
    repo: Optional[str] = typer.Option(None, help="storage backend: yaml|mongo (default: YAML)"),
    yaml_path: Path = typer.Option(Path(os.getenv("WORKSPACE_YAML", "workspace.yaml")), help="YAML path"),
    mongo_uri: Optional[str] = typer.Option(os.getenv("MONGO_URI"), help="Mongo URI"),
    mongo_db: str = typer.Option("parallel_you", help="Mongo database"),
):
    # Build the repo once; share via ctx.obj for all subcommands
    ctx.ensure_object(dict)
    ctx.obj["repo"] = make_repo(
        kind=repo,
        yaml_path=str(yaml_path),
        mongo_uri=mongo_uri,
        mongo_db=mongo_db,
    )

# Small helper to get a Scheduler from ctx.obj
def _sch(ctx: typer.Context) -> Scheduler:
    return Scheduler(ctx.obj["repo"])

# ---- Example commands using ctx.obj -----------------------------------------
@app.command("ls")
def ls_cmd(
    ctx: typer.Context,
    states: Optional[str] = typer.Option(None, help="CSV of states e.g. Ready,Running"),
    energy: Optional[str] = typer.Option(None, help="CSV of energy e.g. Deep,Medium"),
    min_prio: Optional[int] = typer.Option(None),
    max_prio: Optional[int] = typer.Option(None),
    text: Optional[str] = typer.Option(None),
    include_archived: bool = typer.Option(False),
    sort: str = typer.Option("created_at", help="created_at|priority|energy"),
    asc: bool = typer.Option(False),
):
    sch = _sch(ctx)
    def _states(s): 
        return None if not s else [ThreadState(x.strip().title()) for x in s.split(",") if x.strip()]
    def _energy(s):
        return None if not s else [EnergyBand(x.strip().title()) for x in s.split(",") if x.strip()]

    spec = FilterSpec(
        states=_states(states),
        energy=_energy(energy),
        min_priority=min_prio,
        max_priority=max_prio,
        text=text,
        include_archived=include_archived,
    )
    items = sort_threads(
        apply_filters(sch.ws.threads, spec),
        key=sort,
        reverse=(not asc if sort == "created_at" else False),
    )
    for t in items:
        print(f"{t.id:>8}  [{t.state.value:<7}] p{t.priority} {t.energy_band.value:<6}  {t.created_at.isoformat()}  {t.title}")

@app.command("archive")
def archive_cmd(ctx: typer.Context, thread_id: str):
    sch = _sch(ctx)
    t = sch.repo.get(thread_id)
    if not t:
        typer.echo("thread not found"); raise typer.Exit(1)
    t.archived = True
    sch.repo.upsert(t)
    typer.echo(f"archived {t.id}  {t.title}")

@app.command("unarchive")
def unarchive_cmd(ctx: typer.Context, thread_id: str):
    sch = _sch(ctx)
    t = sch.repo.get(thread_id)
    if not t:
        typer.echo("thread not found"); raise typer.Exit(1)
    t.archived = False
    sch.repo.upsert(t)
    typer.echo(f"unarchived {t.id}  {t.title}")

@app.command("import-yaml")
def import_yaml(
    ctx: typer.Context,
    yaml_path: Path = typer.Option(Path("workspace.yaml"))
):
    from .storage.workspace import load_workspace
    from .storage.repo_mongo import _to_doc  # safe even if YAML repo chosen
    sch = _sch(ctx)
    ws = load_workspace(yaml_path)
    # save workspace
    sch.repo.save_workspace(ws)
    # seed threads
    for t in ws.threads:
        sch.repo.upsert(t)
    typer.echo(f"Imported {len(ws.threads)} threads from {yaml_path}")

def build_app() -> typer.Typer:
    return app

def main() -> None:
    build_app()()
