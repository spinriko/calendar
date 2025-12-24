using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.services.tests;

public class UnitOfWorkTests : TestBase
{
    [Fact]
    public async Task SaveChangesAsync_WhenCalled_SavesChangesToDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        var resource = new Resource { Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.NotEqual(0, resource.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var resource = new Resource { Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await unitOfWork.SaveChangesAsync(cts.Token));
    }


    [Fact]
    public async Task CommitTransactionAsync_CommitsTransaction()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync();

        var resource = new Resource { Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);
        await unitOfWork.SaveChangesAsync();

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        Assert.Null(context.Database.CurrentTransaction);

        // Verify data was committed
        var savedResource = await context.Resources.FirstOrDefaultAsync(r => r.Name == "Test Resource");
        Assert.NotNull(savedResource);
    }


    [Fact]
    public async Task Transaction_MultipleOperations_CommitsAllOrNothing()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        // Act - Successful transaction
        await unitOfWork.BeginTransactionAsync();

        var resource1 = new Resource { Name = "Resource 1", GroupId = 1 };
        var resource2 = new Resource { Name = "Resource 2", GroupId = 1 };
        context.Resources.Add(resource1);
        context.Resources.Add(resource2);

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitTransactionAsync();

        // Assert
        var count = await context.Resources.CountAsync();
        Assert.Equal(2, count);
    }


    [Fact]
    public async Task SaveChangesAsync_WithoutTransaction_SavesImmediately()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        var resource = new Resource { Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);

        // Act
        await unitOfWork.SaveChangesAsync();

        // Assert - Data should be saved even without explicit transaction
        var savedResource = await context.Resources.FirstOrDefaultAsync(r => r.Name == "Test Resource");
        Assert.NotNull(savedResource);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionAlreadyActive_DoesNotStartNewTransaction()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        // Act
        await unitOfWork.BeginTransactionAsync();
        var firstTransaction = context.Database.CurrentTransaction;

        await unitOfWork.BeginTransactionAsync();
        var secondTransaction = context.Database.CurrentTransaction;

        // Assert
        Assert.Same(firstTransaction, secondTransaction);

        // Cleanup
        await unitOfWork.RollbackTransactionAsync();
    }

    [Fact]
    public async Task CommitTransactionAsync_WhenNoActiveTransaction_DoesNotThrow()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        // Act & Assert - Should not throw
        await unitOfWork.CommitTransactionAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_WhenNoActiveTransaction_DoesNotThrow()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = CreateUnitOfWork(context);

        // Act & Assert - Should not throw
        await unitOfWork.RollbackTransactionAsync();
    }
}
