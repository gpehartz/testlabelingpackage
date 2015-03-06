using System.Collections.Generic;

namespace ICETeam.TestPackage.Domain.Declarations
{
    public static class TestDefinitions
    {
        public static List<BaseDefinition> Definitions { get; set; }

        public static MethodDefinition ApplyEventMethodDefinition = new MethodDefinition
        {
            Name = "ApplyEvent",
            NameSpace = "",
            IncludingClassInheritedFrom = "",
            ParameterTypes = new List<TypeDefinition>(),
            ParameterBaseTypes = new List<TypeDefinition>
            {
                new TypeDefinition
                {
                    NameSpace = "Domain",
                    Type = "BaseEvent"
                }
            },
            Tags = new List<string>
            {
                "Test"
            }
        };

        public static VariableDeclarationDefinition EventDeclarationDefinition = new VariableDeclarationDefinition
        {
            BaseType = new TypeDefinition
            {
                Type = "BaseEvent",
                NameSpace = "Domain"
            }
        };

        static TestDefinitions()
        {
            Definitions = new List<BaseDefinition>
            {
                ApplyEventMethodDefinition,
                EventDeclarationDefinition
            };
        }
    }
}
