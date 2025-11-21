using Microsoft.EntityFrameworkCore.Storage;

namespace pto.track.services;

/// <summary>
/// Defines the contract for coordinating the work of multiple repositories by creating a single database context
/// shared by all of them. This allows multiple operations to be executed within a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A transaction object that can be committed or rolled back.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction, persisting all changes to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
