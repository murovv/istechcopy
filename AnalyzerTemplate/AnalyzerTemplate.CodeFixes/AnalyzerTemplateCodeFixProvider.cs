using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharpExtensions;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        private int _i;

        public int _counter
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _i; }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set { _i = value; }
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(AnalyzerTemplateAnalyzer.DiagnosticId,
                    AnalyzerTemplateAnalyzer.LambdaDiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();

            if (diagnostic.Id == FixableDiagnosticIds[0])
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitle,
                        createChangedSolution: c => ReplaceConvertAsync(context.Document, diagnostic, root),
                        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                    diagnostic);
            }
            else
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.LambdaCodeFixTitle,
                        createChangedSolution: c => WrapLambdaAsync(context.Document, diagnostic, root, c),
                        equivalenceKey: nameof(CodeFixResources.LambdaCodeFixTitle)),
                    diagnostic);
            }
        }

        private async Task<Solution> WrapLambdaAsync(Document document, Diagnostic diagnostic, SyntaxNode root,
            CancellationToken token)
        {
            foreach (var VARIABLE in root.FindNode(diagnostic.Location.SourceSpan).ChildNodes()
                .Append(root.FindNode(diagnostic.Location.SourceSpan)))
            {
                /*Console.WriteLine(VARIABLE.Kind());*/
            }

            LambdaExpressionSyntax statement = (LambdaExpressionSyntax) root.FindNode(diagnostic.Location.SourceSpan)
                .ChildNodes().Append(root.FindNode(diagnostic.Location.SourceSpan)).FirstOrDefault(t =>
                    t.RawKind == (int) SyntaxKind.SimpleLambdaExpression ||
                    t.RawKind == (int) SyntaxKind.ParenthesizedLambdaExpression);
            SemanticModel model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            document.TryGetSyntaxRoot(out var realRoot);
            //TODO: искать объявление как то по другому
            var declaration = realRoot.DescendantNodes()
                .FirstOrDefault(node => node.IsKind(SyntaxKind.ClassDeclaration)) ?? realRoot.DescendantNodes()
                .FirstOrDefault(node => node.IsKind(SyntaxKind.StructDeclaration));
            if (model.GetSymbolInfo(statement).Symbol is IMethodSymbol lambdaSymbol)
            {
                var editor = await DocumentEditor.CreateAsync(document, token);
                var methodToInsert = GetMethodDeclarationSyntax(
                    returnTypeName: lambdaSymbol.ReturnType.ToDisplayString(),
                    methodName: GetCurrentIdentifyerName(),
                    parameterTypes: lambdaSymbol.Parameters.Select(t => t.ToDisplayString()).ToArray(),
                    paramterNames: lambdaSymbol.Parameters.Select(t => t.Name).ToArray(),
                    expressionToReturn: statement.Body as ExpressionSyntax);

                //TODO: почему нельзя дважды replaceNode
                editor.AddMember(declaration, methodToInsert);
                editor.ReplaceNode(statement.Body,
                    GetInvocationExpressionSyntax(lambdaSymbol.Parameters.Select(t => t.Name).ToArray()));
                //TODO: что то с потоками
                _counter++;
                document = editor.GetChangedDocument();
            }

            return document.Project.Solution;
        }

        private string GetCurrentIdentifyerName()
        {
            return "Temp" + _counter.ToString();
        }

        private async Task<Solution> ReplaceConvertAsync(Document document, Diagnostic diagnostic, SyntaxNode root)
        {
            StatementSyntax statement =
                root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<StatementSyntax>();
            string methodName = statement.DescendantNodes()
                .FirstOrDefault(e => (Regex.IsMatch(e.ToString(), "To*") && e.RawKind == 8616)).ToString();
            var method = typeof(System.Convert).GetMethod(methodName, new[] {typeof(String)});
            var argumentList = (ArgumentListSyntax) statement.DescendantNodes()
                .FirstOrDefault(e => e.RawKind == (int) SyntaxKind.ArgumentList);
            var newRoot = root.ReplaceNode(statement, SyntaxFactory.ExpressionStatement
                (
                    SyntaxFactory.InvocationExpression
                        (
                            SyntaxFactory.MemberAccessExpression
                            (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(method.ReturnType.Name),
                                SyntaxFactory.IdentifierName("Parse")
                            )
                        )
                        .WithArgumentList
                        (
                            argumentList
                        )
                )
            );
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private MethodDeclarationSyntax GetMethodDeclarationSyntax(string returnTypeName, string methodName,
            string[] parameterTypes, string[] paramterNames, ExpressionSyntax expressionToReturn)
        {
            var parameterList =
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(GetParametersList(parameterTypes, paramterNames)));
            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(),
                    returnType: SyntaxFactory.ParseTypeName(returnTypeName),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier(methodName),
                    typeParameterList: null,
                    parameterList: parameterList,
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    body: null,
                    null)
                // Annotate that this node should be formatted
                .WithAdditionalAnnotations(Formatter.Annotation)
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        expressionToReturn
                    )
                ));
        }

        private InvocationExpressionSyntax GetInvocationExpressionSyntax(string[] argumentNames)
        {
            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(GetCurrentIdentifyerName())
                , SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(GetArgumentList(argumentNames))));
        }

        private IEnumerable<ParameterSyntax> GetParametersList(string[] parameterTypes, string[] paramterNames)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(),
                    type: SyntaxFactory.ParseTypeName(parameterTypes[i]),
                    identifier: SyntaxFactory.Identifier(paramterNames[i]),
                    @default: null);
            }
        }

        private IEnumerable<ArgumentSyntax> GetArgumentList(string[] argumentNames)
        {
            for (int i = 0; i < argumentNames.Length; i++)
            {
                yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argumentNames[i]));
            }
        }
    }
}