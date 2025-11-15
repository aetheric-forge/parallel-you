import os
from .repo import Repo
from .backends import MemoryRepo, MongoRepo

MONGO_ENV_VAR = "MONGO_URI"

def make_repo(kind: str = "memory") -> Repo:
    if kind == "memory":
        return MemoryRepo()
    elif kind == "mongo":
        uri = os.getenv(MONGO_ENV_VAR, None)
        if uri is None:
            raise RuntimeError("{MONGO_ENV_VAR} is not set")
        return MongoRepo(uri)
