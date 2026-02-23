using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces;

/// <summary>
/// Unit of Work interface for managing database transactions and persistence
/// </summary>
public interface IUnitOfWork
{
	/// <summary>
	/// Saves all pending changes to the database
	/// </summary>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Begins a new database transaction
	/// </summary>
	Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Begins a new database transaction with the specified isolation level
	/// </summary>
	Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an action within a transaction with automatic commit/rollback
	/// </summary>
	/// <typeparam name="TResult">The result type</typeparam>
	/// <param name="action">The action to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The result of the action</returns>
	Task<TResult> ExecuteInTransactionAsync<TResult>(
		Func<CancellationToken, Task<TResult>> action,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an action within a transaction with automatic commit/rollback (no return value)
	/// </summary>
	/// <param name="action">The action to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ExecuteInTransactionAsync(
		Func<CancellationToken, Task> action,
		CancellationToken cancellationToken = default);
}
