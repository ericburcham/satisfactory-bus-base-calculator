"""Generate tier_graph_vectors.json: the per-tier planner decisions of the Python reference that the tier graph needs.

The .NET TierGraphBuilder / TierBuildOrder tests rebuild each tier's TierResult from this file (recipes looked up in the
real game data by class name) without running the .NET planner, then check that the graph and the build order
reproduce golden-output/tier_N.dot. This isolates the graph and ordering port from the solver port.

Usage (from the repository root):
    python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/TierGraph/generate_tier_graph_vectors.py

It runs run_tiers of python/satisfactory_plan_tiers.py with the defaults the golden output was made with
(data/satisfactory_data.json, --miner mk1, --purity normal, no --include-ingots) and writes the file next to this
script. The run is deterministic, so re-running reproduces the committed file exactly.

File layout (UTF-8 JSON, LF line endings, one tier per line):
    {"python": "<version>", "tiers": [
    {"tier": N,
     "newItems": ["<item class>", ...],             # res["new_items"], in order
     "new": [["<recipe class>", k], ...],           # res["new"], in insertion order
     "recipes": ["<recipe class>", ...],            # res["recs"] keys, in insertion order
     "sink": [["<item class>", q], ...],            # res["sink"], in insertion order (q is a float, repr round-trips)
     "order": ["<recipe class>", ...]},             # tier_order(p, res)[0]: the Python build order
    ...]}
"""

import importlib.util
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
SCRIPT = os.path.join(ROOT, "python", "satisfactory_plan_tiers.py")
DATA = os.path.join(ROOT, "data", "satisfactory_data.json")
OUT = os.path.join(HERE, "tier_graph_vectors.json")


def load_reference():
    sys.dont_write_bytecode = True  # never write a __pycache__ next to the read-only reference script
    spec = importlib.util.spec_from_file_location("satisfactory_plan_tiers", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    ref = load_reference()
    p = ref.TierPlanner(ref.load_data(DATA), miner="mk1", purity="normal")
    results = ref.run_tiers(p, include_ingots=False)
    lines = []
    for res in results:
        order, _ = ref.tier_order(p, res)
        entry = {
            "tier": res["tier"],
            "newItems": list(res["new_items"]),
            "new": [[rc, k] for rc, k in res["new"].items()],
            "recipes": list(res["recs"]),
            "sink": [[c, q] for c, q in res["sink"].items()],
            "order": list(order),
        }
        lines.append(json.dumps(entry, separators=(",", ":")))
    version = ".".join(str(v) for v in sys.version_info[:3])
    text = '{"python":' + json.dumps(version) + ',"tiers":[\n' + ",\n".join(lines) + "\n]}\n"
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
