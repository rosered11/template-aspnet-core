using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace template.infrastructure.Entities
{
    public abstract class BaseEntityMap<TEntityType> : IEntityTypeMap
         where TEntityType : class
    {
        public void Map(ModelBuilder builder)
        {
            InternalMap(builder.Entity<TEntityType>());
        }

        public void BeforeSaveChanges(ChangeTracker changetracker, IHttpContextAccessor accessor, IConfiguration configuration)
        {
            InternalBeforeSaveChanges(changetracker, accessor, configuration);
        }

        protected abstract void InternalMap(EntityTypeBuilder<TEntityType> builder);

        protected abstract void InternalBeforeSaveChanges(ChangeTracker changetracker, IHttpContextAccessor accessor, IConfiguration configuration);
    }
}
