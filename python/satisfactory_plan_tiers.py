#!/usr/bin/env python3
"""
Satisfactory excess-production planner -- PER-TIER, RESOURCE-OPTIMISED version.

Walks the tiers in order. In each tier, every item that first becomes
automatable gets one full excess machine, using the recipe (vanilla or
alternate) that needs the fewest natural resources among the recipes unlocked
by that tier. Every item automated so far keeps at least one machine of excess
capacity. Machines built in earlier tiers stay as they are; spare capacity on
them (rounding headroom, earlier excess, leftover byproducts) is used before
any new machine is built. Output: the NEW machines per tier, plus running
totals.

Usage:
  python3 satisfactory_plan_tiers.py --data satisfactory_data.json --out tiers

  --data   location of the game data file (default: satisfactory_data.json
           next to where you run it; downloaded there if missing)
  --out    folder the output files are written to (created if missing;
           default: tiers)

Base behaviour (recipe costs, byproducts, disposal):

Same as satisfactory_plan.py, except for how each item's recipe is chosen.
Every vanilla and alternate recipe is considered, and each item gets the one
recipe that needs the fewest natural resources in total (ore, crude oil and
nitrogen count 1 per unit; water is nearly free), given the recipes already
chosen for its inputs. Costs are recalculated pass after pass until no
choice changes. Tiers are ignored when choosing recipes.

Given a set of target items, each target gets ONE full "excess" machine
(its recipe running at 100% clock). Everything that machine consumes is
supplied by additional upstream machines, all the way back to extractors.
Intermediate items are not stored: upstream machines are sized exactly to
demand (fractional counts are reported, plus the rounded-up count).

Data: greeny/SatisfactoryTools data.json (dev branch, game 1.0 recipes;
production recipes are unchanged in 1.2).

Usage:
  python3 satisfactory_plan_opt.py --targets "Steel Ingot"
  python3 satisfactory_plan_opt.py --targets "Steel Ingot,Iron Plate" --miner mk2 --purity pure
  python3 satisfactory_plan_opt.py --recipe "Steel Ingot=Alternate: Solid Steel Ingot" --targets "Steel Ingot"
"""
import argparse, json, math, os, sys, urllib.request
from collections import defaultdict

DATA_URL = "https://raw.githubusercontent.com/greeny/SatisfactoryTools/dev/data/data.json"
DATA_FILE = "satisfactory_data.json"

# Extractors (not described with rates in data.json, so defined here).
# rate = units per minute at 100% clock on a NORMAL node; power in MW.
MINERS = {"mk1": ("Miner Mk.1", 60, 5), "mk2": ("Miner Mk.2", 120, 15), "mk3": ("Miner Mk.3", 240, 45)}
PURITY = {"impure": 0.5, "normal": 1.0, "pure": 2.0}
WATER_EXTRACTOR = ("Water Extractor", 120, 20)
OIL_EXTRACTOR = ("Oil Extractor", 120, 40)          # normal node
NITROGEN_WELL = ("Resource Well (Nitrogen, normal satellite)", 60, 0)  # pressurizer power not modelled per satellite
NUCLEAR_MW = 2500          # Nuclear Power Plant output
NUCLEAR_WATER = 240        # m3/min per plant
UNSINKABLE = {"Desc_NuclearWaste_C", "Desc_PlutoniumWaste_C"}   # radioactive waste can't go in the AWESOME Sink
LEFTOVER_TOL = 0.01   # per-minute rounding noise below this is ignored
RESOURCE_WEIGHT = {"Desc_Water_C": 0.001}   # every other natural resource counts 1 per unit
SOLID_RESOURCES = {"Desc_OreIron_C", "Desc_Coal_C", "Desc_Sulfur_C", "Desc_SAM_C", "Desc_OreBauxite_C",
                   "Desc_OreGold_C", "Desc_OreCopper_C", "Desc_RawQuartz_C", "Desc_Stone_C", "Desc_OreUranium_C"}


def load_data(path=DATA_FILE):
    """Read the game data file. If it isn't there, download it to that path first."""
    if not os.path.exists(path):
        print(f"{path} not found; downloading {DATA_URL}")
        urllib.request.urlretrieve(DATA_URL, path)
    with open(path, encoding="utf-8") as f:
        return json.load(f)


class Undisposable(Exception):
    """A leftover fluid that nothing can turn into something sinkable."""


