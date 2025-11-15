import uuid
import re
import unicodedata

def gen_id():
    return uuid.uuid4().hex[:12]

def slugify(text: str, max_length: int = 64) -> str:
    """Convert arbitrary text into a filesystem/URL-safe slug.

    Examples:
        >>> slugify("The Weave Consolidation Protocol!")
        'the-weave-consolidation-protocol'
        >>> slugify("  Saga: Thread #42  ")
        'saga-thread-42'
    """
    if not text:
        return ""

    # Normalize Unicode characters (e.g., é -> e)
    text = unicodedata.normalize("NFKD", text)
    text = text.encode("ascii", "ignore").decode("ascii")

    # Lowercase and strip leading/trailing whitespace
    text = text.strip().lower()

    # Replace any run of non-alphanumeric characters with a single dash
    text = re.sub(r"[^a-z0-9]+", "-", text)

    # Remove leading/trailing dashes
    text = text.strip("-")

    # Enforce a maximum length (optional but handy for slugs in URLs or filenames)
    return text[:max_length]
