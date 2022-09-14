using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lab2.services
{
    public class ClientBuilder
    {
        private IEnumerable<Endpoint> _endpoints;
        private IEnumerable<PhpClass> _classes;
        private string _clientName;
        public ClientBuilder(IEnumerable<Endpoint> endpoints, IEnumerable<PhpClass> classes, string clientName)
        {
            _endpoints = endpoints;
            _classes = classes;
            _clientName = clientName;
        }

        public CompilationUnitSyntax Build()
        {
            return CompilationUnit()
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                                IdentifierName("AwesomeNamespace"))
                            .WithMembers(
                                List<MemberDeclarationSyntax>(GetClasses()))
                            .WithCloseBraceToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.CloseBraceToken,
                                    TriviaList(
                                        Trivia(
                                            SkippedTokensTrivia()
                                                .WithTokens(
                                                    TokenList(
                                                        Token(SyntaxKind.CloseBraceToken)))))))))
                .NormalizeWhitespace();
        }

        private ClassDeclarationSyntax[] GetClasses()
        {
            return _classes.Select(GetDtoDeclaration).Append(GetMainClass()).ToArray();
        }

        private ClassDeclarationSyntax GetMainClass()
        {
            return ClassDeclaration(_clientName)
                .WithModifiers(
                    TokenList(
                        new[]{Token(SyntaxKind.PublicKeyword),Token(SyntaxKind.PartialKeyword)}))
                .WithMembers(
                    List<MemberDeclarationSyntax>(GetEndpoints().Cast<MemberDeclarationSyntax>()));
        }
        private StatementSyntax[] GetEndpoints()
        {
            return _endpoints.Select(BuildEndpoint).ToArray();
        }
        public ClassDeclarationSyntax GetDtoDeclaration(PhpClass dto)
        {
            return ClassDeclaration(dto.Name)
        .WithModifiers(
            TokenList(
                Token(SyntaxKind.PublicKeyword)))
        .WithMembers(
            List<MemberDeclarationSyntax>(GetPropertyList(dto)));
        }

        private MemberDeclarationSyntax[] GetPropertyList(PhpClass dto)
        {
            List<MemberDeclarationSyntax> props = new List<MemberDeclarationSyntax>();
            foreach (var prop in dto.Fields)
            {
                props.Add(PropertyDeclaration(
                        PredefinedType(Identifier(prop.Type)),
                        Identifier(prop.Name))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(
                        AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(
                                            Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(
                                            Token(SyntaxKind.SemicolonToken))
                                }))));
            }

            return props.ToArray();
        }

        private string BuildQuery(Endpoint endpoint)
        {
            string query = $"{endpoint.Path}?";
            if (endpoint.Arguments.Count == 0)
            {
                return $"{endpoint.Path}";
            }

            for (int i = 0; i < endpoint.Arguments.Count; i++)
            {
                var arg = endpoint.Arguments[i];
                query += $"{arg.Name}={{{i}}}";
            }

            return query;
        }

        private StatementSyntax BuildEndpoint(Endpoint endpoint)
        {
            BlockSyntax body;
            switch (endpoint.Method)
            {
                case HttpMethod.GET:
                    body = GetBody(endpoint);
                    break;
                case HttpMethod.POST:
                    body = PostBody(endpoint);
                    break;
                default:
                    throw new Exception("Неожиданный метод");
            }
            return LocalFunctionStatement(
                    IdentifierName(endpoint.ReturnType),
                    //FunctionName
                    Identifier(endpoint.NameWithoutExtension))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(SeparatedList<ParameterSyntax>
                        (
                        GetParameterList(endpoint))))
                .WithBody(body)
                .NormalizeWhitespace();
        }

        public ClassDeclarationSyntax ToStringClass()
        {
            return ClassDeclaration("ExtensionMethodsClass")
                .WithModifiers(
                    TokenList(
                        new[]
                        {
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword)
                        }))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        MethodDeclaration(
                                PredefinedType(
                                    Token(SyntaxKind.StringKeyword)),
                                Identifier("MyToString"))
                            .WithModifiers(
                                TokenList(
                                    new[]
                                    {
                                        Token(SyntaxKind.PublicKeyword),
                                        Token(SyntaxKind.StaticKeyword)
                                    }))
                            .WithTypeParameterList(
                                TypeParameterList(
                                    SingletonSeparatedList<TypeParameterSyntax>(
                                        TypeParameter(
                                            Identifier("T")))))
                            .WithParameterList(
                                ParameterList(
                                    SingletonSeparatedList<ParameterSyntax>(
                                        Parameter(
                                                Identifier("entity"))
                                            .WithModifiers(
                                                TokenList(
                                                    Token(SyntaxKind.ThisKeyword)))
                                            .WithType(
                                                IdentifierName("T")))))
                            .WithBody(
                                Block(
                                    IfStatement(
                                        BinaryExpression(
                                            SyntaxKind.LogicalAndExpression,
                                            PrefixUnaryExpression(
                                                SyntaxKind.LogicalNotExpression,
                                                InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            TypeOfExpression(
                                                                IdentifierName("T")),
                                                            IdentifierName("IsAssignableTo")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SingletonSeparatedList<ArgumentSyntax>(
                                                                Argument(
                                                                    TypeOfExpression(
                                                                        PredefinedType(
                                                                            Token(SyntaxKind.StringKeyword)))))))),
                                            InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        TypeOfExpression(
                                                            IdentifierName("T")),
                                                        IdentifierName("IsAssignableTo")))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList<ArgumentSyntax>(
                                                            Argument(
                                                                TypeOfExpression(
                                                                    IdentifierName("IEnumerable"))))))),
                                        Block(
                                            LocalDeclarationStatement(
                                                VariableDeclaration(
                                                        PredefinedType(
                                                            Token(SyntaxKind.StringKeyword)))
                                                    .WithVariables(
                                                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                            VariableDeclarator(
                                                                    Identifier("ans"))
                                                                .WithInitializer(
                                                                    EqualsValueClause(
                                                                        LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            Literal(""))))))),
                                            ForEachStatement(
                                                IdentifierName(
                                                    Identifier(
                                                        TriviaList(),
                                                        SyntaxKind.VarKeyword,
                                                        "var",
                                                        "var",
                                                        TriviaList())),
                                                Identifier("t"),
                                                CastExpression(
                                                    IdentifierName("IEnumerable"),
                                                    IdentifierName("entity")),
                                                Block(
                                                    ExpressionStatement(
                                                        AssignmentExpression(
                                                            SyntaxKind.AddAssignmentExpression,
                                                            IdentifierName("ans"),
                                                            InvocationExpression(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName("t"),
                                                                    IdentifierName("ToString"))))),
                                                    ExpressionStatement(
                                                        AssignmentExpression(
                                                            SyntaxKind.AddAssignmentExpression,
                                                            IdentifierName("ans"),
                                                            LiteralExpression(
                                                                SyntaxKind.StringLiteralExpression,
                                                                Literal(",")))))),
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    IdentifierName("ans"),
                                                    InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("ans"),
                                                                IdentifierName("Remove")))
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SeparatedList<ArgumentSyntax>(
                                                                    new SyntaxNodeOrToken[]
                                                                    {
                                                                        Argument(
                                                                            BinaryExpression(
                                                                                SyntaxKind.SubtractExpression,
                                                                                MemberAccessExpression(
                                                                                    SyntaxKind
                                                                                        .SimpleMemberAccessExpression,
                                                                                    IdentifierName("ans"),
                                                                                    IdentifierName("Length")),
                                                                                LiteralExpression(
                                                                                    SyntaxKind.NumericLiteralExpression,
                                                                                    Literal(1)))),
                                                                        Token(SyntaxKind.CommaToken),
                                                                        Argument(
                                                                            LiteralExpression(
                                                                                SyntaxKind.NumericLiteralExpression,
                                                                                Literal(1)))
                                                                    }))))),
                                            ReturnStatement(
                                                IdentifierName("ans")))),
                                    ReturnStatement(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("entity"),
                                                IdentifierName("ToString"))))))));
        }
        private SyntaxNodeOrToken[] GetParameterList(Endpoint endpoint)
        {
            var paramsList = new List<SyntaxNodeOrToken>();
            foreach (var param in endpoint.Arguments)
            {

                paramsList.Add(Parameter(
                        Identifier(param.Name))
                    .WithType(
                        IdentifierName(param.Type)));
                paramsList.Add(Token(SyntaxKind.CommaToken));
            }
            if(paramsList.Count!=0)
                paramsList.RemoveAt(paramsList.Count-1);
            return paramsList.ToArray();
        }

        private BlockSyntax GetBody(Endpoint endpoint)
        {
            return Block(
        LocalDeclarationStatement(
            VariableDeclaration(
                PredefinedType(
                    Token(SyntaxKind.StringKeyword)))
            .WithVariables(
                SingletonSeparatedList<VariableDeclaratorSyntax>(
                    VariableDeclarator(
                        Identifier("requestUri"))
                    .WithInitializer(
                        EqualsValueClause(GetQueryStringSyntax(endpoint)))))),
        LocalDeclarationStatement(
            VariableDeclaration(
                IdentifierName("HttpResponseMessage"))
            .WithVariables(
                SingletonSeparatedList<VariableDeclaratorSyntax>(
                    VariableDeclarator(
                        Identifier("response"))
                    .WithInitializer(
                        EqualsValueClause(
                            AwaitExpression(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("client"),
                                        IdentifierName("GetAsync")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                IdentifierName("requestUri"))))))))))),
        LocalDeclarationStatement(
            VariableDeclaration(
                IdentifierName(
                    Identifier(
                        TriviaList(),
                        SyntaxKind.VarKeyword,
                        "var",
                        "var",
                        TriviaList())))
            .WithVariables(
                SingletonSeparatedList<VariableDeclaratorSyntax>(
                    VariableDeclarator(
                        Identifier("convertedResponse"))
                    .WithInitializer(
                        EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("JsonConvert"),
                                    GenericName(
                                        Identifier("DeserializeObject"))
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SingletonSeparatedList<TypeSyntax>(
                                                IdentifierName(endpoint.ReturnType))))))
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList<ArgumentSyntax>(
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("response"),
                                                            IdentifierName("Content")),
                                                        IdentifierName("ReadAsStringAsync"))),
                                                IdentifierName("Result"))))))))))),
        ReturnStatement(
            IdentifierName("convertedResponse")));
        }
        //пока пост только одного объекта
        private BlockSyntax PostBody(Endpoint endpoint)
        {
            return Block(
                LocalDeclarationStatement(
                    VariableDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.StringKeyword)))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                        Identifier("requestUri"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("String"),
                                                        IdentifierName("Format")))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList<ArgumentSyntax>(
                                                            Argument(
                                                                LiteralExpression(
                                                                    SyntaxKind.StringLiteralExpression,
                                                                    Literal(endpoint.Path))))))))))),
                LocalDeclarationStatement(
                    VariableDeclaration(
                            IdentifierName("HttpResponseMessage"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                        Identifier("response"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            AwaitExpression(
                                                InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("client"),
                                                            IdentifierName("PostAsJsonAsync")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SeparatedList<ArgumentSyntax>(
                                                                new SyntaxNodeOrToken[]
                                                                {
                                                                    Argument(
                                                                        IdentifierName("requestUri")),
                                                                    Token(SyntaxKind.CommaToken),
                                                                    Argument(
                                                                        IdentifierName(endpoint.Arguments[0].Name))
                                                                }))))))))));
        }
        public InvocationExpressionSyntax GetQueryStringSyntax(Endpoint endpoint)
        {
            List<SyntaxNodeOrToken> stringTokens = new List<SyntaxNodeOrToken>{Argument(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal(BuildQuery(endpoint))))};
            foreach (var arg in endpoint.Arguments)
            {
                stringTokens.Add(Token(SyntaxKind.CommaToken));
                stringTokens.Add(Argument(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(arg.Name),
                            IdentifierName("MyToString")))));
            }

            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("String"),
                        IdentifierName("Format")))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList<ArgumentSyntax>(stringTokens)));
        }
    }
}