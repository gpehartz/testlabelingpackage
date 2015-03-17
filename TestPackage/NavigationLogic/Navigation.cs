using System.Collections.Generic;
using System.Linq;
using ICETeam.TestPackage.Domain;
using ICETeam.TestPackage.Domain.Declarations;
using ICETeam.TestPackage.Domain.LabelDefinitions;
using Microsoft.CodeAnalysis;

namespace ICETeam.TestPackage.NavigationLogic
{
    public static class Navigation
    {
        public static bool IsNavigationAllowed(IEnumerable<NodeWithLabels> parsedData, DocumentId actualDocumentId, int position)
        {
            var connections = TestDefinitions.Connections;

            foreach (var connection in connections)
            {
                var variableMethodConnection = connection as VariableDeclarationByBaseTypeAndMethodConnectionDefinition;
                if(variableMethodConnection == null) continue;

                var labelsOnSource = CheckSourceByDefinition(actualDocumentId, parsedData, variableMethodConnection).ToList();
                var sourceResult = labelsOnSource.Any(item => item.Node.Span.Contains(position));

                var labelsOnTarget = CheckTargetByDefinitions(parsedData, labelsOnSource, variableMethodConnection);
                var targetTesult = labelsOnTarget.Any();
                
                if (sourceResult && targetTesult) return true;
            }

            return false;
        }

        public static IEnumerable<NodeWithLabels> GetNavigationTargets(IEnumerable<NodeWithLabels> parsedData, DocumentId actualDocumentId, int position)
        {
            var connections = TestDefinitions.Connections;

            var result = new List<NodeWithLabels>();

            foreach (var connection in connections)
            {
                var variableMethodConnection = connection as VariableDeclarationByBaseTypeAndMethodConnectionDefinition;
                if (variableMethodConnection == null) continue;

                var labelsOnSource = CheckSourceByDefinition(actualDocumentId, parsedData, variableMethodConnection).Where(item => item.Node.Span.Contains(position)).ToList();
                var sourceResult = labelsOnSource.Any();
                if(!sourceResult) continue;

                var labelsOnTarget = CheckTargetByDefinitions(parsedData, labelsOnSource, variableMethodConnection);

                result.AddRange(labelsOnTarget);
            }

            return result;
        }

        private static IEnumerable<NodeWithLabels> CheckTargetByDefinitions(IEnumerable<NodeWithLabels> parsedData, IEnumerable<NodeWithLabels> sourceResult,
            VariableDeclarationByBaseTypeAndMethodConnectionDefinition variableMethodConnection)
        {
            var result = new List<NodeWithLabels>();

            foreach (var nodeWithLabels in sourceResult)
            {
                var foundItems = parsedData.Where(item => item.AttachedLabels.OfType<MethodNameLabel>().Any(i => i.Name == variableMethodConnection.MethodName))
                    .Where(
                        item => item.AttachedLabels.OfType<ParameterLabel>()
                            .Any(i => i.NameSpace == nodeWithLabels.GetVariableNameSpace() && i.TypeName == nodeWithLabels.GetVariableType()))
                    .Where(
                        item =>
                            item.AttachedLabels.OfType<ParameterBaseClassLabel>()
                                .Any(i => i.NameSpace == nodeWithLabels.GetVariableBaseNameSpace() && i.TypeName == nodeWithLabels.GetVariableBaseType()));

                result.AddRange(foundItems);
            }

            return result;
        }

        private static IEnumerable<NodeWithLabels> CheckSourceByDefinition(DocumentId documentId, IEnumerable<NodeWithLabels> parsedData,
            VariableDeclarationByBaseTypeAndMethodConnectionDefinition variableMethodConnection)
        {
            return parsedData.Where(item => item.ContainingDocumentId == documentId)
                .Where(item => item.AttachedLabels.OfType<VariableBaseTypeLabel>().Any(i => i.NameSpace == variableMethodConnection.Type.NameSpace))
                .Where(item => item.AttachedLabels.OfType<VariableBaseTypeLabel>().Any(i => i.TypeName == variableMethodConnection.Type.Type)).ToList();
        }
    }
}