class Planner:
    def __init__(self, data, miner="mk1", purity="normal", overrides=None):
        self.no_credit = set()
        self.no_byproduct = set()   # fluids that must never be left over: avoid recipes making them as a byproduct
        self.d = data
        self.items = data["items"]
        self.buildings = data["buildings"]
        self.recipes = [r for r in data["recipes"].values() if r["inMachine"] and r["producedIn"]]
        for rod, waste, n_waste, label in (("Desc_NuclearFuelRod_C", "Desc_NuclearWaste_C", 50, "Uranium"),
                                           ("Desc_PlutoniumFuelRod_C", "Desc_PlutoniumWaste_C", 10, "Plutonium")):
            t = self.items[rod]["energyValue"] / NUCLEAR_MW          # seconds per rod
            self.recipes.append(dict(
                className=f"Gen_Nuclear_{label}", name=f"Burn {label} Fuel Rod", alternate=False,
                time=t, producedIn=["Desc_GeneratorNuclear_C"], buildingName="Nuclear Power Plant",
                ingredients=[{"item": rod, "amount": 1}, {"item": "Desc_Water_C", "amount": NUCLEAR_WATER * t / 60}],
                products=[{"item": waste, "amount": n_waste}], power=-NUCLEAR_MW))
        self.miner = MINERS[miner]
        self.purity = PURITY[purity]
        self.by_name = {v["name"].lower(): k for k, v in self.items.items()}
        self.overrides = {}  # item class -> recipe
        for item_name, recipe_name in (overrides or {}).items():
            ic = self.item_class(item_name)
            r = next((r for r in self.recipes if r["name"].lower() == recipe_name.lower()), None)
            if not r or not any(p["item"] == ic for p in r["products"]):
                sys.exit(f"Recipe '{recipe_name}' does not produce '{item_name}'")
            self.overrides[ic] = r
        self.automatable = self._automatable()

    def item_class(self, name):
        c = self.by_name.get(name.lower())
        if not c:
            sys.exit(f"Unknown item: {name}")
        return c

    def name(self, c):
        return self.items.get(c, {}).get("name", c)

    def _automatable(self):
        auto = {c for c in self.d["resources"] if self.extractor_for(c)}
        changed = True
        while changed:
            changed = False
            for r in self.recipes:
                if all(i["item"] in auto for i in r["ingredients"]):
                    for p in r["products"]:
                        if p["item"] not in auto:
                            auto.add(p["item"]); changed = True
        return auto

    # ---- resource cost of every item -------------------------------------------
    def _costs(self):
        """Natural resources needed per unit of each item, taking the cheapest
        recipe for everything (Bellman-Ford style relaxation, repeated until
        nothing changes). A multi-output recipe's other products are credited at
        their own cost, unless they are in self.no_credit (they'd only be sunk)."""
        INF = float("inf")
        cost = {c: RESOURCE_WEIGHT.get(c, 1.0) for c in self.automatable if self.extractor_for(c)}
        recs = [r for r in self.recipes if all(i["item"] in self.automatable for i in r["ingredients"])]
        for _ in range(500):
            changed = False
            for r in recs:
                cin = sum(cost.get(i["item"], INF) * i["amount"] for i in r["ingredients"])
                if cin == INF:
                    continue
                for p in r["products"]:
                    if p["item"] in cost and self.extractor_for(p["item"]):
                        continue
                    credit = sum(cost[q["item"]] * q["amount"] for q in r["products"]
                                 if q is not p and q["item"] in cost and q["item"] not in self.no_credit)
                    c = max(0.0, cin - credit) / p["amount"]
                    if c < cost.get(p["item"], INF) - 1e-9:
                        cost[p["item"]] = c
                        changed = True
            if not changed:
                break
        self.cost = cost
        return cost

    def recipe_cost(self, r, item):
        """Natural resources per unit of `item` when made with recipe r."""
        cost = self.cost
        cin = sum(cost.get(i["item"], float("inf")) * i["amount"] for i in r["ingredients"])
        p = next(x for x in r["products"] if x["item"] == item)
        credit = sum(cost.get(q["item"], 0) * q["amount"] for q in r["products"]
                     if q is not p and q["item"] not in self.no_credit)
        return max(0.0, cin - credit) / p["amount"]

    def _candidates(self, item):
        """Recipes that make `item` from automatable inputs, cheapest in natural
        resources first; ties go to vanilla, then to recipes where the item is the
        main product, unpackaging recipes last."""
        if not hasattr(self, "cost"):
            self._costs()
        cands = [r for r in self.recipes
                 if any(p["item"] == item for p in r["products"])
                 and all(i["item"] in self.automatable for i in r["ingredients"])
                 and not any(p["item"] in self.no_byproduct and p["item"] != item for p in r["products"])
                 and not r["name"].startswith("Unpackage")]   # unpackaging is logistics, not production
        nm = self.name(item).lower()
        cands.sort(key=lambda r: (round(self.recipe_cost(r, item), 6),
                                  r["alternate"],
                                  r["name"].startswith("Unpackage"),
                                  r["products"][0]["item"] != item,
                                  r["name"].lower().replace("alternate: ", "") != nm,
                                  len(r["ingredients"]), r["name"]))
        return cands

    def _choose_all(self):
        """Pick one recipe per item: the most-preferred candidate that does not
        make the item depend on itself through the other chosen recipes
        (e.g. Compacted Coal via Rocket Fuel, which needs Compacted Coal)."""
        items = [c for c in self.automatable if not self.extractor_for(c)]
        cands = {c: ([self.overrides[c]] if c in self.overrides else self._candidates(c)) for c in items}
        pick = {c: 0 for c in items if cands[c]}

        def ingredients(c):
            if self.extractor_for(c) or c not in pick:
                return []
            return [i["item"] for i in cands[c][pick[c]]["ingredients"]]

        def cyclic(c):
            seen, stack = set(), list(ingredients(c))
            while stack:
                x = stack.pop()
                if x == c:
                    return True
                if x in seen:
                    continue
                seen.add(x)
                stack += ingredients(x)
            return False

        for _ in range(100):
            changed = False
            for c in sorted(pick):
                while cyclic(c) and pick[c] + 1 < len(cands[c]):
                    pick[c] += 1
                    changed = True
            if not changed:
                break
        return {c: cands[c][k] for c, k in pick.items()}

    def recipe_for(self, item):
        if not hasattr(self, "_chosen"):
            self._chosen = self._choose_all()
        return self._chosen.get(item)

    def extractor_for(self, item):
        if item in getattr(self, "locked_raw", ()):
            return None
        if item in SOLID_RESOURCES:
            name, rate, mw = self.miner
            return name, rate * self.purity, mw
        if item == "Desc_Water_C":
            return WATER_EXTRACTOR
        if item == "Desc_LiquidOil_C":
            return OIL_EXTRACTOR
        if item == "Desc_NitrogenGas_C":
            return NITROGEN_WELL
        return None

    @staticmethod
    def per_min(amount, recipe, item):
        # fluids are stored in data as liters; show m3
        return amount * 60.0 / recipe["time"]

    def is_fluid(self, c):
        return self.items.get(c, {}).get("liquid", False)

    def qty(self, c, amount):
        return amount   # data.json already stores fluids in m3

    def optimise(self, targets):
        """Choose recipes, plan, and repeat if a byproduct that was credited as
        useful actually ends up in the sink (then it earns no credit)."""
        for _ in range(20):
            for a in ("cost", "_chosen"):
                if hasattr(self, a):
                    delattr(self, a)
            try:
                result = self.plan(targets)
            except Undisposable as e:
                self.no_byproduct.add(e.args[0])
                continue
            wasted = {c for c in list(self.sink) + list(self.disposal) if c not in self.no_credit}
            if not wasted:
                return result
            self.no_credit |= wasted
        return result

    def plan(self, targets):
        """Non-optimising solve. Each item is made only by its chosen recipe,
        which runs exactly enough to cover that item's shortfall; byproducts
        from other machines are credited first. Returns (rows, excess, surplus)."""
        excess = dict(getattr(self, "fixed_excess", {}))
        self.excess_recipe = dict(getattr(self, "fixed_excess_recipe", {}))
        for t in targets:
            if t in excess:
                continue
            r = self.recipe_for(t)
            if r is None:
                sys.exit(f"{self.name(t)} is not machine-producible")
            prod = next(p for p in r["products"] if p["item"] == t)
            excess[t] = prod["amount"] * 60 / r["time"]
            self.excess_recipe[t] = r
        demand = defaultdict(float, excess)
        runs = defaultdict(float)      # recipe className -> machines
        rec = {}
        extract = defaultdict(float)
        self.disposal = defaultdict(float)   # fluid -> m3/min turned into a sinkable solid
        for _ in range(100):
            self._converge(runs, rec, extract, demand)
            self._trim(runs, rec, extract, demand)
            # Fluid byproducts can't be sunk: turn any leftover fluid into a solid
            # (e.g. Heavy Oil Residue -> Petroleum Coke), then settle the new demand.
            bal = self._balance(runs, rec, extract, demand)
            left = {k: v for k, v in bal.items() if v > LEFTOVER_TOL and self.is_fluid(k)}
            if not left:
                break
            for fluid, q in left.items():
                r = self.disposal_recipe(fluid)
                rec[r["className"]] = r
                use = next(i for i in r["ingredients"] if i["item"] == fluid)
                runs[r["className"]] += q / (use["amount"] * 60 / r["time"])
                self.disposal[fluid] += q
        else:
            sys.exit("Byproduct disposal did not converge")
        rows = []
        for rc, n in runs.items():
            r = rec[rc]
            bcls = r["producedIn"][0]
            bname = r.get("buildingName") or self.buildings.get(bcls, {}).get("name", bcls)
            rows.append(dict(kind="machine", building=bname, recipe=r["name"], count=n,
                             mw=self._power(r, bcls, n), alternate=r["alternate"], cls=rc, r=r))
        for res, q in extract.items():
            name, rate, mw = self.extractor_for(res)
            rows.append(dict(kind="extractor", building=name, recipe=self.name(res), count=q / rate,
                             mw=mw * q / rate, rate=q))
        bal = self._balance(runs, rec, extract, demand)
        surplus = {k: v for k, v in bal.items() if v > LEFTOVER_TOL}
        bad = [self.name(k) for k in surplus if k in UNSINKABLE or self.is_fluid(k)]
        if bad:
            sys.exit("Left over but cannot be sunk: " + ", ".join(bad))
        self.sink = surplus            # everything left over goes to the AWESOME Sink
        return rows, excess, surplus

    def _converge(self, runs, rec, extract, demand):
        for _ in range(5000):
            bal = self._balance(runs, rec, extract, demand)
            short = {k: -v for k, v in bal.items() if v < -1e-9}
            if not short:
                return
            for item, q in short.items():
                if self.extractor_for(item):
                    extract[item] += q
                    continue
                # Spare capacity on machines built in earlier tiers comes first.
                for rc, have in getattr(self, "existing", {}).items():
                    r0 = self.existing_rec[rc]
                    if r0["products"][0]["item"] != item or q <= 1e-12:
                        continue
                    head = have - runs.get(rc, 0.0)
                    if head <= 1e-12:
                        continue
                    rate = r0["products"][0]["amount"] * 60 / r0["time"]
                    use = min(head, q / rate)
                    rec[rc] = r0
                    runs[rc] += use
                    q -= use * rate
                if q <= 1e-12:
                    continue
                r = self.recipe_for(item)
                if r is None:
                    sys.exit(f"No automatable source for {self.name(item)}")
                rec[r["className"]] = r
                prod = next(p for p in r["products"] if p["item"] == item)
                runs[r["className"]] += q / (prod["amount"] * 60 / r["time"])
        sys.exit("Did not converge")

    def _trim(self, runs, rec, extract, demand):
        """Use byproducts first: when an item is over-supplied because a byproduct
        arrived after its own machines were sized, remove the now-unneeded machines
        (e.g. Silica constructors once Alumina Solution refineries supply Silica)."""
        disposal_products = {p["item"] for f in self.disposal for p in self.disposal_recipe(f)["products"]}
        for _ in range(50):
            bal = self._balance(runs, rec, extract, demand)
            cut = False
            for item, q in bal.items():
                if q <= 1e-6 or item in disposal_products:
                    continue
                if self.extractor_for(item):          # over-extracted raw resource
                    d = min(extract.get(item, 0), q)
                    if d > 1e-9:
                        extract[item] -= d
                        cut = True
                    continue
                r = self.recipe_for(item)
                if r is None or r["className"] not in runs or r["products"][0]["item"] != item:
                    continue
                rate = r["products"][0]["amount"] * 60 / r["time"]
                d = min(runs[r["className"]], q / rate)
                if d > 1e-9:
                    runs[r["className"]] -= d
                    cut = True
            if not cut:
                return
            self._converge(runs, rec, extract, demand)

    def disposal_recipe(self, fluid):
        """Recipe that turns a leftover fluid into sinkable solids (alternates
        allowed here). Water may come out as well; its extraction is then
        reduced. Preference: single-input recipes, then packaging, then the
        fewest extra natural resources."""
        if not hasattr(self, "cost"):
            self._costs()
        def ok(p):
            return p["item"] == "Desc_Water_C" or (not self.is_fluid(p["item"]) and p["item"] not in UNSINKABLE)
        cands = [r for r in self.recipes
                 if "power" not in r
                 and any(i["item"] == fluid for i in r["ingredients"])
                 and all(ok(p) for p in r["products"])
                 and all(i["item"] in self.automatable for i in r["ingredients"])]
        def extra(r):
            return sum(self.cost.get(i["item"], 0) * i["amount"] for i in r["ingredients"] if i["item"] != fluid)
        cands.sort(key=lambda r: (len(r["ingredients"]) > 1, not r["name"].startswith("Packaged"),
                                  round(extra(r), 6), r["alternate"], r["name"]))
        if not cands:
            raise Undisposable(fluid)
        return cands[0]

    def _balance(self, runs, rec, extract, demand):
        bal = defaultdict(float)
        for rc, n in runs.items():
            r = rec[rc]
            f = n * 60 / r["time"]
            for p in r["products"]:
                bal[p["item"]] += p["amount"] * f
            for i in r["ingredients"]:
                bal[i["item"]] -= i["amount"] * f
        for res, q in extract.items():
            bal[res] += q
        for k, v in demand.items():
            bal[k] -= v
        return bal

    def _power(self, r, bcls, n):
        if "power" in r:                      # generator pseudo-recipe (negative = produces)
            return r["power"] * n
        b = self.buildings[bcls]
        if r.get("isVariablePower") and r["maxPower"] > 0:
            avg = (r["minPower"] + r["maxPower"]) / 2
            return avg * n
        return b["metadata"]["powerConsumption"] * n


