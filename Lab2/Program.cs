using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lab2.services;
using Lab2.source;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using HttpMethod = Lab2.services.HttpMethod;
partial class Program
{

    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5002/");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Product product = new Product
        {
            id = 13,
            category_id = 123,
            category_name = "asd",
            description = "my perfect description",
            name = "asdaav",
            price = 300
        };
        string requestUri = String.Format("product/create.php");
        HttpResponseMessage response = await client.PostAsJsonAsync(requestUri,product);
        List<Argument> arguments_read = new List<Argument>
            {new Argument(AccessModifier.PUBLIC, "Product", "product", ArgumentType.BODY)};
        Endpoint get_read = new Endpoint
        {
            Path = "products/create.php",
            ReturnType = "void",
            Arguments = arguments_read,
            Method = HttpMethod.POST
        };
    }
    static partial void HelloFrom(string name);

}