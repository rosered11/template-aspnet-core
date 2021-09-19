using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;

namespace template.infrastructure.Entities
{
    public interface IEntityTypeMap
    {
        void Map(ModelBuilder builder);

        void BeforeSaveChanges(ChangeTracker changetracker, IHttpContextAccessor accessor, IConfiguration configuration);
    }
}