# ---------------------------------------------------------------------------
# Production-order DAG
# ---------------------------------------------------------------------------
EXTRACTOR_BUILDING = {"Desc_Water_C": "Desc_WaterPump_C", "Desc_LiquidOil_C": "Desc_OilPump_C",
                      "Desc_NitrogenGas_C": "Desc_FrackingSmasher_C"}
# The data file lists the Smelter under "Basic Steel Production" (Tier 3), but in the
# game the Smelter is available from Tier 0 onboarding.
TIER_OVERRIDES = {"Recipe_SmelterMk1_C": 0}
TIER_NAMES = {0: "Tier 0 - Onboarding", 1: "Tier 1", 2: "Tier 2", 3: "Tier 3", 4: "Tier 4",
              5: "Tier 5", 6: "Tier 6", 7: "Tier 7", 8: "Tier 8", 9: "Tier 9"}


def unlock_tiers(p):
    """Earliest progression tier at which each recipe (className) becomes available.
    Milestones carry their own tier. MAM research and alternate-recipe hard drives
    have no tier, so they take the tier of the items needed to research them."""
    S = p.d["schematics"]
    item_tier = {c: 0 for c in p.d["resources"]}
    rec_tier = {}
    sch_tier = {}
    recipes = {r["className"]: r for r in p.d["recipes"].values()}
    for _ in range(50):
        changed = False
        for sc, s in S.items():
            if s["type"] not in ("EST_Milestone", "EST_Tutorial", "EST_MAM", "EST_Alternate", "EST_HardDrive"):
                continue
            if s["type"] in ("EST_Milestone", "EST_Tutorial"):
                t = s["tier"]
            else:                                  # MAM research / hard drives need a MAM
                t = rec_tier.get("Recipe_Mam_C")
                if t is None:
                    continue
            deps = [sch_tier.get(q) for q in s.get("requiredSchematics", [])]
            costs = [item_tier.get(c["item"], 0) for c in s["cost"]]   # gathered items count as tier 0
            if any(x is None for x in deps):
                continue
            t = max([t] + deps + costs)
            if sch_tier.get(sc) != t:
                sch_tier[sc] = t; changed = True
            for rc in s["unlock"]["recipes"]:
                if rc not in rec_tier or t < rec_tier[rc]:
                    rec_tier[rc] = t; changed = True
        for rc, t in rec_tier.items():
            r = recipes.get(rc)
            if not r:
                continue
            ins = [item_tier.get(i["item"]) for i in r["ingredients"]]
            if any(x is None for x in ins):
                continue
            eff = max([t] + ins)
            for pr in r["products"]:
                if item_tier.get(pr["item"], 99) > eff:
                    item_tier[pr["item"]] = eff; changed = True
        if not changed:
            break
    rec_tier.update(TIER_OVERRIDES)
    return rec_tier


