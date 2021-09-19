using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace template.infrastructure.Entities.Maps
{
    public class EntityMap<TEntity> : BaseEntityMap<TEntity> where TEntity : class
    {
        protected override void InternalBeforeSaveChanges(ChangeTracker changetracker, IHttpContextAccessor accessor, IConfiguration configuration)
        {
        }

        protected override void InternalMap(EntityTypeBuilder<TEntity> builder)
        {
            builder.ToTable(typeof(TEntity).Name);
        }
    }
}
