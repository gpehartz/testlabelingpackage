using System.Collections.Generic;
using ICETeam.TestPackage.Domain;
using ICETeam.TestPackage.Domain.Declarations;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace ICETeam.TestPackage.ParseLogic
{
    public interface IDefinitionParser
    {
        IEnumerable<NodeWithLabels> Parse(VisualStudioWorkspace vsWorkspace, BaseDefinition definition, Document document);
    }
}
