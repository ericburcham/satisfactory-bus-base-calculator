"""Generate python_number_vectors.json: Python's own float formatting, round() and repr() results.

The .NET port must reproduce Python's number formatting byte for byte. This script records what CPython prints
for a fixed, reproducible set of doubles so that the NUnit tests can check PythonNumerics / PythonNumberFormat
against the real thing.

Usage (from the repository root):
    python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/generate_python_vectors.py

The output file is written next to this script. The seed is fixed, so re-running the script with the same Python
version reproduces the committed file exactly.

File layout (UTF-8 JSON, LF line endings, one value per line):
    {
      "python": "<version of the generating interpreter>",
      "seed": <int>,
      "formatSpecs": [".0f", ...],        # float format specs, f"{x:<spec>}"
      "roundDigits": [0, 1, ...],         # ndigits for round(x, ndigits); outputs are repr(round(x, ndigits))
      "intSpecs": ["4d", ...],            # int format specs, f"{n:<spec>}"
      "floats": [                          # one entry per double
        {"x": "<repr(x)>", "format": [...one per formatSpecs...], "round": [...one per roundDigits...]},
        ...
      ],
      "ints": [{"n": <int>, "format": [...one per intSpecs...]}, ...]
    }
"x" is repr(x), which round-trips exactly ("inf", "-inf" and "nan" for the non-finite values), so it is also the
expected repr() output.
"""

import json
import math
import os
import random
import struct
import sys

SEED = 20260928

FORMAT_SPECS = [
    ".0f", ".1f", ".2f", ".3f", ",.0f", ",.1f", ",.2f",
    "8.2f", "10,.2f", "7.1f", "7.3f", "8.1f", "10,.0f", "10,.1f", "13,.0f",
]
ROUND_DIGITS = [0, 1, 2, 6]
INT_SPECS = ["d", "4d", "5d", "9d", "12d", "14d"]


def handpicked():
    """Edge cases: ties, near ties, signed zeros, tiny and huge values, non-finite values, thousands boundaries."""
    ties = [0.5, 1.5, 2.5, -2.5, 3.5, -0.5, 0.125, 0.375, 0.625, -0.125, 2.675, 1.005, 1.015, 1.025, 0.045,
            1234.5, 12345.25, 12345.75, 0.25, 0.75, 2.25, 0.05, 0.15, 0.35, 0.95, 9.995, 99.995, 0.0005,
            1.0000005, 2.0000005, 0.1234565, 5e-7, 1.5e-6, 2.5e-6]
    near = []
    for t in [0.5, 1.5, 2.5, 0.125, 1234.5, 999.5, 0.05, 0.25]:
        near += [math.nextafter(t, math.inf), math.nextafter(t, -math.inf)]
    zeros = [0.0, -0.0, -0.001, -1e-12, -0.004, -0.04, -0.05, -0.049999, -0.4, -0.5, -0.51, 0.001, 1e-12,
             -0.0000004, -0.00000051]
    big = [1e15, 1e16, 1e17, 123456789.987, 1e22, 1e23, 9007199254740992.0, 9007199254740993.0,
           1.7976931348623157e308, -1e22, 123456789012345678.0, 1.5e16, 9999999999999998.0,
           99999999999999.99, 4.35e15]
    tiny = [5e-324, -5e-324, 2.2250738585072014e-308, 1e-7, 1e-5, 1e-4, 0.0001, 0.00012, 1.5e-5, 1e-300,
            123e-20]
    special = [math.inf, -math.inf, math.nan]
    thousands = [999.5, 999.4, 999.49, 999.95, 999.995, 1000.0, 999999.5, 999999.4, -999.5, -999999.5,
                 -1234567.891, 1234567.891, 12345678.9, 100000.0, 1000000.0, -1000.0, 9999.5]
    integral = [16.0, -30000.0, 4.0, 165.0, 1000.0, 2500.0, -2500.0, 1.0, 10.0, 100.0, 1e6, 12.0, 45.0,
                16.5, 0.1, 0.2, 0.3, 0.1 + 0.2, 1 / 3, 2 / 3, 22.5, 7.5, 37.5, 112.5, 3.14159, 2.71828]
    return ties + near + zeros + big + tiny + special + thousands + integral


