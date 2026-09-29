using System.Collections.Frozen;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Configuration;

/// <summary>
/// The constants of the Python reference (<c>satisfactory_plan_tiers.py</c>): URLs, extractor specs, well-known ids,
/// tier names, string markers, tolerances and loop caps. Collections are frozen.
/// </summary>
public static class PlannerConstants
{
    // ---- data file ---------------------------------------------------------------------------------------------

    /// <summary>Where the game data file is downloaded from when it is missing (Python <c>DATA_URL</c>).</summary>
    public const string DataUrl = "https://raw.githubusercontent.com/greeny/SatisfactoryTools/dev/data/data.json";

    /// <summary>The data file's name (Python <c>DATA_FILE</c>).</summary>
    public const string DataFileName = "satisfactory_data.json";

    /// <summary>
    /// The default <c>--data</c> path of the port: <see cref="DataFileName"/> (Python <c>DATA_FILE</c>) under
    /// <c>data/</c>, relative to the working directory.
    /// </summary>
    public const string DefaultDataPath = "data/" + DataFileName;

    /// <summary>The default <c>--out</c> folder.</summary>
    public const string DefaultOutputFolder = "tiers";

    // ---- well-known ids ----------------------------------------------------------------------------------------

    /// <summary><c>Desc_Water_C</c>.</summary>
    public static ItemId Water { get; } = new("Desc_Water_C");

    /// <summary><c>Desc_LiquidOil_C</c> (crude oil).</summary>
    public static ItemId CrudeOil { get; } = new("Desc_LiquidOil_C");

    /// <summary><c>Desc_NitrogenGas_C</c>.</summary>
    public static ItemId NitrogenGas { get; } = new("Desc_NitrogenGas_C");

    /// <summary><c>Desc_NuclearFuelRod_C</c> (uranium fuel rod).</summary>
    public static ItemId NuclearFuelRod { get; } = new("Desc_NuclearFuelRod_C");

    /// <summary><c>Desc_PlutoniumFuelRod_C</c>.</summary>
    public static ItemId PlutoniumFuelRod { get; } = new("Desc_PlutoniumFuelRod_C");

    /// <summary><c>Desc_NuclearWaste_C</c> (uranium waste).</summary>
    public static ItemId NuclearWaste { get; } = new("Desc_NuclearWaste_C");

    /// <summary><c>Desc_PlutoniumWaste_C</c>.</summary>
    public static ItemId PlutoniumWaste { get; } = new("Desc_PlutoniumWaste_C");

    /// <summary><c>Desc_GeneratorNuclear_C</c>, the building of the generator pseudo-recipes.</summary>
    public static BuildingId NuclearPowerPlant { get; } = new("Desc_GeneratorNuclear_C");

    /// <summary>The name reported for the generator pseudo-recipes' building (Python <c>buildingName</c>).</summary>
    public const string NuclearPowerPlantName = "Nuclear Power Plant";

    /// <summary><c>Recipe_Mam_C</c>: MAM research and hard drives are unlocked no earlier than the MAM.</summary>
    public static RecipeId MamRecipe { get; } = new("Recipe_Mam_C");

    /// <summary><c>Desc_MinerMk1_C</c>: the building whose unlock tier gates solid resources.</summary>
    public static BuildingId DefaultExtractorBuilding { get; } = new("Desc_MinerMk1_C");

    // ---- extractors --------------------------------------------------------------------------------------------

    /// <summary>The miners (Python <c>MINERS</c>): rate on a normal node and power.</summary>
    public static FrozenDictionary<MinerTier, ExtractorSpec> Miners { get; } = new Dictionary<MinerTier, ExtractorSpec>
    {
        [MinerTier.Mk1] = new("Miner Mk.1", 60, 5),
        [MinerTier.Mk2] = new("Miner Mk.2", 120, 15),
        [MinerTier.Mk3] = new("Miner Mk.3", 240, 45),
    }.ToFrozenDictionary();

    /// <summary>The node purity rate multipliers (Python <c>PURITY</c>).</summary>
    public static FrozenDictionary<NodePurity, double> PurityMultipliers { get; } = new Dictionary<NodePurity, double>
    {
        [NodePurity.Impure] = 0.5,
        [NodePurity.Normal] = 1.0,
        [NodePurity.Pure] = 2.0,
    }.ToFrozenDictionary();

