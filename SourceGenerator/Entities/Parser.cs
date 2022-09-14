using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SourceGenerator.Entities;
using SourceGenerator.services;

namespace SourceGenerator
{
    public class Parser
    {
        private string _sourceRoot;
        private string _serverRoot;

        public Parser(string sourceRoot, string serverRoot)
        {
            _sourceRoot = sourceRoot;
            _serverRoot = serverRoot;
        }

        public IEnumerable<Endpoint> GetEndpoints()
        {
            List<string> structure = ParseStructure(_serverRoot);
            foreach (var path in structure)
            {
                var tokens = GetTokens(path);
                if (tokens.All(x => x.TokenName != "T_CLASS"))
                {
                    yield return new EndpointBuilder(tokens).Build(path);
                }
            }
        }

        public IEnumerable<PhpClass> GetDtos()
        {
            List<string> structure = ParseStructure(_serverRoot);
            foreach (var path in structure)
            {
                var tokens = GetTokens(path);
                if (tokens.Any(x => x.TokenName == "T_CLASS"))
                {
                    yield return new PhpClassBuilder(tokens).Build();
                }
            }
        }
        public List<string> ParseStructure(string root)
        {
            List<string> t = new List<string>(Directory.GetFiles(root, "*.*", SearchOption.AllDirectories));
            Regex regex = new Regex(" *idea| *.json| *services| *config");
            t = t.Where(e=>!regex.IsMatch(e)).Select(e => Regex.Replace(e, "/home/paperblade/PhpstormProjects/php_rest_2/*", String.Empty)).ToList();
            return t;
        }

        public List<PhpToken> GetTokens(string path)
        {
            string name = path.Split('/').Last().Replace(".php",".json");
            FileStream source = FindSource(_sourceRoot + "/" + name);
            return JsonSerializer.Deserialize<List<PhpToken>>(source);
        }
        private FileStream FindSource(string source)
        {

            FileInfo fileInfo = new FileInfo(source);
            FileStream ans = new FileStream(fileInfo.ToString(),FileMode.Open,FileAccess.Read);
            return ans;
        }
    }
}