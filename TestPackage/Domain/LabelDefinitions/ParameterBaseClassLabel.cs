using System.Collections.Generic;

namespace ICETeam.TestPackage.Domain.LabelDefinitions
{
    public class ParameterBaseClassLabel : BaseLabel
    {
        public string NameSpace { get; set; }

        public string TypeName { get; set; }

        private sealed class NameSpaceTypeNameEqualityComparer : IEqualityComparer<ParameterBaseClassLabel>
        {
            public bool Equals(ParameterBaseClassLabel x, ParameterBaseClassLabel y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.NameSpace, y.NameSpace) && string.Equals(x.TypeName, y.TypeName);
            }

            public int GetHashCode(ParameterBaseClassLabel obj)
            {
                unchecked
                {
                    return ((obj.NameSpace != null ? obj.NameSpace.GetHashCode() : 0)*397) ^ (obj.TypeName != null ? obj.TypeName.GetHashCode() : 0);
                }
            }
        }

        private static readonly IEqualityComparer<ParameterBaseClassLabel> NameSpaceTypeNameComparerInstance = new NameSpaceTypeNameEqualityComparer();

        public static IEqualityComparer<ParameterBaseClassLabel> NameSpaceTypeNameComparer
        {
            get { return NameSpaceTypeNameComparerInstance; }
        }
    }
}
