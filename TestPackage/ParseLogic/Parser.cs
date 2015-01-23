using System.Collections.Generic;
using System.Linq;
using System.Web.UI.Design.WebControls;
using ICETeam.TestPackage.Declarations;
using ICETeam.TestPackage.LabelDefinitions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace ICETeam.TestPackage.ParseLogic
{
    public class Parser
    {
        private List<NodeWithLabels> _nodesWithLabels = new List<NodeWithLabels>();

        public IEnumerable<NodeWithLabels> ParsedData => _nodesWithLabels;

        public void ParseWorkSpace(VisualStudioWorkspace vsWorkspace)
        {
            var definitions = TestDefinitions.Definitions;

            var documentsToCheck = vsWorkspace.CurrentSolution.Projects.SelectMany(item => item.Documents).Where(item => item.SourceCodeKind == SourceCodeKind.Regular);
            var parserFactory = new ParserFactory();

            foreach (var document in documentsToCheck.Where(item => item.SupportsSyntaxTree))
            {
                foreach (var definition in definitions)
                {
                    var parser = parserFactory.GetDefinitionParserFor(definition);
                    _nodesWithLabels.AddRange(parser.Parse(vsWorkspace, definition, document));
                }
            }

            _nodesWithLabels = _nodesWithLabels.Distinct(NodeWithLabels.NodeContainingDocumentIdComparer).ToList();
        }

        public void ReparseItemsForDocument(VisualStudioWorkspace vsWorkspace, Document changedDocument)
        {
            if (ParsedData == null) return;

            var labelsBefore = GetLabelsForClasses();
            RemoveParsedItemsForDocument(changedDocument);
            
            var definitions = TestDefinitions.Definitions;
            var parserFactory = new ParserFactory();

            foreach (var definition in definitions)
            {
                var parser = parserFactory.GetDefinitionParserFor(definition);
                _nodesWithLabels.AddRange(parser.Parse(vsWorkspace, definition, changedDocument));
            }

            var labelsAfter = GetLabelsForClasses();
            var changes = GetChanges(labelsBefore, labelsAfter);
            if (changes.Any())
            {
                ReparseRelatedDocuments(vsWorkspace, changes);
            }
        }

        private void ReparseRelatedDocuments(VisualStudioWorkspace vsWorkspace, List<NodeWithLabels> changes)
        {
            var reparsedDocuments = new List<DocumentId>();

            var definitions = TestDefinitions.Definitions;
            var parserFactory = new ParserFactory();

            foreach (var nodeWithLabels in changes)
            {
                if(reparsedDocuments.Contains(nodeWithLabels.ContainingDocumentId)) continue;

                var document = vsWorkspace.CurrentSolution.GetDocument(nodeWithLabels.ContainingDocumentId);

                RemoveParsedItemsForDocument(document);

                foreach (var definition in definitions)
                {
                    var parser = parserFactory.GetDefinitionParserFor(definition);
                    _nodesWithLabels.AddRange(parser.Parse(vsWorkspace, definition, document));
                }

                reparsedDocuments.Add(document.Id);
            }
        }

        private List<NodeWithLabels> GetChanges(List<NodeWithLabels> labelsBefore, List<NodeWithLabels> labelsAfter)
        {
            
        }

        private List<NodeWithLabels> GetLabelsForClasses()
        {
            return ParsedData.Where(item => item.AttachedLabels.Any(item2 => item2 is BaseClassLabel || item2 is ClassLabel)).ToList();
        }

        private void RemoveParsedItemsForDocument(Document changedDocument)
        {
            var itemsToRemove = ParsedData.Where(item => item.ContainingDocumentId == changedDocument.Id).ToList();

            foreach (var item in itemsToRemove)
            {
                _nodesWithLabels.Remove(item);
            }
        }
    }

    public class NodeWithLabels
    {
        public SyntaxNode Node { get; set; }

        public List<BaseLabel> AttachedLabels { get; set; }

        public DocumentId ContainingDocumentId { get; set; }

        private sealed class NodeContainingDocumentIdEqualityComparer : IEqualityComparer<NodeWithLabels>
        {
            public bool Equals(NodeWithLabels x, NodeWithLabels y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return Equals(x.Node, y.Node) && Equals(x.ContainingDocumentId, y.ContainingDocumentId);
            }

            public int GetHashCode(NodeWithLabels obj)
            {
                unchecked
                {
                    return ((obj.Node != null ? obj.Node.GetHashCode() : 0)*397) ^ (obj.ContainingDocumentId != null ? obj.ContainingDocumentId.GetHashCode() : 0);
                }
            }
        }

        private static readonly IEqualityComparer<NodeWithLabels> NodeContainingDocumentIdComparerInstance = new NodeContainingDocumentIdEqualityComparer();

        public static IEqualityComparer<NodeWithLabels> NodeContainingDocumentIdComparer
        {
            get { return NodeContainingDocumentIdComparerInstance; }
        }
    }
}