def building_tier(p, rec_tier, bcls):
    rc = "Recipe_" + bcls[len("Desc_"):]
    return rec_tier.get(rc, 0)


def build_dag(p, rows, targets):
    """Nodes: every item the plan produces, plus raw resources.
    Edges: ingredient -> product, using each item's chosen recipe."""
    rec_tier = unlock_tiers(p)
    machines = {r["cls"]: r for r in rows if r["kind"] == "machine"}
    raw_rate = {p.item_class(r["recipe"]): r["rate"] for r in rows if r["kind"] == "extractor"}
    produced = {}
    for rc, row in machines.items():
        r = next(x for x in p.recipes if x["className"] == rc)
        for c, rr in p._chosen.items():
            if rr["className"] == rc:
                produced[c] = r
    nodes = {}
    def tier_of(c, stack=()):
        if c in nodes:
            return nodes[c]["tier"]
        if p.extractor_for(c):
            b = EXTRACTOR_BUILDING.get(c, "Desc_MinerMk1_C")
            nodes[c] = dict(raw=True, tier=building_tier(p, rec_tier, b), depth=0, inputs=[], upstream=set())
            return nodes[c]["tier"]
        r = produced[c]
        ins = [i["item"] for i in r["ingredients"]]
        for i in ins:
            tier_of(i)
        own = rec_tier.get(r["className"], 0)
        bt = building_tier(p, rec_tier, r["producedIn"][0])
        up = set(ins)
        for i in ins:
            up |= nodes[i]["upstream"]
        nodes[c] = dict(raw=False, recipe=r, inputs=ins,
                        tier=max([own, bt] + [nodes[i]["tier"] for i in ins]),
                        depth=1 + max([nodes[i]["depth"] for i in ins] or [0]),
                        upstream=up, machines=machines[r["className"]], excess=c in targets)
        return nodes[c]["tier"]
    for c in produced:
        tier_of(c)
    # A raw resource appears in the tier where something first consumes it.
    for c, n in nodes.items():
        if n["raw"]:
            n["tier"] = min((m["tier"] for m in nodes.values() if c in m["inputs"]), default=n["tier"])
    for c, n in nodes.items():
        if n["raw"]:
            n["rate"] = raw_rate.get(c, 0)
    return nodes


