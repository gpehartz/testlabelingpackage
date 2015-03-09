using System.Collections.Generic;
using System.Linq;

namespace ICETeam.TestPackage.Extensions
{
    public static class ListExtensions
    {
        public static void AddIfNotExists<T>(this List<T> source, T itemToAdd, IEqualityComparer<T> comparer)
        {
            if(source.Contains(itemToAdd, comparer)) return;         
            source.Add(itemToAdd);
        }

        public static void AddIfNotExists<T, TK>(this List<T> source, TK itemToAdd, IEqualityComparer<TK> comparer) where TK : T
        {
            var itemsWithTypeK = source.OfType<TK>();

            if(itemsWithTypeK.Contains(itemToAdd, comparer)) return;
            source.Add(itemToAdd);
        }
    }
}
