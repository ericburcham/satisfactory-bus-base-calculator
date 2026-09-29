using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>
/// A leftover fluid that nothing can turn into something sinkable (Python <c>Undisposable</c>). The optimiser catches it
/// and forbids recipes that leave the fluid over as a byproduct.
/// </summary>
public sealed class UndisposableFluidException : PlannerException
{
    /// <summary>Creates an exception with a default message and an empty fluid id.</summary>
    public UndisposableFluidException()
        : this("A leftover fluid cannot be turned into something sinkable.")
    {
    }

    /// <summary>Creates an exception with the given message and an empty fluid id.</summary>
    /// <param name="message">The message.</param>
    public UndisposableFluidException(string message)
        : base(message)
    {
        Fluid = new ItemId(string.Empty);
    }

    /// <summary>Creates an exception with the given message, the exception that caused it and an empty fluid id.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public UndisposableFluidException(string message, Exception innerException)
        : base(message, innerException)
    {
        Fluid = new ItemId(string.Empty);
    }

    /// <summary>Creates an exception for a fluid; the message names its id.</summary>
    /// <param name="fluid">The fluid that cannot be disposed of.</param>
    public UndisposableFluidException(ItemId fluid)
        : base($"No recipe turns leftover {fluid} into something sinkable.")
    {
        Fluid = fluid;
    }

    /// <summary>The fluid that cannot be disposed of (empty when the exception was created without one).</summary>
    public ItemId Fluid { get; }
}
