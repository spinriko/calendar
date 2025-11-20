using System.Linq.Expressions;

namespace pto.track.services.Specifications;

/// <summary>
/// Specification pattern interface for encapsulating query logic.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria expression.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading related entities.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of include expressions using string-based navigation properties.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression for ascending sorting.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by expression for descending sorting.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the number of records to skip (for paging).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the number of records to take (for paging).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether this is a no-tracking query.
    /// </summary>
    bool AsNoTracking { get; }
}
