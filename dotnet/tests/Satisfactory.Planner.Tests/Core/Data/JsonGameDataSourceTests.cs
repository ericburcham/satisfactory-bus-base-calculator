using System.Text.Json;
using System.Text.Json.Nodes;
using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Data;

[TestFixture]
public sealed class JsonGameDataSourceTests
{
    private const string TinyJson = """
        {
          "items": {
            "Desc_OreIron_C": { "name": "Iron Ore", "liquid": false, "stackSize": 100, "sinkPoints": 1, "energyValue": 0.0 },
            "Desc_Water_C": { "name": "Water", "liquid": true, "stackSize": 0, "sinkPoints": 5000, "energyValue": 0.0 },
            "Desc_IronIngot_C": { "name": "Iron Ingot" },
            "Desc_NuclearFuelRod_C": { "name": "Uranium Fuel Rod", "liquid": false, "energyValue": 750000.0 },
            "Desc_PlutoniumFuelRod_C": { "name": "Plutonium Fuel Rod", "energyValue": 1500000 },
            "Desc_NuclearWaste_C": { "name": "Uranium Waste" },
            "Desc_PlutoniumWaste_C": { "name": "Plutonium Waste" }
          },
          "recipes": {
            "Recipe_TradingPost_C": {
              "name": "The HUB", "className": "Recipe_TradingPost_C", "alternate": false, "time": 1.0,
              "inMachine": false, "producedIn": [],
              "ingredients": [ { "item": "Desc_OreIron_C", "amount": 20.0 } ],
              "products": [ { "item": "Desc_TradingPost_C", "amount": 1.0 } ]
            },
            "Recipe_IngotIron_C": {
              "name": "Iron Ingot", "className": "Recipe_IngotIron_C", "alternate": false, "time": 2.0,
              "inMachine": true, "producedIn": [ "Desc_SmelterMk1_C", "Desc_Workshop_C" ],
              "ingredients": [ { "item": "Desc_OreIron_C", "amount": 1.0 } ],
              "products": [ { "item": "Desc_IronIngot_C", "amount": 1.0 } ],
              "isVariablePower": false, "minPower": 0, "maxPower": 1
            },
            "Recipe_HandOnly_C": {
              "name": "Hand Only", "className": "Recipe_HandOnly_C", "alternate": false, "time": 1.0,
              "inMachine": true, "producedIn": [],
              "ingredients": [], "products": [ { "item": "Desc_IronIngot_C", "amount": 1.0 } ]
            },
            "Recipe_Alternate_Pure_C": {
              "name": "Alternate: Pure Iron Ingot", "className": "Recipe_Alternate_Pure_C", "alternate": true, "time": 12.0,
              "inMachine": true, "producedIn": [ "Desc_OilRefinery_C" ],
              "ingredients": [ { "item": "Desc_OreIron_C", "amount": 7.0 }, { "item": "Desc_Water_C", "amount": 4.0 } ],
              "products": [ { "item": "Desc_IronIngot_C", "amount": 13.0 } ]
            },
            "Recipe_Variable_C": {
              "name": "Variable", "className": "Recipe_Variable_C", "alternate": false, "time": 6.0,
              "inMachine": true, "producedIn": [ "Desc_HadronCollider_C" ],
              "ingredients": [ { "item": "Desc_IronIngot_C", "amount": 1.0 } ],
              "products": [ { "item": "Desc_OreIron_C", "amount": 2.0 } ],
              "isVariablePower": true, "minPower": 500, "maxPower": 1500
            }
          },
          "schematics": {
            "Schematic_1-1_C": {
              "className": "Schematic_1-1_C", "type": "EST_Milestone", "name": "Base Building",
              "cost": [ { "item": "Desc_IronIngot_C", "amount": 10.0 } ],
              "unlock": { "recipes": [ "Recipe_IngotIron_C" ] }, "requiredSchematics": [], "tier": 1
            },
            "Schematic_Alt_C": {
              "className": "Schematic_Alt_C", "type": "EST_Alternate", "name": "Alternate: Pure Iron Ingot",
              "cost": [], "unlock": { "recipes": [ "Recipe_Alternate_Pure_C" ] },
              "requiredSchematics": [ "Schematic_1-1_C" ], "tier": 0
            },
            "Schematic_Sink_C": { "type": "EST_ResourceSink" },
            "Schematic_Odd_C": { "type": "EST_Something", "name": "Odd", "tier": 3, "cost": [], "unlock": { "recipes": [] } }
          },
          "resources": {
            "Desc_Water_C": { "item": "Desc_Water_C", "speed": 1 },
            "Desc_OreIron_C": { "item": "Desc_OreIron_C", "speed": 1 }
          },
          "buildings": {
            "Desc_SmelterMk1_C": { "name": "Smelter", "metadata": { "powerConsumption": 4, "manufacturingSpeed": 1 } },
            "Desc_OilRefinery_C": { "name": "Refinery", "metadata": { "powerConsumption": 30.0 } },
            "Desc_HadronCollider_C": { "name": "Particle Accelerator", "metadata": { "powerConsumption": 1e3 } },
            "Desc_Wall_C": { "name": "Wall" },
            "Desc_Beam_C": { "name": "Beam", "metadata": { } }
          }
        }
        """;