def is_shared_byproduct(p, c, n):
    """True when this item comes out of machines already listed under another item
    (e.g. Heavy Oil Residue from the Plastic refineries)."""
    main = n["recipe"]["products"][0]["item"]
    return main != c and p.recipe_for(main) is n["recipe"]


def build_order(p, nodes):
    """Within each tier: fewest production steps first, then fewest distinct
    upstream parts, then name. This is also a valid topological order."""
    made = [c for c, n in nodes.items() if not n["raw"]]
    made.sort(key=lambda c: (nodes[c]["tier"], nodes[c]["depth"], len(nodes[c]["upstream"]),
                             is_shared_byproduct(p, c, nodes[c]), p.name(c)))
    pos = {c: k for k, c in enumerate(made)}
    for c in made:                                   # sanity: inputs always come earlier
        for i in nodes[c]["inputs"]:
            assert nodes[i]["raw"] or pos[i] < pos[c], (p.name(i), p.name(c))
    return made


def sink_notes(p, c):
    """Text flags for items that go to the AWESOME Sink, and for machines that exist
    only to turn a leftover fluid byproduct into something sinkable."""
    notes = []
    for fluid, q in getattr(p, "disposal", {}).items():
        r = p.disposal_recipe(fluid)
        if any(x["item"] == c for x in r["products"]):
            use = next(i for i in r["ingredients"] if i["item"] == fluid)
            n = q / (use["amount"] * 60 / r["time"])
            notes.append(f"{math.ceil(n - 1e-9)} of these machines turn leftover {p.name(fluid)} into {p.name(c)} for the sink")
    q = getattr(p, "sink", {}).get(c)
    if q:
        notes.append(f"SINK {q:,.1f}/min (cannot all be used)")
    return notes


def write_order(p, nodes, order, path):
    lines, tier, step = [], None, 0
    for c in order:
        n = nodes[c]
        if n["tier"] != tier:
            tier = n["tier"]
            raws = sorted(p.name(r) for r, m in nodes.items() if m["raw"] and m["tier"] == tier)
            lines.append(f"\n=== {TIER_NAMES.get(tier, f'Tier {tier}')} ===")
            if raws:
                lines.append(f"    new raw inputs: {', '.join(raws)}")
        step += 1
        m = n["machines"]
        build = math.ceil(m["count"] - 1e-9)
        rec = n["recipe"]["name"]
        alt = "  [ALTERNATE]" if n["recipe"]["alternate"] else ""
        ins = ", ".join(p.name(i) for i in n["inputs"]) or "(no inputs)"
        if is_shared_byproduct(p, c, n):
            main = p.name(n["recipe"]["products"][0]["item"])
            lines.append(f"{step:4d}. {p.name(c):30s} {'':4s}   byproduct of the {main} machines ({m['building']}){alt}")
        else:
            lines.append(f"{step:4d}. {p.name(c):30s} {build:4d} x {m['building']:22s} <- {ins}{alt}")
        for note in sink_notes(p, c):
            lines.append(f"{'':8s}-> {note}")
    sink = getattr(p, "sink", {})
    if sink:
        lines.append("\n=== AWESOME Sink ===")
        lines.append("    Byproducts that cannot all be used; route each to the AWESOME Sink:")
        for c, q in sorted(sink.items(), key=lambda kv: -kv[1]):
            lines.append(f"      {p.name(c):30s} {q:10,.1f} /min")
    with open(path, "w") as f:
        f.write("PRODUCTION ORDER (build top to bottom; counts include the one excess machine)\n")
        f.write("\n".join(lines) + "\n")


def write_csv(p, nodes, order, path):
    """Flat topological sort: every item appears after all of its inputs."""
    import csv
    with open(path, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["step", "tier", "item", "machines", "building", "recipe", "alternate", "excess",
                    "sink_per_min", "notes", "inputs"])
        for k, c in enumerate(order, 1):
            n = nodes[c]
            m = n["machines"]
            shared = is_shared_byproduct(p, c, n)
            w.writerow([k, n["tier"], p.name(c), 0 if shared else math.ceil(m["count"] - 1e-9),
                        m["building"], n["recipe"]["name"] + (" (byproduct)" if shared else ""),
                        "yes" if n["recipe"]["alternate"] else "", "yes" if n["excess"] else "",
                        round(getattr(p, "sink", {}).get(c, 0), 2) or "",
                        "; ".join(x for x in sink_notes(p, c) if not x.startswith("SINK")),
                        "; ".join(p.name(i) for i in n["inputs"])])


