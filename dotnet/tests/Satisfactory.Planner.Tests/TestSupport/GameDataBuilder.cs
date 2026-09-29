using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test Data Builder for synthetic <see cref="GameData"/>. Everything a recipe uses must be declared explicitly: its
/// items (<see cref="WithItem"/>), its building (<see cref="WithBuilding"/>); resources and schematic costs must be
/// declared items too. <see cref="Build"/> checks this and throws <see cref="InvalidOperationException"/> otherwise.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GameData.Recipes"/> lists the manufacturing recipes (plain and variable power) in the order they were
/// added, then the generator recipes in the order they were added, as the JSON loader does.
/// <see cref="GameData.RecipeDefinitions"/> lists every recipe added (manufacturing, generator and definition-only
/// recipes such as build recipes) in the order they were added.
/// </para>
/// <example>
/// <code>
/// GameData data = new GameDataBuilder()
///     .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
///     .WithItem("Desc_IronIngot_C", "Iron Ingot")
///     .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
///     .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2,
///                 [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
///     .Build();
/// </code>
/// </example>
/// </remarks>
public sealed class GameDataBuilder
{
    private readonly List<Item> _items = [];
    private readonly List<Building> _buildings = [];
    private readonly List<Schematic> _schematics = [];
    private readonly List<ItemId> _resources = [];
    private readonly List<Recipe> _manufacturing = [];
    private readonly List<Recipe> _generators = [];
    private readonly List<RecipeDefinition> _definitions = [];
    private readonly HashSet<RecipeId> _recipeIds = [];

