using System;
using System.Collections.Generic;
using ICETeam.TestPackage.Domain.Declarations;
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

            if (t.BaseType == null) return false;
            return IsSubTypeOf(t.BaseType, subType);
        }

        public static IEnumerable<TypeNameWithNameSpace> GetSubTypes(this ITypeSymbol t)
        {
            var result = new List<TypeNameWithNameSpace>();

            if (t.BaseType == null) return result;

            var baseType = t.BaseType;
            while (GetFullName(baseType) != typeof(object).FullName)
            {
                result.Add(new TypeNameWithNameSpace { NameSpace = baseType.ContainingNamespace.Name, TypeName = baseType.Name });
                baseType = baseType.BaseType;

                if(baseType == null) break;
            }

            return result;
        }

        private static string GetFullName(ISymbol t)
        {
            return t.ContainingNamespace.Name + "." + t.Name;
        }

        public class TypeNameWithNameSpace
        {
            public string NameSpace { get; set; }

            public string TypeName { get; set; }
        }
    }
}
