using System;
using ICETeam.TestPackage.Domain.Declarations;

namespace ICETeam.TestPackage.ParseLogic
{
    class ParserFactory
    {
        public IDefinitionParser GetDefinitionParserFor(BaseDefinition definition)
        {
            var variableDeclarationDefiniton = definition as VariableDeclarationDefinition;
            if (variableDeclarationDefiniton != null)
            {
                var variableDeclarationParseLogic = new VariableDeclarationParseLogic();
                return variableDeclarationParseLogic;
            }

            var methodDefinition = definition as MethodDefinition;
            if (methodDefinition != null)
            {
                var methodParseLogic = new MethodParseLogic();
                return methodParseLogic;
            }

            throw new ApplicationException("Invalid definition type!");
        }
    }
}
