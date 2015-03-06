using System.Collections.Generic;
using System.Linq;
using ICETeam.TestPackage.Domain;
using ICETeam.TestPackage.Domain.Declarations;
using ICETeam.TestPackage.Domain.LabelDefinitions;
using ICETeam.TestPackage.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;

namespace ICETeam.TestPackage.ParseLogic
{
    public class MethodParseLogic : IDefinitionParser
    {

        public IEnumerable<NodeWithLabels> Parse(VisualStudioWorkspace vsWorkspace, BaseDefinition definition, Document document)
        {
            var methodDefinition = (MethodDefinition)definition;

            var result = ProcessMethodDefinitionForDocument(methodDefinition, document);
            return result;
        }

        private static IEnumerable<NodeWithLabels> ProcessMethodDefinitionForDocument(MethodDefinition methodDefinition, Document document)
        {
            var syntaxRoot = document.GetSyntaxRootAsync().Result;
            var semanticModel = document.GetSemanticModelAsync().Result;

            var result = new List<NodeWithLabels>();

            result.AddRange(ParseMethods(methodDefinition, document, syntaxRoot, semanticModel));

            //For change management the classes and baseclasses must be parsed too
            result.AddRange(ParseClassDeclarations(methodDefinition, document, syntaxRoot, semanticModel));

            return result;
        }

        private static IEnumerable<NodeWithLabels> ParseClassDeclarations(MethodDefinition methodDefinition, Document document, SyntaxNode syntaxRoot, SemanticModel semanticModel)
        {
            var result = new List<NodeWithLabels>();

            var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                var declaredSymbol = (ITypeSymbol) semanticModel.GetDeclaredSymbol(classDeclaration);
                if(declaredSymbol == null) continue;

                foreach (var parameterType in methodDefinition.ParameterTypes)
                {
                    if (declaredSymbol.IsType(parameterType))
                    {
                        var node = BuildNode(document, classDeclaration, parameterType);
                        result.Add(node);
                    }
                }

                foreach (var parameterType in methodDefinition.ParameterBaseTypes)
                {
                    if (declaredSymbol.IsSubType(parameterType))
                    {
                        var node = BuildNode(document, classDeclaration, parameterType);
                        result.Add(node);
                    }
                }
            }

            return result;
        }

        private static NodeWithLabels BuildNode(Document document, SyntaxNode classDeclaration, TypeDefinition parameterType)
        {
            var node = new NodeWithLabels
            {
                Node = classDeclaration,
                ContainingDocumentId = document.Id,
                AttachedLabels = new List<BaseLabel>
                {
                    new NameSpaceLabel {NameSpace = parameterType.NameSpace},
                    new ClassLabel {TypeName = parameterType.Type}
                }
            };
            return node;
        }

        private static IEnumerable<NodeWithLabels> ParseMethods(MethodDefinition methodDefinition, Document document, SyntaxNode syntaxRoot, SemanticModel semanticModel)
        {
            var result = new List<NodeWithLabels>();

            var methodDeclarations = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in methodDeclarations)
            {
                var methodSymbol = (IMethodSymbol) semanticModel.GetDeclaredSymbol(methodDeclaration);

                if (methodSymbol.Name != methodDefinition.Name) continue;
                if (IsNameSpaceInvalid(methodDefinition, methodSymbol)) continue;
                if (AreParameterTypesInvalid(methodDefinition, methodSymbol)) continue;
                if (AreParameterBaseTypesInvalid(methodDefinition, methodSymbol)) continue;

                var node = BuildParsedItem(methodDeclaration, methodDefinition, document);
                result.Add(node);
            }

            return result;
        }

        private static NodeWithLabels BuildParsedItem(SyntaxNode methodDeclaration, MethodDefinition methodDefinition, Document document)
        {
            var parsedItem = new NodeWithLabels
            {
                Node = methodDeclaration,
                ContainingDocumentId = document.Id,
                AttachedLabels = new List<BaseLabel>
                    {
                        new MethodNameLabel {Name = methodDefinition.Name}
                    }
            };

            if (!string.IsNullOrEmpty(methodDefinition.NameSpace))
            {
                parsedItem.AttachedLabels.Add(new NameSpaceLabel { NameSpace = methodDefinition.NameSpace });
            }

            parsedItem.AttachedLabels.AddRange(methodDefinition.Tags.Select(item => new TagLabel { Name = item }));
            parsedItem.AttachedLabels.AddRange(methodDefinition.ParameterTypes.Select(item => new ParameterLabel { TypeName = item.Type }));
            parsedItem.AttachedLabels.AddRange(methodDefinition.ParameterBaseTypes.Select(item => new ParameterBaseClassLabel { TypeName = item.Type }));

            return parsedItem;
        }

        private static bool AreParameterBaseTypesInvalid(MethodDefinition methodDefinition, IMethodSymbol methodSymbol)
        {
            if (!methodDefinition.ParameterBaseTypes.Any()) return false;

            var areParameterBaseTypesMatching = true;
            foreach (var parameterSymbol in methodSymbol.Parameters)
            {
                if (methodDefinition.ParameterBaseTypes.Any(item => parameterSymbol.Type.IsSubTypeOf(item))) continue;

                areParameterBaseTypesMatching = false;
                break;
            }

            return !areParameterBaseTypesMatching;
        }

        private static bool AreParameterTypesInvalid(MethodDefinition methodDefinition, IMethodSymbol methodSymbol)
        {
            if (!methodDefinition.ParameterTypes.Any()) return false;

            var areParametersMatching = true;
            foreach (var parameterSymbol in methodSymbol.Parameters)
            {
                if (methodDefinition.ParameterTypes.Any(item => parameterSymbol.Type.Name == item.Type && parameterSymbol.ContainingNamespace.Name == item.NameSpace)) continue;

                areParametersMatching = false;
                break;
            }

            return !areParametersMatching;
        }

        private static bool IsNameSpaceInvalid(MethodDefinition methodDefinition, IMethodSymbol methodSymbol)
        {
            if (string.IsNullOrEmpty(methodDefinition.NameSpace)) return false;

            if (methodSymbol.ContainingNamespace.Name != methodDefinition.NameSpace) return true;
            return false;
        }
    }
}
