using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SourceGenerator.Entities;
using SourceGenerator.services;

namespace SourceGenerator
{
    [Generator]
    public class MyWonderfulSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);
            /*List<Argument> argumentsReadOne = new() {new Argument( "int", "id", ArgumentType.BODY)};
            List<Argument> argumentsCreate = new() {new Argument("Product", "product", ArgumentType.BODY)};
            List<Argument> argumentsPurchase = new() {new Argument("int[]", "ids", ArgumentType.BODY)};
            Endpoint read = new()
            {
                Path = "product/read.php",
                ReturnType = "Product[]",
                Arguments = new List<Argument>(),
                Method = SourceGenerator.services.HttpMethod.GET
            };
            Endpoint read_one = new()
            {
                Path = "product/read_one.php",
                ReturnType = "Product",
                Arguments = argumentsReadOne,
                Method = SourceGenerator.services.HttpMethod.GET
            };
            Endpoint create = new()
            {
                Path = "product/create.php",
                ReturnType = "void",
                Arguments = argumentsCreate,
                Method = SourceGenerator.services.HttpMethod.POST
            };
            Endpoint purchase = new()
            {
                Path = "product/purchase.php",
                ReturnType = "int",
                Arguments = argumentsPurchase,
                Method = SourceGenerator.services.HttpMethod.GET
            };
            List<Field> fields = new List<Field>()
            {
                new Field("int","id"),
                new Field("string","Name"),
                new Field("string","description"),
                new Field("int","price"),
                new Field("int","category_id"),
                new Field("string","category_name"),
                new Field("string","created"),
            };
            PhpClass Product = new PhpClass
            {
                Name = "Product",
                Fields = fields
            };*/
            Parser parser = new Parser("/home/paperblade/RiderProjects/Lab2/SourceGenerator/source","/home/paperblade/PhpstormProjects/php_rest_2");
            ClientBuilder builder = new ClientBuilder(parser.GetEndpoints(), parser.GetDtos(),
                "GeneratedClient");

            // Build up the source code
            string source = builder.Build().ToFullString();
            Console.WriteLine(source);
            var typeName = mainMethod.ContainingType.Name;
            Console.WriteLine($"{typeName}.g.cs");
            // Add the source code to the compilation
            context.AddSource($"{typeName}.g.cs", source);
        }
    }
}