def pseudo_random(rng):
    """About 2000 reproducible values across magnitudes 1e-8 .. 1e9, plus arithmetic that the planner performs."""
    out = []
    for _ in range(700):  # log-uniform magnitudes, both signs
        x = 10 ** rng.uniform(-8, 9)
        out.append(-x if rng.random() < 0.25 else x)
    for _ in range(250):  # multiples of 1/8 (exact binary ties)
        out.append(rng.randint(-80000, 800000) / 8)
    for _ in range(250):  # hundredths (inexact decimal ties)
        out.append(rng.randint(-100000, 10000000) / 100)
    for _ in range(200):  # sums of tenths (accumulated binary error)
        s = 0.0
        for _ in range(rng.randint(1, 60)):
            s += 0.1 * rng.randint(1, 9)
        out.append(s)
    for _ in range(200):  # rates like amount * 60 / time
        out.append(rng.randint(1, 500) * 60 / rng.choice([0.5, 1, 2, 3, 4, 6, 8, 10, 12, 16, 20, 24, 30, 40, 48, 60,
                                                          7, 9, 11, 13, 14, 22.5, 32, 36, 120]))
    for _ in range(200):  # values just around x.5, x.05, x.005 ties at scale 10^-d
        d = rng.randint(0, 3)
        k = rng.randint(0, 1000000)
        t = (k + 0.5) / 10 ** d
        out.append(rng.choice([t, math.nextafter(t, math.inf), math.nextafter(t, -math.inf)]))
    for _ in range(150):  # machine counts: n / k products
        out.append(rng.randint(1, 5000) / rng.randint(1, 997) * rng.choice([1, 0.5, 2.5, 60, 0.1]))
    for _ in range(50):  # arbitrary bit patterns with moderate exponents (non-finite ones skipped)
        while True:
            bits = rng.getrandbits(64)
            x = struct.unpack("<d", struct.pack("<Q", bits))[0]
            if math.isfinite(x) and 1e-30 < abs(x) < 1e30:
                out.append(x)
                break
    return out


def entry(x):
    return {
        "x": repr(x),
        "format": [format(x, spec) for spec in FORMAT_SPECS],
        "round": [repr(round(x, n)) for n in ROUND_DIGITS],
    }


def main():
    rng = random.Random(SEED)
    floats = handpicked() + pseudo_random(rng)
    ints = [0, 1, -1, 4, 9, 10, 99, 999, 1000, 9999, 12345, -12345, 123456789, 1234567890, -1234567890,
            99999999999999, 2 ** 62, -(2 ** 63)]
    ints += [rng.randint(-10 ** rng.randint(1, 15), 10 ** rng.randint(1, 15)) for _ in range(200)]
    header = {
        "python": sys.version.split()[0],
        "seed": SEED,
        "formatSpecs": FORMAT_SPECS,
        "roundDigits": ROUND_DIGITS,
        "intSpecs": INT_SPECS,
    }
    sections = {
        "floats": [entry(x) for x in floats],
        "ints": [{"n": n, "format": [format(n, spec) for spec in INT_SPECS]} for n in ints],
    }
    lines = ["{"]
    for key, value in header.items():
        lines.append(f"  {json.dumps(key)}: {json.dumps(value)},")
    for index, (key, items) in enumerate(sections.items()):
        lines.append(f"  {json.dumps(key)}: [")
        lines += ["    " + json.dumps(item) + ("," if i < len(items) - 1 else "") for i, item in enumerate(items)]
        lines.append("  ]" + ("," if index < len(sections) - 1 else ""))
    lines.append("}")
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "python_number_vectors.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print(f"wrote {len(floats)} floats and {len(ints)} ints to {path}")


if __name__ == "__main__":
    main()
