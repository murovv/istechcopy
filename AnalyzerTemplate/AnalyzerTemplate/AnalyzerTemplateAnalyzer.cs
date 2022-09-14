using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerTemplate
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerTemplateAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AnalyzerTemplate";
        public const string LambdaDiagnosticId = "LambdaAnalyzerTemplate";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle),
            Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString LambdaTitle =
            new LocalizableResourceString(nameof(Resources.LambdaAnalyzerTitle), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString LambdaMessageFormat =
            new LocalizableResourceString(nameof(Resources.LambdaAnalyzerMessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString LambdaDescription =
            new LocalizableResourceString(nameof(Resources.LambdaAnalyzerDescription), Resources.ResourceManager,
                typeof(Resources));

        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly DiagnosticDescriptor LambdaRule = new DiagnosticDescriptor(LambdaDiagnosticId,
            LambdaTitle, LambdaMessageFormat, "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: LambdaDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule, LambdaRule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSimpleLambdaExpression, SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var children = context.Node.ChildNodes().ToList();
            if (children.Any(c => c.ToString() == "Convert") && children.Any(c => Regex.IsMatch(c.ToString(), "To*")))
            {
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), context.Node);
                context.ReportDiagnostic(diagnostic);
            }

            ;
        }

        private static void AnalyzeSimpleLambdaExpression(SyntaxNodeAnalysisContext context)
        {
            LambdaExpressionSyntax lambda = (LambdaExpressionSyntax) context.Node;
            if (!(lambda.Body.RawKind == (int) SyntaxKind.Block ||
                  lambda.Body.RawKind == (int) SyntaxKind.InvocationExpression))
            {
                var diagnostic = Diagnostic.Create(LambdaRule, lambda.GetLocation(), lambda);
                context.ReportDiagnostic(diagnostic);
            }
        }
        /*
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }*/
    }
}