using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace ICETeam.TestPackage.Extensions
{
    internal static class TextSpanExtensions
    {
        internal static SnapshotSpan ToSnapshotSpan(this TextSpan textSpan, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, new Span(textSpan.Start, textSpan.Length));
        }
    }
}