def write_dot(p, nodes, order, path):
    palette = ["#e8e8e8", "#fde2c8", "#fff1b8", "#d9f2c9", "#c9ecef", "#cfdcf7",
               "#e0d4f5", "#f7d4e6", "#f5cccc", "#d6d6d6"]
    out = ["digraph production {", '  rankdir=LR; newrank=true; node [shape=box, style="rounded,filled", fontname="Helvetica", fontsize=10];',
           '  edge [color="#888888", arrowsize=0.6];']
    step = {c: k + 1 for k, c in enumerate(order)}
    tiers = sorted({n["tier"] for n in nodes.values()})
    for t in tiers:
        out.append(f'  subgraph cluster_t{t} {{ label="{TIER_NAMES.get(t, t)}"; style=filled; color="{palette[t % 10]}"; fontsize=16;')
        for c, n in nodes.items():
            if n["tier"] != t:
                continue
            if n["raw"]:
                out.append(f'    "{c}" [label="{p.name(c)}\\n{n["rate"]:.0f}/min", shape=ellipse, fillcolor="#ffffff"];')
            else:
                m = n["machines"]
                build = math.ceil(m["count"] - 1e-9)
                alt = " (alt)" if n["recipe"]["alternate"] else ""
                how = (f'from the {build} x {m["building"]} making {p.name(n["recipe"]["products"][0]["item"])}'
                       if is_shared_byproduct(p, c, n) else f'{build} x {m["building"]}{alt}')
                extra = "".join("\\n" + x.replace("SINK", "to sink:") for x in sink_notes(p, c) if not x.startswith("SINK"))
                out.append(f'    "{c}" [label="#{step[c]} {p.name(c)}\\n{how}{extra}", fillcolor="#ffffff"'
                           + (', penwidth=2' if n["excess"] else '') + '];')
        out.append("  }")
    for c, n in nodes.items():
        for i in n["inputs"]:
            out.append(f'  "{i}" -> "{c}";')
    sink = getattr(p, "sink", {})
    if sink:
        out.append('  "AWESOME_SINK" [label="AWESOME Sink", shape=doubleoctagon, style=filled, fillcolor="#ffd6d6", fontsize=14];')
        for c, q in sink.items():
            out.append(f'  "{c}" -> "AWESOME_SINK" [color="#cc3333", fontcolor="#cc3333", style=dashed, label="{q:,.0f}/min"];')
    out.append("}")
    with open(path, "w") as f:
        f.write("\n".join(out) + "\n")


def report(p, rows, excess, surplus, extractors=False):
    print("EXCESS OUTPUT (one full machine each)")
    for c, q in excess.items():
        print(f"  {p.name(c):35s} {q:8.2f} /min")
    print("\nMACHINES")
    print(f"  {'Building':28s} {'Recipe / resource':38s} {'Exact':>7s} {'Build':>5s} {'MW@100%':>8s}")
    total_mw = 0
    total_gen = 0
    total_n = 0
    raw = [r for r in rows if r["kind"] == "extractor"]
    if not extractors:
        rows = [r for r in rows if r["kind"] != "extractor"]
    for row in sorted(rows, key=lambda r: (r["kind"] != "extractor", r["building"], r["recipe"])):
        n = math.ceil(row["count"] - 1e-9)
        mw = row["mw"] / row["count"] * n   # whole machines, all at 100% clock
        if mw < 0:
            total_gen -= mw
        else:
            total_mw += mw
        total_n += n
        print(f"  {row['building']:28s} {row['recipe']:38s} {row['count']:7.3f} {n:5d} {mw:8.1f}")
    print(f"\n  Total: {total_n} machines, {total_mw:,.1f} MW consumed at 100% clock"
          + (f", {total_gen:,.0f} MW generated by nuclear plants" if total_gen else "") + ("" if extractors else " (extractors excluded)"))
    if not extractors and raw:
        print("\nRAW RESOURCE INPUT")
        for r in sorted(raw, key=lambda r: r["recipe"]):
            print(f"  {r['recipe']:35s} {r['rate']:8.2f} /min")
    if surplus:
        print("\nSURPLUS BYPRODUCTS (not consumed)")
        for c, q in surplus.items():
            print(f"  {p.name(c):35s} {q:8.2f} /min")




# ---------------------------------------------------------------------------
# Per-tier driver
# ---------------------------------------------------------------------------
class TierPlanner(Planner):
    def __init__(self, data, **kw):
        super().__init__(data, **kw)
        self.all_recipes = list(self.recipes)
        self.rec_tier = unlock_tiers(self)
        self.existing, self.existing_rec = {}, {}
        self.fixed_excess, self.fixed_excess_recipe = {}, {}

    def recipe_tier(self, r):
        return max(self.rec_tier.get(r["className"], 0),
                   building_tier(self, self.rec_tier, r["producedIn"][0]))

    def raw_tier(self, c):
        return building_tier(self, self.rec_tier, EXTRACTOR_BUILDING.get(c, "Desc_MinerMk1_C"))

    def set_tier(self, t):
        """Only recipes, machines and extractors unlocked by tier t."""
        self.tier = t
        self.locked_raw = set()
        self.locked_raw = {c for c in self.d["resources"] if self.raw_tier(c) > t}
        self.recipes = [r for r in self.all_recipes if self.recipe_tier(r) <= t]
        self.automatable = self._automatable()
        self.no_credit, self.no_byproduct = set(), set()
        for a in ("cost", "_chosen"):
            if hasattr(self, a):
                delattr(self, a)


