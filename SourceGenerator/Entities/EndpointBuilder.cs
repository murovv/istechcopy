using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SourceGenerator.services;

namespace SourceGenerator.Entities
{
    public class EndpointBuilder:Builder
    {
        public Endpoint Endpoint { get;}

        public EndpointBuilder(List<PhpToken> tokens):base(tokens)
        {
            Endpoint = new Endpoint();
        }

        public Endpoint Build(string path)
        {
            Endpoint.Path = path;
            Endpoint.Method = GetMethod().Value;
            Endpoint.ReturnType = GetResponseType();
            if (Endpoint.Method == HttpMethod.POST)
            {
                Endpoint.ReturnType = "void";
            }
            Endpoint.Arguments =  GetArguments().ToList();
            return Endpoint;
        }

        private int FindResponseStatus(int n)
        {
            while (!_tokens[n].Text.Contains( "http_response_code"))
            {
                n--;
            }

            n = FindNextValuable(n);
            return Convert.ToInt32(_tokens[n].Text);
        }

        private HttpMethod? GetMethod()
        {
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].Text == "$_SERVER")
                {
                    HttpMethod method;
                    while (!Enum.TryParse(_tokens[i].Text.Replace("\'",""), out method))
                    {
                        i++;
                    }

                    return method;
                }
            }

            return null;
        }

        private IEnumerable<Argument> GetArguments()
        {
            HashSet<string> variables = new HashSet<string>();
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].Text is "$_POST" or "$_GET")
                {
                    var variableAss = _tokens.First(x => x.line == _tokens[i].line && x.TokenName == "T_VARIABLE");
                    variables.Add(variableAss.Text);
                }
            }

            foreach (var variable in variables)
            {
                var type = TypeTransformer.Transform(DefineVariableType(variable));
                var name = variable.Replace("$","");
                yield return new Argument(type, name, ArgumentType.BODY);
            }
        }

        private string GetResponseType()
        {
            string responseType = "";
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].TokenName == "T_ECHO")
                {
                    i++;
                    while(i<_tokens.Count && (_tokens[i].TokenName == "T_WHITESPACE" || _tokens[i].TokenName == "(" || _tokens[i].Text=="json_encode"))
                    {
                        i++;
                    }

                    if (i == _tokens.Count)
                    {
                        return null;
                    }

                    int status = FindResponseStatus(i);
                    if (status == 200 || status == 201)
                    {
                        if (_tokens[i].TokenName == "T_ARRAY")
                        {
                            responseType = "Dictionary<string,string>";//нам дали просто массив, что в нем лежит понять нам не дано
                        }

                        else if (_tokens[i].TokenName == "T_VARIABLE")
                        {
                            responseType = TypeTransformer.Transform(DefineVariableType(_tokens[i].Text));
                        }
                        else
                        {
                            throw new Exception("Неожиданный тип токена: " + _tokens[i].TokenName);
                        }
                    }
                }
            }

            return responseType;
        }


        public void SetType()
        {
            PhpToken token = _tokens.FirstOrDefault(e => Regex.IsMatch(e.Text, " *Access-Control-Allow-Methods: *"));
            string method = token.Text.Split(' ').Last();
            method.Trim();
            method = method.Replace("\"", String.Empty);
            switch(method)
            {
                case "GET":
                    Endpoint.Method = HttpMethod.GET;
                    return;
                case "POST":
                    Endpoint.Method = HttpMethod.POST;
                    return;
                default:
                    throw new Exception("Неизвестный вид запроса: " + method);
            }
        }

    }
}