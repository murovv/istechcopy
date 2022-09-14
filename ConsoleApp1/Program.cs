using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using AwesomeNamespace;

using SourceGenerator;
using SourceGenerator.Entities;
using SourceGenerator.services;


namespace ConsoleApp1
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(GeneratedClient.read_one(1).name+" "+ GeneratedClient.read_one(1).price);
            Console.WriteLine(GeneratedClient.read_one(2).name+" "+ GeneratedClient.read_one(2).price);
            Console.WriteLine(GeneratedClient.purchase(new[]{1,2}));
            foreach (Product product in GeneratedClient.read())
            {
                Console.WriteLine(product.name);
            }

        }
    }
}