"""Generate python_sum_vectors.json: CPython's builtin sum() over lists of floats.

Since Python 3.12, sum() of floats is not a plain left-to-right addition: CPython adds with Neumaier's compensated
summation (builtin_sum_impl in Python/bltinmodule.c), so for example sum([0.1, 0.2, 0.3]) is 0.6 while
0.1 + 0.2 + 0.3 is 0.6000000000000001. The planner calls sum() on floats in _costs, recipe_cost, disposal_recipe and
main, so the .NET port (PythonNumerics.Sum) must reproduce it bit for bit. This script records what CPython returns
for a fixed, reproducible set of lists.

Some lists contain the Python int 0 (written as "0"): the planner's sums can contain int zeros
(cost.get(item, 0) * amount with an int amount), and the .NET port passes them as 0.0; the vectors check that this
gives the same result. The empty list sums to the int 0 (written as "0").

Usage (from the repository root):
    python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/generate_python_sum_vectors.py

The output file is written next to this script. The seed is fixed, so re-running the script with the same Python
version reproduces the committed file exactly.

File layout (UTF-8 JSON, LF line endings, one list per line):
    {
      "python": "<version of the generating interpreter>",
      "seed": <int>,
      "sums": [
        {"values": ["<repr(x)>", ...], "sum": "<repr(sum(values))>"},
        ...
      ]
    }
Every value is repr() of a Python float ("inf", "-inf" and "nan" for the non-finite values) or of the int 0.
"""

import json
import math
import os
import random
import sys

SEED = 20260929
RANDOM_LISTS = 1000


def handpicked():
    """Cancellation, signed zeros, non-finite values, overflow and int zeros."""
    inf, nan = math.inf, math.nan
    return [
        [],
        [0.1, 0.2, 0.3],
        [0.3, 0.2, 0.1],
        [1.0, 1e16, -1e16],
        [1e16, 1.0, -1e16],
        [1e16, 1.0, 1.0],
        [1e16, 1.0, 1.0, 1.0, 1.0],
        [1e100, 1.0, -1e100, 1e-100],
        [1e100, 1.0, -1e100],
        [2.0 ** 53, 1.0, 1.0],
        [0.1] * 10,
        [0.1] * 100,
        [1.0, -1.0],
        [-1.0, 1.0],
        [-1.5, 2.25, -0.75],
        [3.0, -1e-16, -3.0],
        [inf],
        [-inf],
        [inf, 1.0],
        [1.0, inf],
        [inf, -inf],
        [inf, inf],
        [-inf, -inf, 5.0],
        [nan],
        [1.0, nan],
        [nan, inf],
        [-0.0],
        [-0.0, -0.0],
        [0.0, -0.0],
        [-1.0, 1.0, -0.0],
        [1e308, 1e308],
        [1e308, 1e308, -1e308],
        [1.7976931348623157e308, 1.7976931348623157e308, -1.7976931348623157e308, -1.7976931348623157e308],
        [5e-324, 5e-324],
        [5e-324, -5e-324, 1.0],
        [0, 0.1, 0.2, 0.3],
        [0.1, 0, 0.2, 0, 0.3],
        [0, 0],
        [0, -0.0],
        [0, inf],
        [0, 1e16, 1.0, 0, 1.0],
        [1e16, 1.0, 0, 1.0],
    ]


def planner_like(rng):
    """Sums the planner does: resource costs times recipe amounts, credits, extraction rates, machines' worth."""
    costs = [1.0, 0.001, 2.0, 0.5, 1 / 3, 2 / 3, 0.1, 0.2, 1.25, 3.7777777777777777, 0.0005, 6.6000000000000005,
             7.333333333333333, 12.5, 0.26666666666666666]
    amounts = [1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 24, 25, 30, 40, 50, 60, 100, 0.5, 1.5, 2.5, 0.75, 7.5]
    out = []
    for _ in range(rng.randint(1, 8)):
        if rng.random() < 0.1:
            out.append(0 * rng.choice([a for a in amounts if isinstance(a, int)]))  # cost.get(i, 0) * int amount
        else:
            out.append(rng.choice(costs) * rng.randint(1, 9) / rng.choice([1, 3, 7, 9]) * rng.choice(amounts))
    return out


def random_list(rng):
    kind = rng.randrange(6)
    n = rng.randint(0, 40)
    if kind == 0:  # log-uniform magnitudes, mixed signs
        return [(-1 if rng.random() < 0.4 else 1) * 10 ** rng.uniform(-10, 12) for _ in range(n)]
    if kind == 1:  # heavy cancellation: big values and their negatives around small ones
        out = []
        for _ in range(max(1, n // 3)):
            big = 10 ** rng.uniform(8, 20)
            out += [big, rng.uniform(-10, 10), -big]
        rng.shuffle(out)
        return out
    if kind == 2:  # sums of tenths and hundredths
        return [rng.randint(-50, 99) / rng.choice([10, 100]) for _ in range(n)]
    if kind == 3:  # rates like amount * 60 / time, all positive
        return [rng.randint(1, 500) * 60 / rng.choice([0.5, 2, 3, 4, 6, 7, 9, 11, 13, 16, 22.5, 24, 36, 120])
                for _ in range(n)]
    if kind == 4:
        return planner_like(rng)
    # occasional non-finite values and int zeros in otherwise ordinary lists
    specials = [math.inf, -math.inf, math.nan, 0, -0.0, 0.0]
    return [rng.choice(specials) if rng.random() < 0.15 else rng.uniform(-1e6, 1e6) for _ in range(n)]


def main():
    rng = random.Random(SEED)
    lists = handpicked() + [random_list(rng) for _ in range(RANDOM_LISTS)]
    lines = [
        "{",
        f"  \"python\": {json.dumps(sys.version.split()[0])},",
        f"  \"seed\": {SEED},",
        "  \"sums\": [",
    ]
    entries = [json.dumps({"values": [repr(x) for x in values], "sum": repr(sum(values))}) for values in lists]
    lines += ["    " + e + ("," if i < len(entries) - 1 else "") for i, e in enumerate(entries)]
    lines += ["  ]", "}"]
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "python_sum_vectors.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print(f"wrote {len(lists)} sums to {path}")


if __name__ == "__main__":
    main()
