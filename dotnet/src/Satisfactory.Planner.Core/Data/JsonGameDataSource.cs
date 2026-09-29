using System.Text.Json;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Data;

/// <summary>
/// Reads the greeny/SatisfactoryTools <c>data.json</c> (Python <c>load_data</c>) and builds the planner's recipes as
/// <c>Planner.__init__</c> does: every recipe with <c>inMachine</c> true and a non-empty <c>producedIn</c>, in document
/// order, then the uranium and plutonium generator pseudo-recipes. Document order is kept everywhere.
/// </summary>
public sealed class JsonGameDataSource : IGameDataSource
{
    private const int UraniumWastePerRod = 50;
    private const int PlutoniumWastePerRod = 10;

    private readonly string _path;

    /// <summary>Creates the source.</summary>
    /// <param name="path">The data file.</param>
    public JsonGameDataSource(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    /// <inheritdoc />
    /// <exception cref="GameDataFormatException">
    /// The file is not valid JSON, a required field is missing or mistyped, or a fuel rod item is missing.
    /// </exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public async Task<GameData> LoadAsync(CancellationToken cancellationToken)
    {
        FileStream stream = new(
            _path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        await using (stream.ConfigureAwait(false))
        {
            JsonDocument document;
            try
            {
                document = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                throw new GameDataFormatException($"{_path} is not valid JSON: {e.Message}", e);
            }

            using (document)
            {
                return Parse(document.RootElement);
            }
        }
    }

    private static GameData Parse(JsonElement root)
    {
        const string rootContext = "the data file";
        OrderedDictionary<string, JsonElement> items =
            JsonFields.Properties(JsonFields.Required(root, "items", rootContext), "items");
        OrderedDictionary<string, JsonElement> buildings =
            JsonFields.Properties(JsonFields.Required(root, "buildings", rootContext), "buildings");
        OrderedDictionary<string, JsonElement> schematics =
            JsonFields.Properties(JsonFields.Required(root, "schematics", rootContext), "schematics");
        OrderedDictionary<string, JsonElement> resources =
            JsonFields.Properties(JsonFields.Required(root, "resources", rootContext), "resources");
        OrderedDictionary<string, JsonElement> recipes =
            JsonFields.Properties(JsonFields.Required(root, "recipes", rootContext), "recipes");

        var itemList = items.Select(kv => ParseItem(kv.Key, kv.Value)).ToList();
        var buildingList = buildings.Select(kv => ParseBuilding(kv.Key, kv.Value)).ToList();
        var schematicList = schematics.Select(kv => ParseSchematic(kv.Key, kv.Value)).ToList();
        var resourceList = resources.Keys.Select(k => new ItemId(k)).ToList();

        var definitions = new List<RecipeDefinition>(recipes.Count);
        var runnable = new List<Recipe>();
        foreach ((string key, JsonElement value) in recipes)
        {
            (RecipeDefinition definition, ManufacturingRecipe? manufacturing) = ParseRecipe(key, value);
            definitions.Add(definition);
            if (manufacturing is not null)
            {
                runnable.Add(manufacturing);
            }
        }

        runnable.Add(Generator(
            items, PlannerConstants.NuclearFuelRod, PlannerConstants.NuclearWaste, UraniumWastePerRod, "Uranium"));
        runnable.Add(Generator(
            items, PlannerConstants.PlutoniumFuelRod, PlannerConstants.PlutoniumWaste, PlutoniumWastePerRod, "Plutonium"));

        return new GameData(itemList, buildingList, schematicList, resourceList, runnable, definitions);
    }

    private static Item ParseItem(string key, JsonElement value)
    {
        string context = $"items[\"{key}\"]";
        return new Item(
            new ItemId(key),
            JsonFields.RequiredString(value, "name", context),
            JsonFields.OptionalBool(value, "liquid", context, fallback: false),
            JsonFields.OptionalInt(value, "stackSize", context, fallback: 0),
            JsonFields.OptionalInt(value, "sinkPoints", context, fallback: 0),
            JsonFields.OptionalDouble(value, "energyValue", context, fallback: 0));
    }

    private static Building ParseBuilding(string key, JsonElement value)
    {
        string context = $"buildings[\"{key}\"]";
        string name = JsonFields.RequiredString(value, "name", context);
        double? power = null;
        bool isInteger = false;
        if (JsonFields.TryGet(value, "metadata", context, out JsonElement metadata)
            && metadata.ValueKind != JsonValueKind.Null)
        {
            string metadataContext = context + ".metadata";
            if (JsonFields.TryGet(metadata, "powerConsumption", metadataContext, out JsonElement token)
                && token.ValueKind != JsonValueKind.Null)
            {
                power = JsonFields.RequiredDouble(metadata, "powerConsumption", metadataContext);
                isInteger = JsonFields.IsIntegerLiteral(token);
            }
        }

        return new Building(new BuildingId(key), name, power, isInteger);
    }

    private static Schematic ParseSchematic(string key, JsonElement value)
    {
        string context = $"schematics[\"{key}\"]";
        SchematicType type = TypeOf(JsonFields.RequiredString(value, "type", context));
        string name = JsonFields.OptionalString(value, "name", context) ?? key;

        // unlock_tiers reads "tier" of milestones and tutorials, and "cost" / "unlock.recipes" of the five schematic
        // types it considers; other schematics only need a "type".
        bool hasOwnTier = type is SchematicType.Milestone or SchematicType.Tutorial;
        bool considered = hasOwnTier || type is SchematicType.Mam or SchematicType.Alternate or SchematicType.HardDrive;
        int tier = hasOwnTier
            ? JsonFields.RequiredInt(value, "tier", context)
            : JsonFields.OptionalInt(value, "tier", context, fallback: 0);

        List<SchematicId> required = [];
        if (JsonFields.OptionalArray(value, "requiredSchematics", context) is { } requiredArray)
        {
            foreach (JsonElement entry in requiredArray.EnumerateArray())
            {
                required.Add(new SchematicId(JsonFields.AsString(entry, "requiredSchematics", context)));
            }
        }

        List<ItemAmount> cost = [];
        JsonElement? costArray = considered
            ? JsonFields.RequiredArray(value, "cost", context)
            : JsonFields.OptionalArray(value, "cost", context);
        if (costArray is { } costs)
        {
            cost.AddRange(ParseAmounts(costs, context + ".cost"));
        }

        List<RecipeId> unlocks = [];
        JsonElement? unlockArray = null;
        if (considered)
        {
            JsonElement unlock = JsonFields.RequiredObject(value, "unlock", context);
            unlockArray = JsonFields.RequiredArray(unlock, "recipes", context + ".unlock");
        }
        else if (JsonFields.TryGet(value, "unlock", context, out JsonElement unlock)
                 && unlock.ValueKind == JsonValueKind.Object)
        {
            unlockArray = JsonFields.OptionalArray(unlock, "recipes", context + ".unlock");
        }

        if (unlockArray is { } unlockRecipes)
        {
            foreach (JsonElement entry in unlockRecipes.EnumerateArray())
            {
                unlocks.Add(new RecipeId(JsonFields.AsString(entry, "recipes", context + ".unlock")));
            }
        }

        return new Schematic(
            new SchematicId(key), name, type, tier, required.AsReadOnly(), cost.AsReadOnly(), unlocks.AsReadOnly());
    }

    private static (RecipeDefinition Definition, ManufacturingRecipe? Manufacturing) ParseRecipe(
        string key, JsonElement value)
    {
        string context = $"recipes[\"{key}\"]";
        var id = new RecipeId(JsonFields.RequiredString(value, "className", context));
        var ingredients = ParseAmounts(JsonFields.RequiredArray(value, "ingredients", context), context + ".ingredients");
        var products = ParseAmounts(JsonFields.RequiredArray(value, "products", context), context + ".products");

        // Python: [r for r in data["recipes"].values() if r["inMachine"] and r["producedIn"]]
        bool inMachine = JsonFields.RequiredBool(value, "inMachine", context);
        List<BuildingId> producedIn = [];
        if (inMachine)
        {
            foreach (JsonElement entry in JsonFields.RequiredArray(value, "producedIn", context).EnumerateArray())
            {
                producedIn.Add(new BuildingId(JsonFields.AsString(entry, "producedIn", context)));
            }
        }

        if (!inMachine || producedIn.Count == 0)
        {
            string definitionName = JsonFields.OptionalString(value, "name", context) ?? id.Value;
            return (new RecipeDefinition(id, definitionName, ingredients, products), null);
        }

        string name = JsonFields.RequiredString(value, "name", context);
        bool isVariablePower = JsonFields.OptionalBool(value, "isVariablePower", context, fallback: false);
        double minPower = isVariablePower
            ? JsonFields.RequiredDouble(value, "minPower", context)
            : JsonFields.OptionalDouble(value, "minPower", context, fallback: 0);
        double maxPower = isVariablePower
            ? JsonFields.RequiredDouble(value, "maxPower", context)
            : JsonFields.OptionalDouble(value, "maxPower", context, fallback: 0);
        var manufacturing = new ManufacturingRecipe(
            id,
            name,
            JsonFields.RequiredBool(value, "alternate", context),
            JsonFields.RequiredDouble(value, "time", context),
            producedIn[0],
            ingredients,
            products,
            isVariablePower,
            minPower,
            maxPower);
        return (new RecipeDefinition(id, name, ingredients, products), manufacturing);
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<ItemAmount> ParseAmounts(JsonElement array, string context)
    {
        var amounts = new List<ItemAmount>(array.GetArrayLength());
        int index = 0;
        foreach (JsonElement entry in array.EnumerateArray())
        {
            string entryContext = $"{context}[{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}]";
            amounts.Add(new ItemAmount(
                new ItemId(JsonFields.RequiredString(entry, "item", entryContext)),
                JsonFields.RequiredDouble(entry, "amount", entryContext)));
            index++;
        }

        return amounts.AsReadOnly();
    }

    /// <summary>
    /// Planner.__init__ lines 88-95: <c>t = items[rod]["energyValue"] / NUCLEAR_MW</c> seconds per rod; ingredients
    /// the rod and <c>NUCLEAR_WATER * t / 60</c> water; product the waste; power <c>-NUCLEAR_MW</c>.
    /// </summary>
    private static GeneratorRecipe Generator(
        OrderedDictionary<string, JsonElement> items, ItemId rod, ItemId waste, int wastePerRod, string label)
    {
        if (!items.TryGetValue(rod.Value, out JsonElement rodItem))
        {
            throw new GameDataFormatException($"Missing item {rod} in items (needed for the {label} generator).");
        }

        double energy = JsonFields.RequiredDouble(rodItem, "energyValue", $"items[\"{rod}\"]");
        double t = energy / PlannerConstants.NuclearMw;
        return new GeneratorRecipe(
            new RecipeId($"Gen_Nuclear_{label}"),
            $"Burn {label} Fuel Rod",
            IsAlternate: false,
            t,
            PlannerConstants.NuclearPowerPlant,
            new[]
            {
                new ItemAmount(rod, 1),
                new ItemAmount(PlannerConstants.Water, PlannerConstants.NuclearWaterPerMinute * t / PlannerConstants.SecondsPerMinute),
            }.AsReadOnly(),
            new[] { new ItemAmount(waste, wastePerRod) }.AsReadOnly(),
            PlannerConstants.NuclearPowerPlantName,
            PlannerConstants.NuclearMw);
    }

    private static SchematicType TypeOf(string type) => type switch
    {
        "EST_Milestone" => SchematicType.Milestone,
        "EST_Tutorial" => SchematicType.Tutorial,
        "EST_MAM" => SchematicType.Mam,
        "EST_Alternate" => SchematicType.Alternate,
        "EST_HardDrive" => SchematicType.HardDrive,
        "EST_ResourceSink" => SchematicType.ResourceSink,
        "EST_Customization" => SchematicType.Customization,
        _ => SchematicType.Other,
    };
}
