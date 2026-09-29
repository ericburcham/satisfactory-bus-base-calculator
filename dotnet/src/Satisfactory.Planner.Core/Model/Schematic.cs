using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>An entry of the data file's <c>schematics</c> object.</summary>
/// <param name="Id">The schematic's key in <c>schematics</c>.</param>
/// <param name="Name">The display name (<c>name</c>; the key when absent).</param>
/// <param name="Type">The <c>type</c>.</param>
/// <param name="Tier">The <c>tier</c> (0 when absent; required for milestones and tutorials).</param>
/// <param name="Required">The <c>requiredSchematics</c> (empty when absent), in data file order.</param>
/// <param name="Cost">The <c>cost</c> items, in data file order.</param>
/// <param name="UnlocksRecipes">The <c>unlock.recipes</c> class names, in data file order.</param>
public sealed record Schematic(
    SchematicId Id,
    string Name,
    SchematicType Type,
    int Tier,
    IReadOnlyList<SchematicId> Required,
    IReadOnlyList<ItemAmount> Cost,
    IReadOnlyList<RecipeId> UnlocksRecipes);
