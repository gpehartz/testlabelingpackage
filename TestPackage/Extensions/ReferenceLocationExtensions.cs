using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICETeam.TestPackage.Extensions
{
    public static class ReferenceLocationExtensions
    {
        public static SyntaxNode GetNode(this ReferenceLocation location)
        {
            return location.Document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(location.Location.SourceSpan.Start).Parent;
        }
    }
}
