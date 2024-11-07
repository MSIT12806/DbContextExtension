using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using System.Linq.Expressions;
namespace DbContextExtension
{

    public class DbSetUnikey<TEntity> : DbSet<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet;

        protected Func<TEntity, Expression<Func<TEntity, bool>>> findByUnikey;
        public override IEntityType EntityType => this._dbSet.EntityType;
        public DbSetUnikey(DbSet<TEntity> dbSet, Func<TEntity, Expression<Func<TEntity, bool>>> unikeyMapper)
        {
            this._dbSet = dbSet;
            this.findByUnikey = unikeyMapper;
        }

        public TEntity FindByUnikey(TEntity data)
        {
            var entity = this._dbSet.First(this.findByUnikey(data));
            return entity;
        }
    }
    public class DbSetWithCache<TEntity> : DbSet<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet;
        private readonly ConcurrentDictionary<string, TEntity> _cache;

        private readonly Func<TEntity, string> getKey;
        public DbSetWithCache(DbSet<TEntity> dbSet, Func<TEntity, string> getIdFunc)
        {
            this._dbSet = dbSet;
            this._cache = new ConcurrentDictionary<string, TEntity>();
            this.getKey = getIdFunc;
        }

        public override IEntityType EntityType => this._dbSet.EntityType;

        // 包裝 Add 方法，將實體添加到字典中
        public override EntityEntry<TEntity> Add(TEntity entity)
        {
            var key = this.getKey(entity);
            this._cache.TryAdd(key, entity);
            return this._dbSet.Add(entity);
        }

        // 查詢方法，先查字典再查資料庫
        public TEntity FindById(string id)
        {
            if (this._cache.TryGetValue(id, out TEntity cachedEntity))
            {
                return cachedEntity;
            }

            var entity = this._dbSet.Find(id);
            return entity;
        }

    }
}
