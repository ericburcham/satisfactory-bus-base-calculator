namespace Satisfactory.Planner.Core.Progression;

/// <summary>Creates the <see cref="ITierContext"/> of a tier.</summary>
public interface ITierContextFactory
{
    /// <summary>Creates the context of a tier.</summary>
    /// <param name="tier">The tier.</param>
    /// <returns>A new context.</returns>
    ITierContext Create(int tier);
}