    private CoreScratchDirectory _scratch = null!;

    [SetUp]
    public void SetUp() => _scratch = new CoreScratchDirectory();

    [TearDown]
    public void TearDown() => _scratch.Dispose();

    private Task<GameData> LoadAsync(string json) =>
        new JsonGameDataSource(_scratch.WriteFile("data.json", json)).LoadAsync(CancellationToken.None);

    [Test]
    public async Task Items_KeepDocumentOrderAndPythonDefaults()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.That(
            data.Items.Select(i => i.Id.Value),
            Is.EqualTo(ExpectedSequence.Of(
                "Desc_OreIron_C", "Desc_Water_C", "Desc_IronIngot_C", "Desc_NuclearFuelRod_C", "Desc_PlutoniumFuelRod_C",
                "Desc_NuclearWaste_C", "Desc_PlutoniumWaste_C")));
        Item water = data.GetItem(new ItemId("Desc_Water_C"));
        Item ingot = data.GetItem(new ItemId("Desc_IronIngot_C"));
        Assert.Multiple(() =>
        {
            Assert.That(water, Is.EqualTo(new Item(new ItemId("Desc_Water_C"), "Water", true, 0, 5000, 0)));
            Assert.That(ingot, Is.EqualTo(new Item(new ItemId("Desc_IronIngot_C"), "Iron Ingot", false, 0, 0, 0)));
        });
    }

    [Test]
    public async Task Resources_AreTheKeysInDocumentOrder()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.That(data.Resources, Is.EqualTo(ExpectedSequence.Of(new ItemId("Desc_Water_C"), new ItemId("Desc_OreIron_C"))));
    }

    [Test]
    public async Task Buildings_PowerAndIntegerFlagComeFromTheJsonToken()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.Multiple(() =>
        {
            Assert.That(
                data.Buildings.Select(b => b.Id.Value),
                Is.EqualTo(ExpectedSequence.Of("Desc_SmelterMk1_C", "Desc_OilRefinery_C", "Desc_HadronCollider_C", "Desc_Wall_C", "Desc_Beam_C")));
            Assert.That(
                data.GetBuilding(new BuildingId("Desc_SmelterMk1_C")),
                Is.EqualTo(new Building(new BuildingId("Desc_SmelterMk1_C"), "Smelter", 4, true)));
            Assert.That(
                data.GetBuilding(new BuildingId("Desc_OilRefinery_C")),
                Is.EqualTo(new Building(new BuildingId("Desc_OilRefinery_C"), "Refinery", 30, false)));
            Assert.That(
                data.GetBuilding(new BuildingId("Desc_HadronCollider_C")),
                Is.EqualTo(new Building(new BuildingId("Desc_HadronCollider_C"), "Particle Accelerator", 1000, false)));
            Assert.That(data.GetBuilding(new BuildingId("Desc_Wall_C")).PowerConsumptionMw, Is.Null);
            Assert.That(data.GetBuilding(new BuildingId("Desc_Beam_C")).PowerConsumptionMw, Is.Null);
        });
    }

    [Test]
    public async Task Recipes_AreMachineRecipesInDocumentOrderThenTheGenerators()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.That(
            data.Recipes.Select(r => r.Id.Value),
            Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IngotIron_C", "Recipe_Alternate_Pure_C", "Recipe_Variable_C", "Gen_Nuclear_Uranium", "Gen_Nuclear_Plutonium")));
    }

    [Test]
    public async Task ManufacturingRecipe_TakesAllFieldsAndTheFirstProducedIn()
    {
        GameData data = await LoadAsync(TinyJson);

        var ingot = (ManufacturingRecipe)data.Recipes[0];
        var pure = (ManufacturingRecipe)data.Recipes[1];
        var variable = (ManufacturingRecipe)data.Recipes[2];
        Assert.Multiple(() =>
        {
            Assert.That(ingot.Name, Is.EqualTo("Iron Ingot"));
            Assert.That(ingot.TimeSeconds, Is.EqualTo(2.0));
            Assert.That(ingot.BuildingId, Is.EqualTo(new BuildingId("Desc_SmelterMk1_C")));
            Assert.That(ingot.IsVariablePower, Is.False);
            Assert.That(ingot.MaxPowerMw, Is.EqualTo(1));
            Assert.That(pure.IsAlternate, Is.True);
            Assert.That(pure.IsVariablePower, Is.False, "isVariablePower defaults to false");
            Assert.That(
                pure.Ingredients,
                Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_OreIron_C"), 7), new ItemAmount(new ItemId("Desc_Water_C"), 4))));
            Assert.That(pure.Products, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_IronIngot_C"), 13))));
            Assert.That(variable.IsVariablePower, Is.True);
            Assert.That(variable.MinPowerMw, Is.EqualTo(500));
            Assert.That(variable.MaxPowerMw, Is.EqualTo(1500));
        });
    }

    [Test]
    public async Task GeneratorRecipes_FollowPlannerInit()
    {
        GameData data = await LoadAsync(TinyJson);

        var uranium = (GeneratorRecipe)data.Recipes[^2];
        var plutonium = (GeneratorRecipe)data.Recipes[^1];
        Assert.Multiple(() =>
        {
            Assert.That(uranium, Is.EqualTo(uranium with
            {
                Id = new RecipeId("Gen_Nuclear_Uranium"),
                Name = "Burn Uranium Fuel Rod",
                IsAlternate = false,
                TimeSeconds = 300.0,
                BuildingId = new BuildingId("Desc_GeneratorNuclear_C"),
                BuildingName = "Nuclear Power Plant",
                PowerOutputMw = 2500,
            }));
            Assert.That(
                uranium.Ingredients,
                Is.EqualTo(ExpectedSequence.Of(
                    new ItemAmount(new ItemId("Desc_NuclearFuelRod_C"), 1),
                    new ItemAmount(new ItemId("Desc_Water_C"), 1200.0))));
            Assert.That(uranium.Products, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_NuclearWaste_C"), 50))));
            Assert.That(plutonium.Id, Is.EqualTo(new RecipeId("Gen_Nuclear_Plutonium")));
            Assert.That(plutonium.Name, Is.EqualTo("Burn Plutonium Fuel Rod"));
            Assert.That(plutonium.TimeSeconds, Is.EqualTo(600.0));
            Assert.That(plutonium.Ingredients[1], Is.EqualTo(new ItemAmount(new ItemId("Desc_Water_C"), 2400.0)));
            Assert.That(plutonium.Products, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_PlutoniumWaste_C"), 10))));
        });
    }

    [Test]
    public async Task RecipeDefinitions_AreEveryRecipeInDocumentOrder()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.That(
            data.RecipeDefinitions.Select(r => r.Id.Value),
            Is.EqualTo(ExpectedSequence.Of(
                "Recipe_TradingPost_C", "Recipe_IngotIron_C", "Recipe_HandOnly_C", "Recipe_Alternate_Pure_C", "Recipe_Variable_C")));
        Assert.That(
            data.RecipeDefinitions[0].Products,
            Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_TradingPost_C"), 1))));
    }

    [Test]
    public async Task Schematics_KeepDocumentOrderAndMapTheirFields()
    {
        GameData data = await LoadAsync(TinyJson);

        Assert.That(
            data.Schematics.Select(s => s.Id.Value),
            Is.EqualTo(ExpectedSequence.Of("Schematic_1-1_C", "Schematic_Alt_C", "Schematic_Sink_C", "Schematic_Odd_C")));
        Schematic milestone = data.Schematics[0];
        Schematic alternate = data.Schematics[1];
        Schematic sink = data.Schematics[2];
        Assert.Multiple(() =>
        {
            Assert.That(milestone.Type, Is.EqualTo(SchematicType.Milestone));
            Assert.That(milestone.Name, Is.EqualTo("Base Building"));
            Assert.That(milestone.Tier, Is.EqualTo(1));
            Assert.That(milestone.Cost, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_IronIngot_C"), 10))));
            Assert.That(milestone.UnlocksRecipes, Is.EqualTo(ExpectedSequence.Of(new RecipeId("Recipe_IngotIron_C"))));
            Assert.That(alternate.Type, Is.EqualTo(SchematicType.Alternate));
            Assert.That(alternate.Required, Is.EqualTo(ExpectedSequence.Of(new SchematicId("Schematic_1-1_C"))));
            Assert.That(sink.Type, Is.EqualTo(SchematicType.ResourceSink));
            Assert.That(sink.Name, Is.EqualTo("Schematic_Sink_C"), "name falls back to the key");
            Assert.That(sink.Cost, Is.Empty);
            Assert.That(sink.UnlocksRecipes, Is.Empty);
            Assert.That(data.Schematics[3].Type, Is.EqualTo(SchematicType.Other));
        });
    }

    [TestCase("EST_Milestone", SchematicType.Milestone)]
    [TestCase("EST_Tutorial", SchematicType.Tutorial)]
    [TestCase("EST_MAM", SchematicType.Mam)]
    [TestCase("EST_Alternate", SchematicType.Alternate)]
    [TestCase("EST_HardDrive", SchematicType.HardDrive)]
    [TestCase("EST_ResourceSink", SchematicType.ResourceSink)]
    [TestCase("EST_Customization", SchematicType.Customization)]
    [TestCase("EST_Cheat", SchematicType.Other)]
    public async Task SchematicType_MapsTheDataFileNames(string jsonType, SchematicType expected)
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["schematics"]!["Schematic_1-1_C"]!["type"] = jsonType;

        GameData data = await LoadAsync(root.ToJsonString());

        Assert.That(data.Schematics[0].Type, Is.EqualTo(expected));
    }

    [Test]
    public async Task DuplicateKeys_KeepTheFirstPositionAndTheLastValue()
    {
        // Python json.load builds a dict: a repeated key keeps its first position and takes the last value.
        const string json = """
            { "items": { "Desc_A_C": { "name": "first" }, "Desc_B_C": { "name": "B" }, "Desc_A_C": { "name": "last" },
                         "Desc_NuclearFuelRod_C": { "name": "U", "energyValue": 1 }, "Desc_PlutoniumFuelRod_C": { "name": "P", "energyValue": 1 } },
              "recipes": {}, "schematics": {}, "resources": {}, "buildings": {} }
            """;

        GameData data = await LoadAsync(json);

        Assert.Multiple(() =>
        {
            Assert.That(data.Items.Select(i => i.Id.Value).Take(2), Is.EqualTo(ExpectedSequence.Of("Desc_A_C", "Desc_B_C")));
            Assert.That(data.Items[0].Name, Is.EqualTo("last"));
        });
    }

    [TestCase("recipes", "Recipe_IngotIron_C", "time")]
    [TestCase("recipes", "Recipe_IngotIron_C", "name")]
    [TestCase("recipes", "Recipe_IngotIron_C", "alternate")]
    [TestCase("recipes", "Recipe_IngotIron_C", "ingredients")]
    [TestCase("recipes", "Recipe_IngotIron_C", "products")]
    [TestCase("recipes", "Recipe_IngotIron_C", "producedIn")]
    [TestCase("recipes", "Recipe_IngotIron_C", "inMachine")]
    [TestCase("recipes", "Recipe_TradingPost_C", "className")]
    [TestCase("recipes", "Recipe_Variable_C", "maxPower")]
    [TestCase("recipes", "Recipe_Variable_C", "minPower")]
    [TestCase("items", "Desc_OreIron_C", "name")]
    [TestCase("items", "Desc_NuclearFuelRod_C", "energyValue")]
    [TestCase("buildings", "Desc_SmelterMk1_C", "name")]
    [TestCase("schematics", "Schematic_1-1_C", "type")]
    [TestCase("schematics", "Schematic_1-1_C", "tier")]
    [TestCase("schematics", "Schematic_1-1_C", "cost")]
    [TestCase("schematics", "Schematic_1-1_C", "unlock")]
    [TestCase("schematics", "Schematic_Alt_C", "cost")]
    public void MissingRequiredField_ThrowsNamingFieldAndObject(string section, string key, string field)
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root[section]![key]!.AsObject().Remove(field);

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>()
                .With.Message.Contains($"'{field}'")
                .And.Message.Contains($"{section}[\"{key}\"]"));
    }

    [TestCase("items")]
    [TestCase("buildings")]
    [TestCase("schematics")]
    [TestCase("resources")]
    [TestCase("recipes")]
    public void MissingTopLevelSection_Throws(string section)
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root.AsObject().Remove(section);

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>().With.Message.Contains($"'{section}'"));
    }

    [Test]
    public void MissingNestedField_NamesTheNestedObject()
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["recipes"]!["Recipe_IngotIron_C"]!["ingredients"]![0]!.AsObject().Remove("amount");

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>()
                .With.Message.Contains("'amount'")
                .And.Message.Contains("recipes[\"Recipe_IngotIron_C\"].ingredients[0]"));
    }

    [Test]
    public void MissingUnlockRecipes_NamesTheUnlockObject()
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["schematics"]!["Schematic_1-1_C"]!["unlock"]!.AsObject().Remove("recipes");

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>()
                .With.Message.Contains("'recipes'")
                .And.Message.Contains("schematics[\"Schematic_1-1_C\"].unlock"));
    }

    [Test]
    public void MissingFuelRodItem_Throws()
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["items"]!.AsObject().Remove("Desc_PlutoniumFuelRod_C");

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>().With.Message.Contains("Desc_PlutoniumFuelRod_C"));
    }

    [Test]
    public void WrongFieldType_Throws()
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["recipes"]!["Recipe_IngotIron_C"]!["time"] = "slow";

        Assert.That(
            () => LoadAsync(root.ToJsonString()),
            Throws.TypeOf<GameDataFormatException>()
                .With.Message.Contains("'time'")
                .And.Message.Contains("must be a number"));
    }

    [Test]
    public async Task OptionalFields_TakePythonDefaultsWhenAbsent()
    {
        JsonNode root = JsonNode.Parse(TinyJson)!;
        root["items"]!["Desc_Water_C"]!.AsObject().Remove("liquid");
        root["schematics"]!["Schematic_Alt_C"]!.AsObject().Remove("requiredSchematics");
        root["recipes"]!["Recipe_IngotIron_C"]!.AsObject().Remove("isVariablePower");
        root["recipes"]!["Recipe_TradingPost_C"]!.AsObject().Remove("producedIn");   // not read when inMachine is false

        GameData data = await LoadAsync(root.ToJsonString());

        Assert.Multiple(() =>
        {
            Assert.That(data.IsFluid(new ItemId("Desc_Water_C")), Is.False);
            Assert.That(data.Schematics[1].Required, Is.Empty);
            Assert.That(((ManufacturingRecipe)data.Recipes[0]).IsVariablePower, Is.False);
        });
    }

    [Test]
    public void InvalidJson_ThrowsGameDataFormatException()
    {
        Assert.That(
            () => LoadAsync("{ \"items\": "),
            Throws.TypeOf<GameDataFormatException>().With.InnerException.InstanceOf<JsonException>());
    }

    // ---- the real data file -------------------------------------------------------------------------------------

    [Test]
    public void RealData_Counts()
    {
        GameData data = CoreRealGameData.Data;

        Assert.Multiple(() =>
        {
            Assert.That(data.RecipeDefinitions, Has.Count.EqualTo(825));
            Assert.That(data.Recipes, Has.Count.EqualTo(278));
            Assert.That(data.Recipes.Take(276), Is.All.InstanceOf<ManufacturingRecipe>());
            Assert.That(data.Recipes.Skip(276), Is.All.InstanceOf<GeneratorRecipe>());
            Assert.That(data.Items, Has.Count.EqualTo(177));
            Assert.That(data.Buildings, Has.Count.EqualTo(528));
            Assert.That(data.Schematics, Has.Count.EqualTo(454));
        });
    }

    [Test]
    public void RealData_ResourcesInDocumentOrder()
    {
        GameData data = CoreRealGameData.Data;

        Assert.That(
            data.Resources.Select(r => r.Value),
            Is.EqualTo(ExpectedSequence.Of(
                "Desc_OreIron_C", "Desc_Coal_C", "Desc_NitrogenGas_C", "Desc_Sulfur_C", "Desc_Water_C", "Desc_SAM_C",
                "Desc_OreBauxite_C", "Desc_OreGold_C", "Desc_OreCopper_C", "Desc_RawQuartz_C", "Desc_Stone_C",
                "Desc_OreUranium_C", "Desc_LiquidOil_C")));
    }

    [Test]
    public void RealData_ManufacturingRecipesMatchTheFileInOrder()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.DataFile));
        var expected = document.RootElement.GetProperty("recipes").EnumerateObject()
            .Select(p => p.Value)
            .Where(r => r.GetProperty("inMachine").GetBoolean() && r.GetProperty("producedIn").GetArrayLength() > 0)
            .ToList();
        GameData data = CoreRealGameData.Data;

        Assert.That(expected, Has.Count.EqualTo(276));
        Assert.That(data.Recipes.Take(276).Select(r => r.Id.Value), Is.EqualTo(expected.Select(r => r.GetProperty("className").GetString())));
        JsonElement first = expected[0];
        var recipe = (ManufacturingRecipe)data.Recipes[0];
        Assert.Multiple(() =>
        {
            Assert.That(recipe.Id.Value, Is.EqualTo("Recipe_IronPlate_C"));
            Assert.That(recipe.Name, Is.EqualTo(first.GetProperty("name").GetString()));
            Assert.That(recipe.TimeSeconds, Is.EqualTo(first.GetProperty("time").GetDouble()));
            Assert.That(recipe.IsAlternate, Is.EqualTo(first.GetProperty("alternate").GetBoolean()));
            Assert.That(recipe.BuildingId.Value, Is.EqualTo(first.GetProperty("producedIn")[0].GetString()));
            Assert.That(
                recipe.Ingredients.Select(i => (i.Item.Value, i.Amount)),
                Is.EqualTo(first.GetProperty("ingredients").EnumerateArray()
                    .Select(i => (i.GetProperty("item").GetString(), i.GetProperty("amount").GetDouble()))));
            Assert.That(
                recipe.Products.Select(i => (i.Item.Value, i.Amount)),
                Is.EqualTo(first.GetProperty("products").EnumerateArray()
                    .Select(i => (i.GetProperty("item").GetString(), i.GetProperty("amount").GetDouble()))));
        });
    }

    [Test]
    public void RealData_GeneratorRecipesComputedFromTheFuelRodEnergy()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.DataFile));
        JsonElement items = document.RootElement.GetProperty("items");
        double uraniumSeconds = items.GetProperty("Desc_NuclearFuelRod_C").GetProperty("energyValue").GetDouble() / 2500;
        double plutoniumSeconds = items.GetProperty("Desc_PlutoniumFuelRod_C").GetProperty("energyValue").GetDouble() / 2500;
        GameData data = CoreRealGameData.Data;

        var uranium = (GeneratorRecipe)data.Recipes[276];
        var plutonium = (GeneratorRecipe)data.Recipes[277];
        Assert.Multiple(() =>
        {
            Assert.That(uranium.Id.Value, Is.EqualTo("Gen_Nuclear_Uranium"));
            Assert.That(uranium.TimeSeconds, Is.EqualTo(uraniumSeconds));
            Assert.That(uranium.TimeSeconds, Is.EqualTo(300.0));
            Assert.That(uranium.Ingredients[1], Is.EqualTo(new ItemAmount(new ItemId("Desc_Water_C"), 240 * uraniumSeconds / 60)));
            Assert.That(uranium.Products[0], Is.EqualTo(new ItemAmount(new ItemId("Desc_NuclearWaste_C"), 50)));
            Assert.That(uranium.BuildingId.Value, Is.EqualTo("Desc_GeneratorNuclear_C"));
            Assert.That(uranium.PowerOutputMw, Is.EqualTo(2500));
            Assert.That(plutonium.Id.Value, Is.EqualTo("Gen_Nuclear_Plutonium"));
            Assert.That(plutonium.TimeSeconds, Is.EqualTo(plutoniumSeconds));
            Assert.That(plutonium.Ingredients[0], Is.EqualTo(new ItemAmount(new ItemId("Desc_PlutoniumFuelRod_C"), 1)));
            Assert.That(plutonium.Ingredients[1], Is.EqualTo(new ItemAmount(new ItemId("Desc_Water_C"), 240 * plutoniumSeconds / 60)));
            Assert.That(plutonium.Products[0], Is.EqualTo(new ItemAmount(new ItemId("Desc_PlutoniumWaste_C"), 10)));
        });
    }

    [Test]
    public void RealData_BuildingsAndVariablePower()
    {
        GameData data = CoreRealGameData.Data;
        var darkMatter = (ManufacturingRecipe)data.Recipes.Single(r => r.Id.Value == "Recipe_DarkMatter_C");

        Assert.Multiple(() =>
        {
            Assert.That(data.Buildings[0].Id.Value, Is.EqualTo("Desc_RailroadTrack_C"));
            Assert.That(data.GetBuilding(new BuildingId("Desc_SmelterMk1_C")).PowerConsumptionMw, Is.EqualTo(4));
            Assert.That(data.Buildings, Is.All.Matches<Building>(b => b.PowerConsumptionIsInteger));
            Assert.That(darkMatter.IsVariablePower, Is.True);
            Assert.That(darkMatter.MinPowerMw, Is.EqualTo(500));
            Assert.That(darkMatter.MaxPowerMw, Is.EqualTo(1500));
            Assert.That(data.IsFluid(new ItemId("Desc_Water_C")), Is.True);
            Assert.That(data.ItemName(new ItemId("Desc_IronPlate_C")), Is.EqualTo("Iron Plate"));
        });
    }
}
