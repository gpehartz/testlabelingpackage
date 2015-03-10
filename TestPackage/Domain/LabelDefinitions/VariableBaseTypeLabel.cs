using System.Collections.Generic;

namespace ICETeam.TestPackage.Domain.LabelDefinitions
{
    public class VariableBaseTypeLabel : BaseLabel
    {
        public string NameSpace { get; set; }

        public string TypeName { get; set; }

        private sealed class TypeNameNameSpaceEqualityComparer : IEqualityComparer<VariableBaseTypeLabel>
        {
            public bool Equals(VariableBaseTypeLabel x, VariableBaseTypeLabel y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.TypeName, y.TypeName) && string.Equals(x.NameSpace, y.NameSpace);
            }

            public int GetHashCode(VariableBaseTypeLabel obj)
            {
                unchecked
                {
                    return ((obj.TypeName != null ? obj.TypeName.GetHashCode() : 0)*397) ^ (obj.NameSpace != null ? obj.NameSpace.GetHashCode() : 0);
                }
            }
        }

        private static readonly IEqualityComparer<VariableBaseTypeLabel> TypeNameNameSpaceComparerInstance = new TypeNameNameSpaceEqualityComparer();

        public static IEqualityComparer<VariableBaseTypeLabel> TypeNameNameSpaceComparer
        {
            get { return TypeNameNameSpaceComparerInstance; }
        }
    }
}
