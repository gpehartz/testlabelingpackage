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
    public class VariableDeclarationParseLogic : IDefinitionParser
    {
        public IEnumerable<NodeWithLabels> Parse(VisualStudioWorkspace vsWorkspace, BaseDefinition definition, Document document)
        {
            var variableDeclarationDefinition = (VariableDeclarationDefinition) definition;
 
            var result = ProcessVariableDeclarationDefinitionForDocument(variableDeclarationDefinition, document);
            return result;
        }

        private static IEnumerable<NodeWithLabels> ProcessVariableDeclarationDefinitionForDocument(VariableDeclarationDefinition variableDeclarationDefinition, Document document)
        {
            var syntaxRoot = document.GetSyntaxRootAsync().Result;
            var semanticModel = document.GetSemanticModelAsync().Result;

            var result = new List<NodeWithLabels>();

            result.AddRange(ParseVariableDeclarations(variableDeclarationDefinition, document, syntaxRoot, semanticModel));

            //For change management the classes and baseclasses must be parsed too
            result.AddRange(ParseClassDeclarations(variableDeclarationDefinition, document, syntaxRoot, semanticModel));

            return result;
        }

        private static IEnumerable<NodeWithLabels> ParseClassDeclarations(VariableDeclarationDefinition variableDeclarationDefinition, Document document, SyntaxNode syntaxRoot,
            SemanticModel semanticModel)
        {
            var result = new List<NodeWithLabels>();

            var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                var declaredSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration);

                if (variableDeclarationDefinition.BaseType != null && declaredSymbol.IsSubType(variableDeclarationDefinition.BaseType))
                {
                    var node = BuildNode(variableDeclarationDefinition, document, classDeclaration);
                    result.Add(node);
                }
                if (variableDeclarationDefinition.Type != null && declaredSymbol.IsType(variableDeclarationDefinition.Type))
                {
                    var node = BuildNode(variableDeclarationDefinition, document, classDeclaration);
                    result.Add(node);
                }
            }

            return result;
        }

        private static NodeWithLabels BuildNode(VariableDeclarationDefinition variableDeclarationDefinition, Document document, SyntaxNode classDeclaration)
        {
            var node = new NodeWithLabels
            {
                Node = classDeclaration,
                ContainingDocumentId = document.Id,
                AttachedLabels = new List<BaseLabel>
                {
                    new NameSpaceLabel {NameSpace = variableDeclarationDefinition.BaseType.NameSpace},
                    new BaseClassLabel {TypeName = variableDeclarationDefinition.BaseType.Type}
                }
            };
            return node;
        }

        private static IEnumerable<NodeWithLabels> ParseVariableDeclarations(VariableDeclarationDefinition variableDeclarationDefinition, Document document, SyntaxNode syntaxRoot,
            SemanticModel semanticModel)
        {
            var result = new List<NodeWithLabels>();

            var variableDeclarations = syntaxRoot.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variableDeclaration in variableDeclarations)
            {
                var typeSymbol = ((ILocalSymbol) semanticModel.GetDeclaredSymbol(variableDeclaration)).Type;

                if (CheckNamespace(typeSymbol, variableDeclarationDefinition.NameSpace)) continue;
                if (variableDeclarationDefinition.BaseType != null && !typeSymbol.IsSubType(variableDeclarationDefinition.BaseType)) continue;
                if (variableDeclarationDefinition.Type != null && !typeSymbol.IsType(variableDeclarationDefinition.Type)) continue;

                var node = BuildParsedItem(variableDeclaration, variableDeclarationDefinition, document);
                result.Add(node);
            }

            return result;
        }

        private static NodeWithLabels BuildParsedItem(SyntaxNode syntaxNode, VariableDeclarationDefinition variableDeclarationDefinition, Document document)
        {
            var parsedItem = new NodeWithLabels
            {
                Node = syntaxNode,
                ContainingDocumentId = document.Id,
                AttachedLabels = new List<BaseLabel>()
            };

            if (variableDeclarationDefinition.Type != null)
            {
                parsedItem.AttachedLabels.Add(new VariableTypeLabel { TypeName = variableDeclarationDefinition.Type.Type });
                parsedItem.AttachedLabels.Add(new NameSpaceLabel { NameSpace = variableDeclarationDefinition.Type.NameSpace });
            }

            if (variableDeclarationDefinition.BaseType != null)
            {
                parsedItem.AttachedLabels.Add(new VariableBaseTypeLabel { TypeName = variableDeclarationDefinition.BaseType.Type });
            }

            if (variableDeclarationDefinition.Tags != null)
            {
                parsedItem.AttachedLabels.AddRange(variableDeclarationDefinition.Tags.Select(item => new TagLabel { Name = item }));
            }

            return parsedItem;
        }

        public static bool CheckNamespace(ITypeSymbol typeSymbol, string nameSpace)
        {
            if (!string.IsNullOrEmpty(nameSpace))
            {
                if (typeSymbol.ContainingNamespace.Name != nameSpace) return true;
            }
            return false;
        }
    }
    
}
