using System.Collections.Generic;
using System.Linq;
using SourceGenerator.services;

namespace SourceGenerator.Entities
{
    public class PhpClassBuilder:Builder
    {
        private PhpClass _class;
        public PhpClassBuilder(List<PhpToken> tokens) : base(tokens)
        {
            _class = new PhpClass();
        }

        public PhpClass Build()
        {
            _class.Name = GetName();
            _class.Fields = GetFields().ToList();
            return _class;
        }

        public IEnumerable<Field> GetFields()
        {
            foreach (var token in _tokens.FindAll(x=>x.TokenName=="T_PUBLIC"))
            {
                int pos = FindNextValuable(_tokens.FindIndex(x=>x==token));
                string type = _tokens[pos].Text;
                if (type == "?")
                {
                    pos = FindNextValuable(pos);
                    type = _tokens[pos].Text + "?";
                }
                pos = FindNextValuable(pos);
                string name = _tokens[pos].Text.Replace("$","");
                yield return new Field(type, name);
            }
        }

        public string GetName()
        {
            return _tokens[FindNextValuable(_tokens.FindIndex(x => x.TokenName == "T_CLASS"))].Text;
        }
    }
}