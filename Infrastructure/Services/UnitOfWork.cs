using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Unit of Work implementation with transaction support for ACID compliance
/// </summary>
public class UnitOfWork : IUnitOfWork
{
	private readonly AppDbContext _db;
	private readonly ILogger<UnitOfWork>? _logger;

	public UnitOfWork(AppDbContext db, ILogger<UnitOfWork>? logger = null)
	{
		_db = db;
		_logger = logger;
	}

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> _db.SaveChangesAsync(cancellationToken);

	public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		return _db.Database.BeginTransactionAsync(cancellationToken);
	}

	public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
	{
		return _db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
	}

	public async Task<TResult> ExecuteInTransactionAsync<TResult>(
		Func<CancellationToken, Task<TResult>> action,
		CancellationToken cancellationToken = default)
	{
		// Check if we're already in a transaction
		if (_db.Database.CurrentTransaction is not null)
		{
			_logger?.LogDebug("Already in transaction, executing action without new transaction");
			return await action(cancellationToken);
		}

		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			_logger?.LogDebug("Transaction {TransactionId} started", transaction.TransactionId);

			var result = await action(cancellationToken);
			await _db.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			_logger?.LogDebug("Transaction {TransactionId} committed", transaction.TransactionId);

			return result;
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Transaction {TransactionId} rolling back due to error",
				transaction.TransactionId);

			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task ExecuteInTransactionAsync(
		Func<CancellationToken, Task> action,
		CancellationToken cancellationToken = default)
	{
		await ExecuteInTransactionAsync(async ct =>
		{
			await action(ct);
			return true;
		}, cancellationToken);
	}
}