def run_tiers(p, include_ingots=False):
    results = []
    before = set()
    prev_runs = defaultdict(float)
    for t in range(0, 10):
        p.set_tier(t)
        new_items = sorted(c for c in p.automatable - before
                           if c not in p.d["resources"] and c not in UNSINKABLE
                           and (include_ingots or "Ingot" not in c) and p.recipe_for(c))
        targets = sorted(set(p.fixed_excess) | set(new_items))
        rows, excess, surplus = p.optimise(targets)
        runs = {r["cls"]: r["count"] for r in rows if r["kind"] == "machine"}
        recs = {r["cls"]: r["r"] for r in rows if r["kind"] == "machine"}
        new = {}
        for rc, n in runs.items():
            k = math.ceil(n - p.existing.get(rc, 0) - 1e-6)
            if k > 0:
                new[rc] = k
        reused = defaultdict(float)
        for rc, n in runs.items():
            gain = min(n, p.existing.get(rc, 0)) - prev_runs.get(rc, 0)
            if gain > 1e-6:
                reused[recs[rc]["products"][0]["item"]] += gain
        raw = {p.item_class(r["recipe"]): r["rate"] for r in rows if r["kind"] == "extractor"}
        results.append(dict(tier=t, new_items=new_items, new=new, recs=recs, runs=runs,
                            existing=dict(p.existing), excess={c: excess[c] for c in new_items},
                            excess_recipe={c: p.excess_recipe[c] for c in new_items},
                            reused=dict(reused), raw=raw, sink=dict(p.sink),
                            disposal={f: (q, p.disposal_recipe(f)) for f, q in p.disposal.items()}))
        # Lock this tier in: machines stay built, excess targets stay required.
        for rc, k in new.items():
            p.existing[rc] = p.existing.get(rc, 0) + k
            p.existing_rec[rc] = recs[rc]
        for c in new_items:
            p.fixed_excess[c] = excess[c]
            p.fixed_excess_recipe[c] = p.excess_recipe[c]
        prev_runs = defaultdict(float, runs)
        before |= p.automatable
    return results


def tier_order(p, res):
    """Topological order of this tier's new machines: a recipe comes after every
    recipe (built this tier) that makes one of its inputs; simplest first."""
    new = res["new"]
    recs = res["recs"]
    maker = {}
    for rc in new:
        maker.setdefault(recs[rc]["products"][0]["item"], rc)
    depth = {}
    def d(rc, stack=()):
        if rc in depth:
            return depth[rc]
        if rc in stack:
            return 0
        deps = [maker[i["item"]] for i in recs[rc]["ingredients"] if i["item"] in maker and maker[i["item"]] != rc]
        depth[rc] = 1 + max([d(x, stack + (rc,)) for x in deps] or [0])
        return depth[rc]
    order = sorted(new, key=lambda rc: (d(rc), len(recs[rc]["ingredients"]), p.name(recs[rc]["products"][0]["item"]), recs[rc]["name"]))
    return order, maker


def power(p, r, n):
    return p._power(r, r["producedIn"][0], n)


