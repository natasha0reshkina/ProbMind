using System.Linq.Expressions;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Domain.Common;

namespace ProbMind.UnitTests.TestDoubles;

public sealed class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    private readonly List<TEntity> _items = [];

    public IReadOnlyList<TEntity> Items => _items;

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TEntity>>(_items.ToArray());

    public Task<IReadOnlyList<TEntity>> WhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IReadOnlyList<TEntity>>(_items.Where(compiled).ToArray());
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(predicate.Compile()));

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(predicate is null ? _items.Count : _items.Count(predicate.Compile()));

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
        var index = _items.FindIndex(x => x.Id == entity.Id);
        if (index >= 0) _items[index] = entity;
        else _items.Add(entity);
    }

    public void Remove(TEntity entity) => _items.RemoveAll(x => x.Id == entity.Id);
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        var ids = entities.Select(x => x.Id).ToHashSet();
        _items.RemoveAll(x => ids.Contains(x.Id));
    }

    public void Seed(params TEntity[] entities) => _items.AddRange(entities);
}
