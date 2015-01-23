using System.Collections.Generic;

namespace ICETeam.TestPackage.Declarations
{
    public class VariableDeclarationDefinition : BaseDefinition
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public TypeDefinition Type { get; set; }
        public TypeDefinition BaseType { get; set; }
        public List<string> Tags { get; set; }
    }
}