    /// <summary>Declares an item.</summary>
    /// <param name="id">The class name, e.g. <c>Desc_IronPlate_C</c>.</param>
    /// <param name="name">The display name.</param>
    /// <param name="isFluid">Whether it is a fluid (<c>liquid</c>).</param>
    /// <param name="energyValue">The <c>energyValue</c> (fuel rods: MJ per rod).</param>
    /// <param name="stackSize">The stack size.</param>
    /// <param name="sinkPoints">The AWESOME Sink points.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithItem(
        string id, string name, bool isFluid = false, double energyValue = 0, int stackSize = 100, int sinkPoints = 0)
    {
        _items.Add(new Item(new ItemId(id), name, isFluid, stackSize, sinkPoints, energyValue));
        return this;
    }

    /// <summary>Declares a fluid item.</summary>
    /// <param name="id">The class name.</param>
    /// <param name="name">The display name.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithFluid(string id, string name) => WithItem(id, name, isFluid: true, stackSize: 0);

    /// <summary>Marks a declared item as a natural resource (a key of <c>resources</c>), in declaration order.</summary>
    /// <param name="id">The item's class name.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithResource(string id)
    {
        _resources.Add(new ItemId(id));
        return this;
    }

    /// <summary>Declares a building.</summary>
    /// <param name="id">The class name, e.g. <c>Desc_SmelterMk1_C</c>.</param>
    /// <param name="name">The display name.</param>
    /// <param name="powerMw">The <c>metadata.powerConsumption</c>; <see langword="null"/> for none.</param>
    /// <param name="isInteger">Whether the JSON token would be an integer literal.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithBuilding(string id, string name, double? powerMw, bool isInteger = true)
    {
        _buildings.Add(new Building(new BuildingId(id), name, powerMw, isInteger && powerMw is not null));
        return this;
    }

    /// <summary>Adds a manufacturing recipe with the building's fixed power.</summary>
    /// <param name="id">The recipe class name.</param>
    /// <param name="name">The display name.</param>
    /// <param name="building">The building's class name (<c>producedIn[0]</c>).</param>
    /// <param name="timeSeconds">The cycle time.</param>
    /// <param name="ingredients">The ingredients per cycle, in order.</param>
    /// <param name="products">The products per cycle, in order (the first is the main product).</param>
    /// <param name="alternate">Whether it is an alternate recipe.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithRecipe(
        string id,
        string name,
        string building,
        double timeSeconds,
        IEnumerable<(string Item, double Amount)> ingredients,
        IEnumerable<(string Item, double Amount)> products,
        bool alternate = false)
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId(id), name, alternate, timeSeconds, new BuildingId(building),
            Amounts(ingredients), Amounts(products), IsVariablePower: false, MinPowerMw: 0, MaxPowerMw: 0);
        return AddRunnable(recipe, _manufacturing);
    }

    /// <summary>Adds a manufacturing recipe with variable power (<c>isVariablePower</c>, <c>minPower</c>, <c>maxPower</c>).</summary>
    /// <param name="id">The recipe class name.</param>
    /// <param name="name">The display name.</param>
    /// <param name="building">The building's class name.</param>
    /// <param name="timeSeconds">The cycle time.</param>
    /// <param name="ingredients">The ingredients per cycle, in order.</param>
    /// <param name="products">The products per cycle, in order.</param>
    /// <param name="minPowerMw">The <c>minPower</c>.</param>
    /// <param name="maxPowerMw">The <c>maxPower</c>.</param>
    /// <param name="alternate">Whether it is an alternate recipe.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithVariablePowerRecipe(
        string id,
        string name,
        string building,
        double timeSeconds,
        IEnumerable<(string Item, double Amount)> ingredients,
        IEnumerable<(string Item, double Amount)> products,
        double minPowerMw,
        double maxPowerMw,
        bool alternate = false)
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId(id), name, alternate, timeSeconds, new BuildingId(building),
            Amounts(ingredients), Amounts(products), IsVariablePower: true, minPowerMw, maxPowerMw);
        return AddRunnable(recipe, _manufacturing);
    }

    /// <summary>Adds a generator pseudo-recipe.</summary>
    /// <param name="id">The recipe id, e.g. <c>Gen_Nuclear_Uranium</c>.</param>
    /// <param name="name">The display name.</param>
    /// <param name="building">The building's class name.</param>
    /// <param name="buildingName">The building name reported for the machines.</param>
    /// <param name="timeSeconds">Seconds per cycle.</param>
    /// <param name="ingredients">The ingredients per cycle.</param>
    /// <param name="products">The products per cycle.</param>
    /// <param name="powerOutputMw">The power one machine generates (positive).</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithGeneratorRecipe(
        string id,
        string name,
        string building,
        string buildingName,
        double timeSeconds,
        IEnumerable<(string Item, double Amount)> ingredients,
        IEnumerable<(string Item, double Amount)> products,
        double powerOutputMw)
    {
        var recipe = new GeneratorRecipe(
            new RecipeId(id), name, IsAlternate: false, timeSeconds, new BuildingId(building),
            Amounts(ingredients), Amounts(products), buildingName, powerOutputMw);
        return AddRunnable(recipe, _generators);
    }

    /// <summary>
    /// Adds the two nuclear generator recipes exactly as the JSON loader builds them, from the already declared
    /// <c>Desc_NuclearFuelRod_C</c> and <c>Desc_PlutoniumFuelRod_C</c> items' energy values. The rods, their wastes,
    /// water and the <c>Desc_GeneratorNuclear_C</c> building must be declared as well (checked by <see cref="Build"/>).
    /// </summary>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithNuclearGeneratorRecipes()
    {
        AddNuclear(PlannerConstants.NuclearFuelRod, PlannerConstants.NuclearWaste, 50, "Uranium");
        AddNuclear(PlannerConstants.PlutoniumFuelRod, PlannerConstants.PlutoniumWaste, 10, "Plutonium");
        return this;
    }

    /// <summary>
    /// Adds a recipe that only exists as a definition (not run in a machine), such as a build-gun recipe; it matters for
    /// the unlock tiers only.
    /// </summary>
    /// <param name="id">The recipe class name.</param>
    /// <param name="ingredients">The ingredients.</param>
    /// <param name="products">The products.</param>
    /// <param name="name">The display name (the id when omitted).</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithRecipeDefinition(
        string id,
        IEnumerable<(string Item, double Amount)> ingredients,
        IEnumerable<(string Item, double Amount)> products,
        string? name = null)
    {
        var recipeId = new RecipeId(id);
        AddId(recipeId);
        _definitions.Add(new RecipeDefinition(recipeId, name ?? id, Amounts(ingredients), Amounts(products)));
        return this;
    }

    /// <summary>
    /// Adds the build recipe of a building (<c>Recipe_</c> + the class name after <c>Desc_</c>), producing one building,
    /// so that a schematic can unlock the building (its <c>BuildingTier</c>).
    /// </summary>
    /// <param name="building">The building's class name, e.g. <c>Desc_SmelterMk1_C</c>.</param>
    /// <param name="cost">The build cost (none when omitted).</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithBuildRecipe(string building, IEnumerable<(string Item, double Amount)>? cost = null)
    {
        ArgumentNullException.ThrowIfNull(building);
        string suffix = building.Length > PlannerConstants.BuildingDescriptorPrefix.Length
            ? building[PlannerConstants.BuildingDescriptorPrefix.Length..]
            : string.Empty;
        return WithRecipeDefinition(PlannerConstants.BuildingRecipePrefix + suffix, cost ?? [], [(building, 1)]);
    }

    /// <summary>Adds a schematic.</summary>
    /// <param name="id">The schematic key.</param>
    /// <param name="type">The type.</param>
    /// <param name="tier">The tier (milestones and tutorials).</param>
    /// <param name="unlocks">The recipe class names it unlocks.</param>
    /// <param name="required">The required schematics.</param>
    /// <param name="cost">The item cost.</param>
    /// <param name="name">The display name (the id when omitted).</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithSchematic(
        string id,
        SchematicType type,
        int tier = 0,
        IEnumerable<string>? unlocks = null,
        IEnumerable<string>? required = null,
        IEnumerable<(string Item, double Amount)>? cost = null,
        string? name = null)
    {
        _schematics.Add(new Schematic(
            new SchematicId(id),
            name ?? id,
            type,
            tier,
            (required ?? []).Select(s => new SchematicId(s)).ToList().AsReadOnly(),
            Amounts(cost ?? []),
            (unlocks ?? []).Select(r => new RecipeId(r)).ToList().AsReadOnly()));
        return this;
    }

    /// <summary>Adds a milestone (<see cref="SchematicType.Milestone"/>) of the given tier.</summary>
    /// <param name="id">The schematic key.</param>
    /// <param name="tier">The tier.</param>
    /// <param name="unlocks">The recipe class names it unlocks.</param>
    /// <param name="cost">The item cost.</param>
    /// <param name="required">The required schematics.</param>
    /// <returns>This builder.</returns>
    public GameDataBuilder WithMilestone(
        string id,
        int tier,
        IEnumerable<string> unlocks,
        IEnumerable<(string Item, double Amount)>? cost = null,
        IEnumerable<string>? required = null) =>
        WithSchematic(id, SchematicType.Milestone, tier, unlocks, required, cost);

    /// <summary>Builds the game data after checking that every referenced item and building is declared.</summary>
    /// <returns>The game data.</returns>
    /// <exception cref="InvalidOperationException">Something referenced is not declared.</exception>
    public GameData Build()
    {
        var items = new HashSet<ItemId>(_items.Select(i => i.Id));
        var buildings = new HashSet<BuildingId>(_buildings.Select(b => b.Id));
        foreach (ItemId resource in _resources)
        {
            Require(items.Contains(resource), $"Resource {resource} is not a declared item.");
        }

        foreach (Recipe recipe in _manufacturing.Concat(_generators))
        {
            Require(buildings.Contains(recipe.BuildingId), $"Recipe {recipe.Id} uses undeclared building {recipe.BuildingId}.");
            foreach (ItemAmount amount in recipe.Ingredients.Concat(recipe.Products))
            {
                Require(items.Contains(amount.Item), $"Recipe {recipe.Id} uses undeclared item {amount.Item}.");
            }
        }

        foreach (Schematic schematic in _schematics)
        {
            foreach (ItemAmount amount in schematic.Cost)
            {
                Require(items.Contains(amount.Item), $"Schematic {schematic.Id} costs undeclared item {amount.Item}.");
            }
        }

        return new GameData(
            _items, _buildings, _schematics, _resources, _manufacturing.Concat(_generators), _definitions);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<ItemAmount> Amounts(IEnumerable<(string Item, double Amount)> amounts) =>
        amounts.Select(a => new ItemAmount(new ItemId(a.Item), a.Amount)).ToList().AsReadOnly();

    private void AddNuclear(ItemId rod, ItemId waste, int wastePerRod, string label)
    {
        Item rodItem = _items.Find(i => i.Id == rod)
            ?? throw new InvalidOperationException($"Declare {rod} (with its energy value) before the generators.");
        double t = rodItem.EnergyValue / PlannerConstants.NuclearMw;
        WithGeneratorRecipe(
            $"Gen_Nuclear_{label}",
            $"Burn {label} Fuel Rod",
            PlannerConstants.NuclearPowerPlant.Value,
            PlannerConstants.NuclearPowerPlantName,
            t,
            [(rod.Value, 1), (PlannerConstants.Water.Value, PlannerConstants.NuclearWaterPerMinute * t / 60)],
            [(waste.Value, wastePerRod)],
            PlannerConstants.NuclearMw);
    }

    private GameDataBuilder AddRunnable(Recipe recipe, List<Recipe> target)
    {
        AddId(recipe.Id);
        target.Add(recipe);
        _definitions.Add(new RecipeDefinition(recipe.Id, recipe.Name, recipe.Ingredients, recipe.Products));
        return this;
    }

    private void AddId(RecipeId id)
    {
        if (!_recipeIds.Add(id))
        {
            throw new InvalidOperationException($"Recipe {id} was added twice.");
        }
    }
}