    /// <summary>Python <c>WATER_EXTRACTOR</c>.</summary>
    public static ExtractorSpec WaterExtractor { get; } = new("Water Extractor", 120, 20);

    /// <summary>Python <c>OIL_EXTRACTOR</c> (normal node).</summary>
    public static ExtractorSpec OilExtractor { get; } = new("Oil Extractor", 120, 40);

    /// <summary>Python <c>NITROGEN_WELL</c> (pressurizer power not modelled per satellite).</summary>
    public static ExtractorSpec NitrogenWell { get; } = new("Resource Well (Nitrogen, normal satellite)", 60, 0);

    /// <summary>The solid resources mined with the chosen miner (Python <c>SOLID_RESOURCES</c>).</summary>
    public static FrozenSet<ItemId> SolidResources { get; } = new ItemId[]
    {
        new("Desc_OreIron_C"), new("Desc_Coal_C"), new("Desc_Sulfur_C"), new("Desc_SAM_C"), new("Desc_OreBauxite_C"),
        new("Desc_OreGold_C"), new("Desc_OreCopper_C"), new("Desc_RawQuartz_C"), new("Desc_Stone_C"),
        new("Desc_OreUranium_C"),
    }.ToFrozenSet();

    /// <summary>
    /// The extractor building of each non-solid resource (Python <c>EXTRACTOR_BUILDING</c>); every other resource uses
    /// <see cref="DefaultExtractorBuilding"/>.
    /// </summary>
    public static FrozenDictionary<ItemId, BuildingId> ExtractorBuildings { get; } = new Dictionary<ItemId, BuildingId>
    {
        [Water] = new("Desc_WaterPump_C"),
        [CrudeOil] = new("Desc_OilPump_C"),
        [NitrogenGas] = new("Desc_FrackingSmasher_C"),
    }.ToFrozenDictionary();

    // ---- units -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Seconds per minute: converts a per-cycle amount into a per-minute rate (the Python's inline <c>* 60 /</c>).
    /// </summary>
    public const int SecondsPerMinute = 60;

    // ---- power -------------------------------------------------------------------------------------------------

    /// <summary>The Nuclear Power Plant's output in MW (Python <c>NUCLEAR_MW</c>).</summary>
    public const int NuclearMw = 2500;

    /// <summary>The Nuclear Power Plant's water use in m3/min (Python <c>NUCLEAR_WATER</c>).</summary>
    public const int NuclearWaterPerMinute = 240;

    // ---- items and costs ---------------------------------------------------------------------------------------

    /// <summary>Items that cannot go in the AWESOME Sink (Python <c>UNSINKABLE</c>).</summary>
    public static FrozenSet<ItemId> Unsinkable { get; } = new[] { NuclearWaste, PlutoniumWaste }.ToFrozenSet();

    /// <summary>Per-minute leftovers below this are rounding noise (Python <c>LEFTOVER_TOL</c>).</summary>
    public const double LeftoverTolerance = 0.01;

    /// <summary>Natural resource weights that differ from <see cref="DefaultResourceWeight"/> (Python <c>RESOURCE_WEIGHT</c>).</summary>
    public static FrozenDictionary<ItemId, double> ResourceWeights { get; } = new Dictionary<ItemId, double>
    {
        [Water] = 0.001,
    }.ToFrozenDictionary();

    /// <summary>The weight of every other natural resource.</summary>
    public const double DefaultResourceWeight = 1.0;

    // ---- tiers -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Recipe unlock tiers that replace the computed ones (Python <c>TIER_OVERRIDES</c>): the Smelter is available from
    /// tier 0 onboarding although the data file lists it under a tier 3 milestone.
    /// </summary>
    public static FrozenDictionary<RecipeId, int> TierOverrides { get; } = new Dictionary<RecipeId, int>
    {
        [new RecipeId("Recipe_SmelterMk1_C")] = 0,
    }.ToFrozenDictionary();

