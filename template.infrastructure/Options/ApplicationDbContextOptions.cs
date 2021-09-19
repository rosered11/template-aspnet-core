using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using template.infrastructure.Entities;

namespace template.infrastructure.Options
{
    public class ApplicationDbContextOptions
    {
        public readonly DbContextOptions<ApplicationDbContext> Options;
        public readonly IEnumerable<IEntityTypeMap> Mappings;

        public ApplicationDbContextOptions(DbContextOptions<ApplicationDbContext> options, IEnumerable<IEntityTypeMap> mappings)
        {
            Options = options;
            Mappings = mappings;
        }
    }
}
