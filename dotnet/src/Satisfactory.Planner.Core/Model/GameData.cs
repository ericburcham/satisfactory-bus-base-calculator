using System.Diagnostics.CodeAnalysis;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>
/// The game data the planner works on: the data file's items, buildings, schematics and resources in document order,
/// the recipes the planner can run and every recipe definition.
/// </summary>
public sealed class GameData
{
    private readonly Dictionary<ItemId, Item> _itemsById;
    private readonly Dictionary<BuildingId, Building> _buildingsById;
    private readonly HashSet<ItemId> _resources;

    /// <summary>Creates the game data. Every sequence is copied; its order is kept.</summary>
    /// <param name="items">The items (unique ids), in data file order.</param>
    /// <param name="buildings">The buildings (unique ids), in data file order.</param>
    /// <param name="schematics">The schematics, in data file order.</param>
    /// <param name="resources">The natural resources (keys of <c>resources</c>, unique), in data file order.</param>
    /// <param name="recipes">
    /// The recipes the planner can run: manufacturing recipes in data file order, then the generator recipes.
    /// </param>
    /// <param name="recipeDefinitions">Every data file recipe, in data file order.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A sequence contains <see langword="null"/> or a duplicate item, building or resource id.</exception>
    public GameData(
        IEnumerable<Item> items,
        IEnumerable<Building> buildings,
        IEnumerable<Schematic> schematics,
        IEnumerable<ItemId> resources,
        IEnumerable<Recipe> recipes,
        IEnumerable<RecipeDefinition> recipeDefinitions)
    {
        Items = CopyOf(items, nameof(items));
        Buildings = CopyOf(buildings, nameof(buildings));
        Schematics = CopyOf(schematics, nameof(schematics));
        Recipes = CopyOf(recipes, nameof(recipes));
        RecipeDefinitions = CopyOf(recipeDefinitions, nameof(recipeDefinitions));
        ArgumentNullException.ThrowIfNull(resources);
        Resources = resources.ToList().AsReadOnly();

        _itemsById = [];
        foreach (Item item in Items)
        {
            if (!_itemsById.TryAdd(item.Id, item))
            {
                throw new ArgumentException($"Duplicate item id {item.Id}.", nameof(items));
            }
        }

        _buildingsById = [];
        foreach (Building building in Buildings)
        {
            if (!_buildingsById.TryAdd(building.Id, building))
            {
                throw new ArgumentException($"Duplicate building id {building.Id}.", nameof(buildings));
            }
        }

        _resources = [];
        foreach (ItemId resource in Resources)
        {
            if (!_resources.Add(resource))
            {
                throw new ArgumentException($"Duplicate resource id {resource}.", nameof(resources));
            }
        }
    }

    /// <summary>The items, in data file order.</summary>
    public IReadOnlyList<Item> Items { get; }

    /// <summary>The buildings, in data file order.</summary>
    public IReadOnlyList<Building> Buildings { get; }

    /// <summary>The schematics, in data file order.</summary>
    public IReadOnlyList<Schematic> Schematics { get; }

    /// <summary>The natural resources (keys of the data file's <c>resources</c> object), in data file order.</summary>
    public IReadOnlyList<ItemId> Resources { get; }

    /// <summary>
    /// The recipes the planner can run: every recipe with <c>inMachine</c> true and a non-empty <c>producedIn</c>, in data
    /// file order, followed by the generator pseudo-recipes (Python <c>Planner.recipes</c> after <c>__init__</c>).
    /// </summary>
    public IReadOnlyList<Recipe> Recipes { get; }

    /// <summary>Every recipe of the data file's <c>recipes</c> object, in data file order.</summary>
    public IReadOnlyList<RecipeDefinition> RecipeDefinitions { get; }

    /// <summary>Returns the item with the given id.</summary>
    /// <param name="id">The item id.</param>
    /// <returns>The item.</returns>
    /// <exception cref="KeyNotFoundException">There is no such item.</exception>
    public Item GetItem(ItemId id) =>
        _itemsById.TryGetValue(id, out Item? item) ? item : throw new KeyNotFoundException($"Unknown item {id}.");

    /// <summary>Looks up an item.</summary>
    /// <param name="id">The item id.</param>
    /// <param name="item">The item when found.</param>
    /// <returns><see langword="true"/> when the item exists.</returns>
    public bool TryGetItem(ItemId id, [MaybeNullWhen(false)] out Item item) => _itemsById.TryGetValue(id, out item);

    /// <summary>Returns the building with the given id.</summary>
    /// <param name="id">The building id.</param>
    /// <returns>The building.</returns>
    /// <exception cref="KeyNotFoundException">There is no such building.</exception>
    public Building GetBuilding(BuildingId id) =>
        _buildingsById.TryGetValue(id, out Building? building)
            ? building
            : throw new KeyNotFoundException($"Unknown building {id}.");

    /// <summary>Looks up a building.</summary>
    /// <param name="id">The building id.</param>
    /// <param name="building">The building when found.</param>
    /// <returns><see langword="true"/> when the building exists.</returns>
    public bool TryGetBuilding(BuildingId id, [MaybeNullWhen(false)] out Building building) =>
        _buildingsById.TryGetValue(id, out building);

    /// <summary>The display name of an item, or its class name when the item is unknown (Python <c>name</c>).</summary>
    /// <param name="id">The item id.</param>
    /// <returns>The item's name or <paramref name="id"/>'s value.</returns>
    public string ItemName(ItemId id) => _itemsById.TryGetValue(id, out Item? item) ? item.Name : id.Value;

    /// <summary>Whether an item is a fluid; unknown items are not (Python <c>is_fluid</c>).</summary>
    /// <param name="id">The item id.</param>
    /// <returns>The item's <see cref="Item.IsFluid"/>, or <see langword="false"/> when unknown.</returns>
    public bool IsFluid(ItemId id) => _itemsById.TryGetValue(id, out Item? item) && item.IsFluid;

    /// <summary>Whether an item is a natural resource (a key of the data file's <c>resources</c> object).</summary>
    /// <param name="id">The item id.</param>
    /// <returns><see langword="true"/> for a natural resource.</returns>
    public bool IsResource(ItemId id) => _resources.Contains(id);

    private static System.Collections.ObjectModel.ReadOnlyCollection<T> CopyOf<T>(IEnumerable<T> source, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);
        var copy = source.ToList();
        if (copy.Exists(x => x is null))
        {
            throw new ArgumentException("The sequence contains null.", parameterName);
        }

        return copy.AsReadOnly();
    }
}
