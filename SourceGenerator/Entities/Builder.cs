using System;
using System.Collections.Generic;
using SourceGenerator.services;

namespace SourceGenerator.Entities
{
    public abstract class Builder
    {
        public List<PhpToken> _tokens { get; private set; }
        public Logger Logger { get; }
        public Builder(List<PhpToken> tokens)
        {
            _tokens = tokens;
            Logger = new Logger();
        }

        protected string DefineVariableType(string name)
        {
            //в пхп нет отдельного объявления и инициализации, поэтому сразу же можно посмотреть чем инициализирован.
            //Я надеюсь что автор сервера не меняет тип переменной на ходу.
            //Смотрим где переменную инициализировали
            int i = _tokens.FindIndex(x => x.Text == name);
            i++;
            while (i < _tokens.Count && (_tokens[i].TokenName is "T_WHITESPACE" or "(" or "T_NEW" or "="))
            {
                i++;
            }

            if (i == _tokens.Count)
            {
                throw new Exception("Variable " + name + "  declaration is not found!");
            }
            else if (_tokens[i].TokenName == "T_STRING")
            {
                return _tokens[i].Text;
            }
            else if (_tokens[i].TokenName == "T_ARRAY")
            {
                return $"{FindArrayType(name)}[]";
            }
            else
            {
                throw new Exception("Unknown type of variable: " + _tokens[i].TokenName + " text: " + _tokens[i].Text);
            }

        }

        private string FindArrayType(string name)
        {
            for (int i = 0; i < _tokens.Count - 1; i++)
            {
                //вставка в массив php выглядит как $arr_name[] = arr_item;
                if (_tokens[i].Text == name && _tokens[i + 1].Text == "[")
                {
                    i += 3;
                    while(i<_tokens.Count && (_tokens[i].TokenName is "T_WHITESPACE" or "(" or "T_NEW" or "="))
                    {
                        i++;
                    }
                    return DefineVariableType(_tokens[i].Text);
                }
            }

            return null;
        }
        protected int FindNextValuable(int n)
        {
            n++;
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
    }
}