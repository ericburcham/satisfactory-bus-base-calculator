"""Dump the per-tier planning results of the Python reference, for the .NET parity test.

The .NET port of python/satisfactory_plan_tiers.py must reproduce run_tiers() exactly: the same dict keys in the same
insertion order and bit-identical floats. This script imports the Python reference by path, runs
TierPlanner(load_data(data/satisfactory_data.json), miner=..., purity=...) + run_tiers(p, include_ingots) and writes
everything run_tiers returns per tier, in Python dict order, with every float as repr() (which round-trips exactly).

It also records every call of Planner.plan() made by optimise() (one per optimisation round) so that the parity test
can compare the intermediate rounds too: rounds whose byproducts end up sunk or disposed of, and rounds that hit an
undisposable fluid, never show in the final tier results.

Usage (from anywhere; the repository root is found by walking up from this file unless given):
    python dump_python_tiers.py [--root <repo root>] [--miner mk1|mk2|mk3] [--purity impure|normal|pure]
                                [--include-ingots] [--out <file name next to this script>]

Defaults write python_tier_results.json (the golden-output configuration: mk1, normal, no ingots).
The committed variants:
    python dump_python_tiers.py
    python dump_python_tiers.py --miner mk2 --purity pure --include-ingots --out python_tier_results_mk2_pure_ingots.json

The output does not depend on PYTHONHASHSEED (checked with several seeds).

File layout (UTF-8 JSON, LF line endings). Ordered maps are lists of [key, value...] pairs so the order is explicit:
    {
      "python": "<interpreter version>",
      "options": {"miner": "mk1", "purity": "normal", "include_ingots": false},
      "tiers": [
        {
          "tier": 0,
          "new_items": ["Desc_...", ...],
          "new": [["Recipe_...", <int machines>], ...],
          "runs": [["Recipe_...", "<repr(float)>"], ...],
          "recs": ["Recipe_...", ...],                      # keys of recs, in order
          "existing": [["Recipe_...", <int>], ...],
          "excess": [["Desc_...", "<repr>"], ...],
          "excess_recipe": [["Desc_...", "Recipe_..."], ...],
          "reused": [["Desc_...", "<repr>"], ...],
          "raw": [["Desc_...", "<repr>"], ...],
          "sink": [["Desc_...", "<repr>"], ...],
          "disposal": [["Desc_...", "<repr(q)>", "Recipe_..."], ...]
        },
        ...
      ],
      "rounds": [                                           # every plan() call, in call order
        {
          "tier": 0,
          "outcome": "ok",                                  # or "undisposable:<fluid class>"
          "excess": [["Desc_...", "<repr>"], ...],          # the demand the round solved (plan's excess dict)
          "machines": [["Recipe_...", "<repr(count)>"], ...],   # machine rows (empty unless ok)
          "extractors": [["Desc_...", "<repr(rate)>"], ...],    # extractor rows (empty unless ok)
          "sink": [["Desc_...", "<repr>"], ...],                # self.sink (empty unless ok)
          "disposal": [["Desc_...", "<repr(q)>", "Recipe_..."], ...]   # self.disposal + disposal_recipe (empty unless ok)
        },
        ...
      ]
    }
"""

import argparse
import importlib.util
import json
import os
import sys


def find_root(start):
    here = os.path.abspath(start)
    while True:
        if os.path.isdir(os.path.join(here, "data")) and os.path.isfile(
                os.path.join(here, "python", "satisfactory_plan_tiers.py")):
            return here
        parent = os.path.dirname(here)
        if parent == here:
            sys.exit(f"No ancestor of {start} contains data/ and python/satisfactory_plan_tiers.py")
        here = parent


