using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using template.infrastructure.Attributes;

namespace template.infrastructure.Utilities
{
    public static class ModelTypes
    {
        public static IList<TypeInfo> Get(string name)
        {
            var asm = typeof(ModelTypes).Assembly;
            var classes = asm.GetTypes().Where(p =>
                 p.Namespace == name
                 & p.CustomAttributes.Any(x => x.AttributeType == typeof(GeneratedEntityAttribute))

            );

            return classes
            .Select(x => x.GetTypeInfo())
            .ToArray();
        }
    }
}
