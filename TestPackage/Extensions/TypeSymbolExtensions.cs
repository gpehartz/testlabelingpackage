using ICETeam.TestPackage.Declarations;
using Microsoft.CodeAnalysis;

namespace ICETeam.TestPackage.Extensions
{
    public static class TypeSymbolExtensions
    {
        public static bool IsType(this ITypeSymbol typeSymbol, TypeDefinition typeToCheck)
        {
            if (typeToCheck == null) return false;
            return typeSymbol.Name == typeToCheck.Type && typeSymbol.ContainingNamespace.Name == typeToCheck.NameSpace;
        }

        public static bool IsSubType(this ITypeSymbol typeSymbol, TypeDefinition typeToCheck)
        {
            if (typeToCheck == null) return false;
            return typeSymbol.IsSubTypeOf(typeToCheck);
        }

        public static bool IsSubTypeOf(this ITypeSymbol t, TypeDefinition subType)
        {
            if (GetFullName(t) == subType.GetFullName) return true;
            if (GetFullName(t) == typeof(object).FullName) return false;
            return IsSubTypeOf(t.BaseType, subType);
        }

        private static string GetFullName(ISymbol t)
        {
            return t.ContainingNamespace.Name + "." + t.Name;
        }
    }
}
