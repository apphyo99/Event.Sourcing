namespace EventSourcing.BuildingBlocks.Application.Services;

/// <summary>
/// Marker interface for application services
/// </summary>
public interface IApplicationService
{
}

/// <summary>
/// Unit of Work pattern for managing transactional boundaries
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Begins a new transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the underlying data store
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of changes saved</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base application service with common functionality
/// </summary>
public abstract class ApplicationService : IApplicationService
{
    protected readonly IUnitOfWork UnitOfWork;

    protected ApplicationService(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Executes an operation within a transaction boundary
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    protected async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within a transaction boundary and returns a result
    /// </summary>
    /// <typeparam name="TResult">The type of result</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await UnitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
