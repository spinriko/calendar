using Microsoft.EntityFrameworkCore;

namespace pto.track.services.Specifications;

/// <summary>
/// Extension methods for applying specifications to IQueryable.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies the specification to the queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="inputQuery">The input queryable.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The queryable with the specification applied.</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> specification) where T : class
    {
        var query = inputQuery;

        // Apply criteria (WHERE clause)
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes (eager loading)
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply string-based includes
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply paging
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        // Apply no-tracking if specified
        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}

/// <summary>
/// Extension methods for IQueryable to apply specifications.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies a specification to the queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to apply the specification to.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The query with the specification applied.</returns>
    public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, ISpecification<T> specification) where T : class
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}
