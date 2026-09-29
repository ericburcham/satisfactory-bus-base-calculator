using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>
/// Python <c>unlock_tiers</c>, <c>building_tier</c>, <c>recipe_tier</c> and <c>raw_tier</c>. Milestones and tutorials
/// carry their own tier; MAM research and hard drives take the tier of the MAM and of the items needed to research
/// them. The computed recipe tiers are then overridden by <see cref="PlannerConstants.TierOverrides"/>.
/// </summary>
public sealed class UnlockTierResolver : IUnlockTierResolver
{
    private readonly OrderedDictionary<RecipeId, int> _recipeTiers;

    /// <summary>Computes the unlock tiers of the game data.</summary>
    /// <param name="data">The game data (its schematics and recipe definitions).</param>
    public UnlockTierResolver(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _recipeTiers = Compute(data);
    }

    /// <summary>
    /// The unlock tier of every recipe some considered schematic unlocks, plus the overrides, in Python
    /// <c>rec_tier</c> insertion order.
    /// </summary>
    public IReadOnlyDictionary<RecipeId, int> RecipeUnlockTiers => _recipeTiers;

    /// <inheritdoc />
    public int RecipeTier(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        int own = _recipeTiers.GetValueOrDefault(recipe.Id, 0);
        int building = BuildingTier(recipe.BuildingId);
        return building > own ? building : own;
    }

    /// <inheritdoc />
    public int BuildingTier(BuildingId building)
    {
        // Python: "Recipe_" + bcls[len("Desc_"):] -- a slice past the end is empty.
        string value = building.Value;
        int cut = PlannerConstants.BuildingDescriptorPrefix.Length;
        string rest = value.Length > cut ? value[cut..] : string.Empty;
        return _recipeTiers.GetValueOrDefault(new RecipeId(PlannerConstants.BuildingRecipePrefix + rest), 0);
    }

    /// <inheritdoc />
    public int RawResourceTier(ItemId resource) =>
        BuildingTier(PlannerConstants.ExtractorBuildings.GetValueOrDefault(resource, PlannerConstants.DefaultExtractorBuilding));

    private static OrderedDictionary<RecipeId, int> Compute(GameData data)
    {
        var itemTier = new OrderedDictionary<ItemId, int>();
        foreach (ItemId resource in data.Resources)
        {
            itemTier[resource] = 0;
        }

        var recipeTier = new OrderedDictionary<RecipeId, int>();
        var schematicTier = new OrderedDictionary<SchematicId, int>();
        var recipes = new Dictionary<RecipeId, RecipeDefinition>();
        foreach (RecipeDefinition definition in data.RecipeDefinitions)
        {
            recipes[definition.Id] = definition;
        }

        for (int pass = 0; pass < PlannerConstants.UnlockTierPasses; pass++)
        {
            bool changed = false;
            foreach (Schematic schematic in data.Schematics)
            {
                changed |= ApplySchematic(schematic, itemTier, recipeTier, schematicTier);
            }

            foreach ((RecipeId id, int tier) in recipeTier)
            {
                if (!recipes.TryGetValue(id, out RecipeDefinition? recipe))
                {
                    continue;
                }

                int effective = tier;
                bool known = true;
                foreach (ItemAmount ingredient in recipe.Ingredients)
                {
                    if (!itemTier.TryGetValue(ingredient.Item, out int ingredientTier))
                    {
                        known = false;
                        break;
                    }

                    effective = Math.Max(effective, ingredientTier);
                }

                if (!known)
                {
                    continue;
                }

                foreach (ItemAmount product in recipe.Products)
                {
                    if (itemTier.GetValueOrDefault(product.Item, PlannerConstants.UnknownItemTier) > effective)
                    {
                        itemTier[product.Item] = effective;
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                break;
            }
        }

        foreach ((RecipeId id, int tier) in PlannerConstants.TierOverrides)
        {
            recipeTier[id] = tier;
        }

        return recipeTier;
    }

    private static bool ApplySchematic(
        Schematic schematic,
        OrderedDictionary<ItemId, int> itemTier,
        OrderedDictionary<RecipeId, int> recipeTier,
        OrderedDictionary<SchematicId, int> schematicTier)
    {
        int tier;
        switch (schematic.Type)
        {
            case SchematicType.Milestone:
            case SchematicType.Tutorial:
                tier = schematic.Tier;
                break;
            case SchematicType.Mam:
            case SchematicType.Alternate:
            case SchematicType.HardDrive:
                // MAM research and hard drives need a MAM.
                if (!recipeTier.TryGetValue(PlannerConstants.MamRecipe, out tier))
                {
                    return false;
                }

                break;
            default:
                return false;
        }

        foreach (SchematicId dependency in schematic.Required)
        {
            if (!schematicTier.TryGetValue(dependency, out int dependencyTier))
            {
                return false;
            }

            tier = Math.Max(tier, dependencyTier);
        }

        foreach (ItemAmount cost in schematic.Cost)
        {
            // Gathered items (not made by any unlocked recipe yet) count as tier 0.
            tier = Math.Max(tier, itemTier.GetValueOrDefault(cost.Item, 0));
        }

        bool changed = false;
        if (!schematicTier.TryGetValue(schematic.Id, out int previous) || previous != tier)
        {
            schematicTier[schematic.Id] = tier;
            changed = true;
        }

        foreach (RecipeId recipe in schematic.UnlocksRecipes)
        {
            if (!recipeTier.TryGetValue(recipe, out int current) || tier < current)
            {
                recipeTier[recipe] = tier;
                changed = true;
            }
        }

        return changed;
    }
}
