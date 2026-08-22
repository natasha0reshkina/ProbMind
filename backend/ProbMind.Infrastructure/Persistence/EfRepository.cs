using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Domain.Common;

namespace ProbMind.Infrastructure.Persistence;

public sealed class EfRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    private readonly ProbMindDbContext _db;
    private readonly DbSet<TEntity> _set;

    public EfRepository(ProbMindDbContext db)
    {
        _db = db;
        _set = db.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _set.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await _set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> WhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await _set.Where(predicate).ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        _set.AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? _set.CountAsync(cancellationToken)
            : _set.CountAsync(predicate, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        _set.AddAsync(entity, cancellationToken).AsTask();

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        await _set.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => _set.Update(entity);
    public void Remove(TEntity entity) => _set.Remove(entity);
    public void RemoveRange(IEnumerable<TEntity> entities) => _set.RemoveRange(entities);
}
