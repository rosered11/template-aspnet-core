using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace template.infrastructure.Repositories
{
    public class Repository<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext _context;
        private readonly ISieveProcessor _processor;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(ApplicationDbContext context, ISieveProcessor processor)
        {
            _context = context;
            _processor = processor;
            _dbSet = context.Set<TEntity>();
        }

        public TEntity Get(Guid id)
        {
            return _dbSet.Find(id);
        }

        public IEnumerable<TEntity> Get(SieveModel query)
        {
            var result = _processor.Apply(query, _dbSet);

            return result.AsEnumerable();
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _dbSet.ToList();
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Where(predicate).AsNoTracking();
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, SieveModel query)
        {
            var result = _processor.Apply(query, _dbSet);
            return result.Where(predicate).AsNoTracking();
        }

        public IQueryable<TEntity> FindOrderDescending(Expression<Func<TEntity, bool>> predicate, SieveModel query)
        {
            // Set sorting on Sieve
            query.Sorts = $"{query.Sorts},-created";
            var result = Find(predicate, query);
            return result;
        }

        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.SingleOrDefault(predicate);
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public async Task AddAsync(TEntity entity)
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            _dbSet.AddRange(entities);
            _context.SaveChanges();
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
            _context.SaveChanges();
        }

        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            _dbSet.UpdateRange(entities);
            _context.SaveChanges();
        }
    }
}
