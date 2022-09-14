using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Entities;
using SourceGenerator.services;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceGenerator
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
                .WithUsings(
                    List<UsingDirectiveSyntax>(
                        new UsingDirectiveSyntax[]{
                            UsingDirective(
                                IdentifierName("System")),
                            UsingDirective(
                                QualifiedName(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("Net")),
                                    IdentifierName("Http"))),
                            UsingDirective(
                                QualifiedName(
                                    QualifiedName(
                                        QualifiedName(
                                            IdentifierName("System"),
                                            IdentifierName("Net")),
                                        IdentifierName("Http")),
                                    IdentifierName("Headers"))),
                            UsingDirective(
                                QualifiedName(
                                    QualifiedName(
                                        QualifiedName(
                                            IdentifierName("System"),
                                            IdentifierName("Net")),
                                        IdentifierName("Http")),
                                    IdentifierName("Json"))),
                            UsingDirective(
                                QualifiedName(
                                    IdentifierName("Newtonsoft"),
                                    IdentifierName("Json")))}))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                                IdentifierName("AwesomeNamespace"))
                            .WithMembers(
                                List<MemberDeclarationSyntax>(GetClasses()))
                            ))
                .NormalizeWhitespace();
        }

        private MemberDeclarationSyntax[] GetClientMembers()
        {
            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();
            members.Add(GetFieldDeclarationSyntax());
            members.Add(GetConstructor());
            members.AddRange(GetEndpoints());
            return members.ToArray();
        }

        private ClassDeclarationSyntax[] GetClasses()
        {
            return _classes.Select(GetDtoDeclaration).Append(GetMainClass()).Append(ToStringClass()).ToArray();
        }

        private ClassDeclarationSyntax GetMainClass()
        {
            return ClassDeclaration(_clientName)
                .WithModifiers(
                    TokenList(
                        new[]{Token(SyntaxKind.PublicKeyword),Token(SyntaxKind.StaticKeyword)}))
                .WithMembers(
                    List<MemberDeclarationSyntax>(GetClientMembers()));
        }
        private MethodDeclarationSyntax[] GetEndpoints()
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
                        IdentifierName(prop.Type),
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

        private MethodDeclarationSyntax BuildEndpoint(Endpoint endpoint)
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
            return MethodDeclaration(
                    IdentifierName(endpoint.ReturnType),
                    //FunctionName
                    Identifier(endpoint.NameWithoutExtension))
                .WithModifiers(
                    TokenList(
                        new []{Token(SyntaxKind.PublicKeyword),Token(SyntaxKind.StaticKeyword)}))
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
                                                                    IdentifierName("System.Collections.IEnumerable"))))))),
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
                                                    IdentifierName("System.Collections.IEnumerable"),
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
                Console.WriteLine(param.Name);

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
                            MemberAccessExpression(
    SyntaxKind.SimpleMemberAccessExpression,
    InvocationExpression(
        MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("client"),
            IdentifierName("GetAsync")))
    .WithArgumentList(
        ArgumentList(
            SingletonSeparatedList<ArgumentSyntax>(
                Argument(
                    IdentifierName("requestUri"))))),
    IdentifierName(
        Identifier(
            TriviaList(),
            "Result",
            TriviaList(
                Trivia(
                    SkippedTokensTrivia()
                    .WithTokens(
                        TokenList(
                            Token(SyntaxKind.SemicolonToken))))))))))))),
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
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("client"),
                                                            IdentifierName("PostAsJsonAsync")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SeparatedList<ArgumentSyntax>(
                                                                new SyntaxNodeOrToken[]{
                                                                    Argument(
                                                                        IdentifierName("requestUri")),
                                                                    Token(SyntaxKind.CommaToken),
                                                                    Argument(
                                                                        IdentifierName(endpoint.Arguments[0].Name))}))),
                                                IdentifierName(
                                                    Identifier(
                                                        TriviaList(),
                                                        "Result",
                                                        TriviaList(
                                                            Trivia(
                                                                SkippedTokensTrivia()
                                                                    .WithTokens(
                                                                        TokenList(
                                                                            Token(SyntaxKind.SemicolonToken))))))))
                                                                ))))));
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

        private FieldDeclarationSyntax GetFieldDeclarationSyntax()
        {
            return FieldDeclaration(
                VariableDeclaration(
                        IdentifierName("HttpClient"))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier("client"))))
                ).WithModifiers(
                TokenList(
                    Token(SyntaxKind.StaticKeyword)));
        }

        private ConstructorDeclarationSyntax GetConstructor()
        {
            return ConstructorDeclaration(
                    Identifier(_clientName))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(
                    ParameterList())
                .WithBody(
                    Block(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName("client"),
                                ObjectCreationExpression(
                                        IdentifierName("HttpClient"))
                                    .WithArgumentList(
                                        ArgumentList()))),
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("client"),
                                    IdentifierName("BaseAddress")),
                                ObjectCreationExpression(
                                        IdentifierName("Uri"))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal("http://localhost:5002")))))))),
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("client"),
                                                IdentifierName("DefaultRequestHeaders")),
                                            IdentifierName("Accept")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                ObjectCreationExpression(
                                                        IdentifierName("MediaTypeWithQualityHeaderValue"))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SingletonSeparatedList<ArgumentSyntax>(
                                                                Argument(
                                                                    LiteralExpression(
                                                                        SyntaxKind.StringLiteralExpression,
                                                                        Literal("application/json")))))))))))));
        }
    }
}