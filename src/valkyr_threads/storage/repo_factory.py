import os
from pathlib import Path
from .repo import ThreadRepo

def make_repo(
    kind: str | None = None,
    *,
    yaml_path: str | None = None,
    mongo_uri: str | None = None,
    mongo_db: str = "parallel_you",
) -> ThreadRepo:
    kind = (kind or os.getenv("PARALLEL_YOU_REPO") or "yaml").lower()
    if kind == "mongo":
        from .repo_mongo import MongoThreadRepo  # optional dep
        return MongoThreadRepo(mongo_uri or os.getenv("MONGO_URI", "mongodb://localhost:27017"), mongo_db)
    # default: yaml
    from .repo_yaml import YamlThreadRepo
    return YamlThreadRepo(Path(yaml_path or os.getenv("WORKSPACE_YAML", "workspace.yaml")))
