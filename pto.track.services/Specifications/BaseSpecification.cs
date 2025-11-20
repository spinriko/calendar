using System.Linq.Expressions;

namespace pto.track.services.Specifications;

/// <summary>
/// Base specification class that provides a default implementation of ISpecification.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to.</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification()
    {
    }

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include for eager loading.
    /// </summary>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds an order by ascending expression.
    /// </summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Adds an order by descending expression.
    /// </summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies paging to the specification.
    /// </summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Enables change tracking for this query.
    /// </summary>
    protected virtual void EnableTracking()
    {
        AsNoTracking = false;
    }

    /// <summary>
    /// Combines criteria expressions using AND logic.
    /// </summary>
    protected virtual void And(Expression<Func<T, bool>> additionalCriteria)
    {
        if (Criteria == null)
        {
            Criteria = additionalCriteria;
        }
        else
        {
            var parameter = Expression.Parameter(typeof(T));
            var combined = Expression.AndAlso(
                Expression.Invoke(Criteria, parameter),
                Expression.Invoke(additionalCriteria, parameter)
            );
            Criteria = Expression.Lambda<Func<T, bool>>(combined, parameter);
        }
    }
}
