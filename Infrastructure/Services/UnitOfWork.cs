using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Services;

public class UnitOfWork : IUnitOfWork
{
	private readonly AppDbContext _db;

	public UnitOfWork(AppDbContext db)
	{
		_db = db;
	}

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> _db.SaveChangesAsync(cancellationToken);

	public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
	{

		return _db.Database.BeginTransactionAsync(cancellationToken);
	}

	private class NoOpTransaction : IDbContextTransaction
	{
		public Guid TransactionId => Guid.NewGuid();
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
		public void Commit() { }
		public void Rollback() { }
		public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