def load_module(root):
    path = os.path.join(root, "python", "satisfactory_plan_tiers.py")
    spec = importlib.util.spec_from_file_location("satisfactory_plan_tiers", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def as_int(value):
    if not isinstance(value, int):
        raise TypeError(f"expected an int, got {value!r}")
    return value


def as_repr(value):
    return repr(float(value))


def tier_record(res):
    return {
        "tier": res["tier"],
        "new_items": list(res["new_items"]),
        "new": [[rc, as_int(k)] for rc, k in res["new"].items()],
        "runs": [[rc, as_repr(n)] for rc, n in res["runs"].items()],
        "recs": list(res["recs"].keys()),
        "existing": [[rc, as_int(k)] for rc, k in res["existing"].items()],
        "excess": [[c, as_repr(q)] for c, q in res["excess"].items()],
        "excess_recipe": [[c, r["className"]] for c, r in res["excess_recipe"].items()],
        "reused": [[c, as_repr(q)] for c, q in res["reused"].items()],
        "raw": [[c, as_repr(q)] for c, q in res["raw"].items()],
        "sink": [[c, as_repr(q)] for c, q in res["sink"].items()],
        "disposal": [[f, as_repr(q), r["className"]] for f, (q, r) in res["disposal"].items()],
    }


def record_rounds(module, planner, rounds):
    """Wrap planner.plan so that every optimisation round is recorded."""
    original = planner.plan

    def plan(targets):
        # plan() builds its excess dict from fixed_excess + the targets before solving; rebuild it the same way.
        record = {"tier": planner.tier, "outcome": "ok", "excess": [], "machines": [], "extractors": [],
                  "sink": [], "disposal": []}
        rounds.append(record)
        try:
            rows, excess, surplus = original(targets)
        except module.Undisposable as e:
            record["outcome"] = f"undisposable:{e.args[0]}"
            excess = dict(getattr(planner, "fixed_excess", {}))
            for t in targets:
                if t not in excess:
                    r = planner.recipe_for(t)
                    prod = next(p for p in r["products"] if p["item"] == t)
                    excess[t] = prod["amount"] * 60 / r["time"]
            record["excess"] = [[c, as_repr(q)] for c, q in excess.items()]
            raise
        record["excess"] = [[c, as_repr(q)] for c, q in excess.items()]
        record["machines"] = [[r["cls"], as_repr(r["count"])] for r in rows if r["kind"] == "machine"]
        record["extractors"] = [[planner.item_class(r["recipe"]), as_repr(r["rate"])]
                                for r in rows if r["kind"] == "extractor"]
        record["sink"] = [[c, as_repr(q)] for c, q in planner.sink.items()]
        record["disposal"] = [[f, as_repr(q), planner.disposal_recipe(f)["className"]]
                              for f, q in planner.disposal.items()]
        return rows, excess, surplus

    planner.plan = plan


def render_objects(lines, name, objects, last):
    lines.append(f' {json.dumps(name)}: [')
    for n, obj in enumerate(objects):
        lines.append("  {")
        fields = list(obj.items())
        for k, (key, value) in enumerate(fields):
            comma = "," if k + 1 < len(fields) else ""
            if isinstance(value, list) and value:
                lines.append(f"   {json.dumps(key)}: [")
                for j, entry in enumerate(value):
                    lines.append(f"    {json.dumps(entry)}" + ("," if j + 1 < len(value) else ""))
                lines.append(f"   ]{comma}")
            else:
                lines.append(f"   {json.dumps(key)}: {json.dumps(value)}{comma}")
        lines.append("  }" + ("," if n + 1 < len(objects) else ""))
    lines.append(" ]" + ("" if last else ","))


def render(document):
    """JSON with one map entry per line (stable and diff-friendly)."""
    lines = ["{",
             f' "python": {json.dumps(document["python"])},',
             f' "options": {json.dumps(document["options"])},']
    render_objects(lines, "tiers", document["tiers"], last=False)
    render_objects(lines, "rounds", document["rounds"], last=True)
    lines.append("}")
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=None)
    ap.add_argument("--miner", default="mk1")
    ap.add_argument("--purity", default="normal")
    ap.add_argument("--include-ingots", action="store_true")
    ap.add_argument("--out", default="python_tier_results.json")
    a = ap.parse_args()
    root = a.root or find_root(os.path.dirname(__file__))
    module = load_module(root)
    data = module.load_data(os.path.join(root, "data", "satisfactory_data.json"))
    planner = module.TierPlanner(data, miner=a.miner, purity=a.purity)
    rounds = []
    record_rounds(module, planner, rounds)
    results = module.run_tiers(planner, a.include_ingots)
    document = {
        "python": sys.version.split()[0],
        "options": {"miner": a.miner, "purity": a.purity, "include_ingots": a.include_ingots},
        "tiers": [tier_record(res) for res in results],
        "rounds": rounds,
    }
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), a.out)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(render(document))
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
