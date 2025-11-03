.PHONY: run lint fmt test


run:
python -m valkyr_threads.app


lint:
python -m pyflakes src || true


fmt:
python -m pip install ruff black >/dev/null 2>&1 || true
ruff check --fix src || true
black src || true
