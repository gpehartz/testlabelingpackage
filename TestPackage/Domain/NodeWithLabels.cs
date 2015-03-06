using System.Collections.Generic;
using ICETeam.TestPackage.Domain.LabelDefinitions;
using Microsoft.CodeAnalysis;

namespace ICETeam.TestPackage.Domain
{

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
                    return ((obj.Node != null ? obj.Node.GetHashCode() : 0) * 397) ^ (obj.ContainingDocumentId != null ? obj.ContainingDocumentId.GetHashCode() : 0);
                }
            }
        }

        public static IEqualityComparer<NodeWithLabels> NodeContainingDocumentIdComparer { get; } = new NodeContainingDocumentIdEqualityComparer();
    }
}
