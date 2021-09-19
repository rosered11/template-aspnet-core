using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace template.infrastructure.Entities.Maps
{
    public class MyEntity2Map : EntityMap<MyEntity2>
    {
        protected override void InternalMap(EntityTypeBuilder<MyEntity2> builder)
        {
            builder.ToTable("MyEntity2");

            builder
                .HasMany(b => b.MyEntity)
                .WithOne();
        }
    }
}