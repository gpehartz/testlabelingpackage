using System.Collections.Generic;
using ICETeam.TestPackage.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace ICETeam.TestPackage.ParseLogic
{
    public interface IDefinitionParser
    {
        IEnumerable<NodeWithLabels> Parse(VisualStudioWorkspace vsWorkspace, BaseDefinition definition, Document document);
    }
}
