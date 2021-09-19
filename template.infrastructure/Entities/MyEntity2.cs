using System.Collections.Generic;
using template.infrastructure.Attributes;

namespace template.infrastructure.Entities
{
    [GeneratedEntity]
    public class MyEntity2 : BaseEntity
    {
        public string Data2 { get; set; }
        public IEnumerable<MyEntity> MyEntity { get; set; }
    }
}