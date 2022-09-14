using System.Collections.Generic;
using SourceGenerator.services;

namespace SourceGenerator.Entities
{
    public class PhpClass
    {
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
    }
}