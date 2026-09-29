using System.Globalization;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>A group of new machines, all running the same recipe, built in one tier.</summary>
public sealed class MachineGroupNode : TierGraphNode
{
    /// <summary>Initializes a new machine group node; its id is the recipe class name.</summary>
    /// <param name="recipe">The recipe the machines run.</param>
    /// <param name="newMachines">How many new machines this tier builds.</param>
    /// <param name="productName">The display name of the recipe's main product.</param>
    /// <param name="buildingName">The display name of the building.</param>
    /// <param name="makesNewItem">Whether the main product is one of the tier's new items.</param>
    public MachineGroupNode(Recipe recipe, int newMachines, string productName, string buildingName, bool makesNewItem)
        : base(recipe?.Id.Value ?? throw new ArgumentNullException(nameof(recipe)))
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(buildingName);
        Recipe = recipe;
        NewMachines = newMachines;
        ProductName = productName;
        BuildingName = buildingName;
        MakesNewItem = makesNewItem;
    }

    /// <summary>Gets the recipe the machines run.</summary>
    public Recipe Recipe { get; }

    /// <summary>Gets how many new machines this tier builds.</summary>
    public int NewMachines { get; }

    /// <summary>Gets the display name of the recipe's main product.</summary>
    public string ProductName { get; }

    /// <summary>Gets the display name of the building.</summary>
    public string BuildingName { get; }

    /// <summary>Gets a value indicating whether the main product is one of the tier's new items.</summary>
    public bool MakesNewItem { get; }

    /// <summary>Gets the 1-based build step; set by <see cref="ITierBuildOrder"/>, 0 until then.</summary>
    public int Step { get; internal set; }

    /// <inheritdoc />
    public override TierGraphNodeKind Kind => TierGraphNodeKind.Machine;

    /// <inheritdoc />
    public override string Label => string.Create(
        CultureInfo.InvariantCulture,
        $"#{Step} {ProductName}\n{NewMachines} x {BuildingName}\n{Recipe.Name}");
}
