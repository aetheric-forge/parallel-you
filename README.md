# parallel-you

A tiny, opinionated, YAML-backed task/thread manager with a Textual TUI. Ships with:
* YAML persistence (`threads.yaml`)
* Core domain model (Thread, state, energy bands)
* Scheduler actions (start/yield/park/done)
* CLI helper for quick state flips

## Project tree

```
parallel-you/
|- pyproject.toml
|- Makefile
|- threads.yaml
|- README.md
|- src/
|  +- valkyr_threads/
|     +- __init__.py
|     |- model.py
|     |- storage.py
|     |- scheduler.py
|     |- app.py
|- scripts/
   +- vyctl.py
```

## Quick start

```sh
# 1) create venv
python3 -m venv .venv/ && source .venv/bin/activate

# 2) install
pip install -e .

# 3) run the TUI
vy-tui

# 4) use CLI
vy start baator-dice-refactor --quantum 60m
vy yield baator-dice-refactor --next "write failing test for nullable target_id"
```

### Hotkeys (inside TUI):
* **Enter**: open details
* **s**: start quantum
* **y**: (mark Ready, write next-step hint)
* **p**: park
* **d**: done
* **r**: set ready
* **n**: new thread
* **/**: filter
* **q**: quit

