from .repo import Repo
from .backends import MemoryRepo

def make_repo() -> Repo:
    return MemoryRepo()
