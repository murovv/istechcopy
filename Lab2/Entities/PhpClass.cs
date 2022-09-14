using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Lab2.services
{
    public class PhpClass
    {
        public string Name { get; }
        public List<Argument> Fields { get; set; }
    }
}