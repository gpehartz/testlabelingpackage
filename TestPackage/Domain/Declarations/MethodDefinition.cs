using System.Collections.Generic;

namespace ICETeam.TestPackage.Domain.Declarations
{
    public class MethodDefinition : BaseDefinition
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public string IncludingClassInheritedFrom { get; set; }
        public List<string> Tags { get; set; }
        public List<TypeDefinition> ParameterTypes { get; set; }
        public List<TypeDefinition> ParameterBaseTypes { get; set; }
    }

    public class TypeDefinition
    {
        public string NameSpace { get; set; }
        public string Type { get; set; }

        public string GetFullName => NameSpace + "." + Type;
    }
}