    /// <summary>The display names of the tiers (Python <c>TIER_NAMES</c>).</summary>
    public static FrozenDictionary<int, string> TierNames { get; } = new Dictionary<int, string>
    {
        [0] = "Tier 0 - Onboarding",
        [1] = "Tier 1",
        [2] = "Tier 2",
        [3] = "Tier 3",
        [4] = "Tier 4",
        [5] = "Tier 5",
        [6] = "Tier 6",
        [7] = "Tier 7",
        [8] = "Tier 8",
        [9] = "Tier 9",
    }.ToFrozenDictionary();

    /// <summary>The first tier planned.</summary>
    public const int FirstTier = 0;

    /// <summary>The last tier planned.</summary>
    public const int LastTier = 9;

    /// <summary>The tier of an item no unlocked recipe makes yet (Python <c>item_tier.get(..., 99)</c>).</summary>
    public const int UnknownItemTier = 99;

    // ---- string markers ----------------------------------------------------------------------------------------

    /// <summary>The prefix of building class names (<c>Desc_</c>), cut off by <c>building_tier</c>.</summary>
    public const string BuildingDescriptorPrefix = "Desc_";

    /// <summary>The prefix of the build recipe of a building (<c>Recipe_</c>).</summary>
    public const string BuildingRecipePrefix = "Recipe_";

    /// <summary>Recipes whose name starts with this are logistics, not production.</summary>
    public const string UnpackagePrefix = "Unpackage";

    /// <summary>Packaging recipes (preferred for fluid disposal) start with this.</summary>
    public const string PackagedPrefix = "Packaged";

    /// <summary>The lower-case prefix removed from alternate recipe names when comparing them with item names.</summary>
    public const string AlternateNamePrefix = "alternate: ";

    /// <summary>Item class names containing this are ingots (not excess targets unless <c>--include-ingots</c>).</summary>
    public const string IngotMarker = "Ingot";

    // ---- tolerances --------------------------------------------------------------------------------------------

    /// <summary>A recipe must beat the current cost by more than this (<c>_costs</c>).</summary>
    public const double CostImprovementTolerance = 1e-9;

    /// <summary>Balances below minus this are shortfalls (<c>_converge</c>).</summary>
    public const double ShortfallTolerance = 1e-9;

    /// <summary>Remaining quantities and headroom at or below this are ignored (<c>_converge</c>).</summary>
    public const double CapacityTolerance = 1e-12;

    /// <summary>Surpluses at or below this are not trimmed (<c>_trim</c>).</summary>
    public const double TrimSurplusTolerance = 1e-6;

    /// <summary>Cuts at or below this are not made (<c>_trim</c>).</summary>
    public const double TrimCutTolerance = 1e-9;

    /// <summary>Subtracted before rounding the new machine count up (<c>run_tiers</c>).</summary>
    public const double NewMachineTolerance = 1e-6;

    /// <summary>Reused capacity gains at or below this are ignored (<c>run_tiers</c>).</summary>
    public const double ReusedGainTolerance = 1e-6;

    // ---- rounding ----------------------------------------------------------------------------------------------

    /// <summary>Digits of <c>round(recipe_cost, 6)</c> in candidate sorting.</summary>
    public const int CostRoundingDigits = 6;

    /// <summary>Digits of <c>round(w, 1)</c> in the CSV <c>mw</c> column.</summary>
    public const int PowerRoundingDigits = 1;

    // ---- loop caps ---------------------------------------------------------------------------------------------

    /// <summary>Relaxation passes of <c>_costs</c>.</summary>
    public const int CostRelaxationPasses = 500;

    /// <summary>Passes of the cycle rejection in <c>_choose_all</c>.</summary>
    public const int CycleRejectionPasses = 100;

    /// <summary>Iterations of <c>_converge</c> before "Did not converge".</summary>
    public const int ConvergeIterations = 5000;

    /// <summary>Iterations of <c>_trim</c>.</summary>
    public const int TrimIterations = 50;

    /// <summary>Iterations of the fluid disposal loop of <c>plan</c> before "Byproduct disposal did not converge".</summary>
    public const int DisposalIterations = 100;

    /// <summary>Rounds of <c>optimise</c>.</summary>
    public const int OptimiseRounds = 20;

    /// <summary>Passes of <c>unlock_tiers</c>.</summary>
    public const int UnlockTierPasses = 50;
}