def write_tier(p, res, out):
    import csv, shutil, subprocess
    t = res["tier"]
    title = TIER_NAMES.get(t, f"Tier {t}")
    order, maker = tier_order(p, res)
    step = {rc: k + 1 for k, rc in enumerate(order)}
    recs = res["recs"]
    disposal_by_recipe = {r["className"]: (f, q) for f, (q, r) in res["disposal"].items()}

    def src(item):
        if item in maker:
            return f"{p.name(item)} (#{step[maker[item]]})"
        if p.extractor_for(item):
            return p.name(item)
        return f"{p.name(item)} (bus)"

    L = [f"{title.upper()} - NEW CAPACITY (build top to bottom; whole machines at 100% clock)", ""]
    L.append("New items this tier (one excess machine each):")
    for c in sorted(res["new_items"], key=p.name):
        r = res["excess_recipe"][c]
        L.append(f"    {p.name(c):35s} {res['excess'][c]:8.2f}/min excess  via {r['name']}"
                 + ("  [ALTERNATE]" if r["alternate"] else ""))
    L += ["", "Build order:"]
    total_new, mw, gen = 0, 0.0, 0.0
    rows_csv = []
    for rc in order:
        r = recs[rc]
        k = res["new"][rc]
        total_new += k
        w = power(p, r, k)
        if w < 0:
            gen -= w
        else:
            mw += w
        item = r["products"][0]["item"]
        b = r.get("buildingName") or p.buildings[r["producedIn"][0]]["name"]
        alt = "  [ALTERNATE]" if r["alternate"] else ""
        L.append(f"{step[rc]:4d}. {p.name(item):30s} {k:4d} x {b:22s} {r['name']}{alt}")
        ins = ", ".join(src(i["item"]) for i in r["ingredients"]) or "(no inputs)"
        L.append(f"          inputs: {ins}")
        byp = [q["item"] for q in r["products"][1:]]
        if byp:
            L.append(f"          byproducts: {', '.join(p.name(x) for x in byp)}")
        note = ""
        if rc in disposal_by_recipe:
            f, q = disposal_by_recipe[rc]
            note = f"turns leftover {p.name(f)} ({q:,.1f}/min) into something sinkable"
            L.append(f"          -> {note}")
        rows_csv.append([step[rc], p.name(item), k, b, r["name"], "yes" if r["alternate"] else "",
                         "yes" if item in res["new_items"] else "", note,
                         "; ".join(src(i["item"]) for i in r["ingredients"]),
                         "; ".join(p.name(x) for x in byp), round(w, 1)])
    L += ["", f"New machines this tier: {total_new}   power used: {mw:,.1f} MW at 100% clock"
          + (f"   power generated: {gen:,.0f} MW (nuclear)" if gen else "")]
    if res["reused"]:
        L += ["", "Spare capacity from earlier tiers now put to work (machines' worth):"]
        for c, q in sorted(res["reused"].items(), key=lambda kv: -kv[1]):
            L.append(f"    {p.name(c):35s} {q:8.2f}")
    if res["sink"]:
        L += ["", "To the AWESOME Sink (byproducts not used, plus stored excess is sunk at the far end of the bus):"]
        for c, q in sorted(res["sink"].items(), key=lambda kv: -kv[1]):
            L.append(f"    {p.name(c):35s} {q:10,.2f}/min")
    L += ["", "Raw input for the whole bus after this tier:"]
    for c, q in sorted(res["raw"].items(), key=lambda kv: p.name(kv[0])):
        L.append(f"    {p.name(c):35s} {q:10,.2f}/min")
    base = os.path.join(out, f"tier_{t}")
    with open(base + "_order.txt", "w") as f:
        f.write("\n".join(L) + "\n")
    with open(base + "_order.csv", "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["step", "item", "new_machines", "building", "recipe", "alternate", "excess_target",
                    "sink_disposal", "inputs", "byproducts", "mw"])
        w.writerows(rows_csv)

    # Graph: this tier's new machines; earlier-tier items and raw inputs as grey ovals.
    D = ["digraph tier {", '  rankdir=LR; node [shape=box, style="rounded,filled", fontname="Helvetica", fontsize=10, fillcolor="#ffffff"];',
         '  edge [color="#888888", arrowsize=0.6];',
         f'  labelloc=t; label="{title}: {total_new} new machines"; fontsize=18;']
    ext = set()
    for rc in order:
        r = recs[rc]
        item = r["products"][0]["item"]
        b = r.get("buildingName") or p.buildings[r["producedIn"][0]]["name"]
        hl = ', penwidth=2.5, color="#2a7a2a"' if item in res["new_items"] else ""
        D.append(f'  "{rc}" [label="#{step[rc]} {p.name(item)}\\n{res["new"][rc]} x {b}\\n{r["name"]}"{hl}];')
        for i in r["ingredients"]:
            if i["item"] in maker:
                D.append(f'  "{maker[i["item"]]}" -> "{rc}";')
            else:
                ext.add(i["item"])
                D.append(f'  "in_{i["item"]}" -> "{rc}";')
    for c in ext:
        lab = "raw" if p.extractor_for(c) else "bus"
        D.append(f'  "in_{c}" [label="{p.name(c)}\\n({lab})", shape=ellipse, fillcolor="#eeeeee"];')
    if res["sink"]:
        D.append('  "SINK" [label="AWESOME Sink", shape=doubleoctagon, fillcolor="#ffd6d6"];')
        for c, q in res["sink"].items():
            src_id = maker.get(c)
            node = f'"{src_id}"' if src_id else f'"in_{c}"'
            if not src_id and c not in ext:
                D.append(f'  "in_{c}" [label="{p.name(c)}\\n(bus)", shape=ellipse, fillcolor="#eeeeee"];')
                ext.add(c)
            D.append(f'  {node} -> "SINK" [color="#cc3333", style=dashed, label="{p.name(c)} {q:,.0f}/min", fontcolor="#cc3333"];')
    D.append("}")
    with open(base + ".dot", "w") as f:
        f.write("\n".join(D) + "\n")
    if shutil.which("dot"):
        subprocess.run(["dot", "-Tsvg", base + ".dot", "-o", base + ".svg"], check=True)
    return total_new, mw, gen


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default=DATA_FILE, help="path to the game data file (downloaded there if missing)")
    ap.add_argument("--out", default="tiers", help="folder for the output files (created if missing)")
    ap.add_argument("--miner", default="mk1", choices=MINERS)
    ap.add_argument("--purity", default="normal", choices=PURITY)
    ap.add_argument("--include-ingots", action="store_true")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    p = TierPlanner(load_data(a.data), miner=a.miner, purity=a.purity)
    results = run_tiers(p, a.include_ingots)
    S = ["TIER SUMMARY (machines rounded up, 100% clock; extractors excluded; raw excludes water)", "",
         f"{'Tier':22s} {'new items':>9s} {'new machines':>12s} {'alts':>5s} {'reused':>7s} {'new MW':>10s} {'raw +/min':>10s} | "
         f"{'total machines':>14s} {'total MW':>10s} {'nuclear MW':>10s} {'total raw/min':>13s}",
         "  (reused = machines' worth of spare capacity from earlier tiers put to work;"
         " MW = power used; nuclear MW = power generated by the plants built so far)", ""]
    tot_n, tot_mw, tot_gen, prev_raw = 0, 0.0, 0.0, 0.0
    for res in results:
        n, mw, gen = write_tier(p, res, a.out)
        tot_n += n
        tot_mw += mw
        tot_gen += gen
        raw = sum(q for c, q in res["raw"].items() if c != "Desc_Water_C")
        alts = sum(1 for rc in res["new"] if res["recs"][rc]["alternate"])
        S.append(f"{TIER_NAMES.get(res['tier']):22s} {len(res['new_items']):9d} {n:12d} {alts:5d} "
                 f"{sum(res['reused'].values()):7.1f} {mw:10,.0f} {raw - prev_raw:10,.0f} | "
                 f"{tot_n:14d} {tot_mw:10,.0f} {tot_gen:10,.0f} {raw:13,.0f}")
        prev_raw = raw
    with open(os.path.join(a.out, "summary.txt"), "w") as f:
        f.write("\n".join(S) + "\n")
    print("\n".join(S))


if __name__ == "__main__":
    main()
