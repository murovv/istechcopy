using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lab2.services
{
    public class EndpointBuilder
    {
        public Endpoint Endpoint { get; private set; }
        private string _sourceRoot;
        public List<PhpToken> _tokens { get; private set; }
        public Logger Logger { get; }
        public EndpointBuilder(string sourceRoot)
        {
            Endpoint = new Endpoint();
            _sourceRoot = sourceRoot;
            Logger = new Logger();
        }
        private FileStream FindSource()
        {
            FileInfo[] fileInfo = new DirectoryInfo(_sourceRoot).GetFiles( "*" + Endpoint.NameWithoutExtension + "*.*");
            if (fileInfo.Length != 1)
            {
                throw new Exception("ошибка нахождения ресурсов");
            }

            FileStream ans = new FileStream(fileInfo[0].ToString(),FileMode.Open,FileAccess.Read);
            return ans;
        }
        public void AddPath(string path)
        {
            Endpoint.Path = path;
            _tokens = JsonSerializer.Deserialize<List<PhpToken>>(FindSource());
        }

        private int FindNextValuable(int n)
        {
            while ((_tokens[n].TokenName == "T_WHITESPACE" || _tokens[n].TokenName == "(") && n<_tokens.Count)
            {
                n++;
            }

            if (n == _tokens.Count)
            {
                throw new Exception("Out of bound while finding argument");
            }

            return n;
        }

        private int FindResponseStatus(int n)
        {
            while (!Regex.IsMatch(_tokens[n].Text, "*http_response_code*"))
            {
                n--;
            }

            n = FindNextValuable(n);

            return Convert.ToInt32(_tokens[n].Text);
        }

        private void FindAllResponses()
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
                        return;
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
                            DefineVariableType(_tokens[i].Text);
                        }
                        else
                        {
                            throw new Exception("Неожиданный тип токена: " + _tokens[i].TokenName);
                        }
                    }
                }
            }
        }

        public string DefineVariableType(string Name)
        {
            //в пхп нет отдельного объявления и инициализации, поэтому сразу же можно посмотреть чем инициализирован.
            //Я надеюсь что автор сервера не меняет тип переменной на ходу.
            //Смотрим где переменную инициализировали
            int i = _tokens.FindIndex(x => x.Text == Name);
            while(i<_tokens.Count && (_tokens[i].TokenName is "T_WHITESPACE" or "(" or "T_NEW"))
            {
                i++;
            }

            if (i == _tokens.Count)
            {
                throw new Exception("Variable " + Name + "  declaration is not found!");
            }
            else if (_tokens[i].TokenName == "T_STRING")
            {
                return _tokens[i].TokenName;
            }
            else
            {
                throw new Exception("Unknown type of variable: " + _tokens[i].TokenName + " text: " + _tokens[i].Text);
            }

        }

        public void SetType()
        {
            Console.WriteLine(_tokens.Count);
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