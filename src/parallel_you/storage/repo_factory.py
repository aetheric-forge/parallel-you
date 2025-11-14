import os
from .repo import Repo
from .backends import MemoryRepo, MongoRepo

def make_repo(kind: str = "memory") -> Repo:
    if kind == "memory":
        return MemoryRepo()
    elif kind == "mongo":
        uri = os.getenv("MONGO_URI", None)
        if uri is None:
            raise RuntimeError("MONGO_URI is not set")
        return MongoRepo(uri)
