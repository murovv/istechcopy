using System.Collections.Generic;
using System.Linq;

namespace Lab2.services
{
    public class Endpoint
    {
        public string Path { get; set; }
        public string Name => Path.Split('/').Last();
        public string NameWithoutExtension => Name.Split('.').First();
        public HttpMethod Method { get; set; }
        public string ReturnType { get; set; }
        public List<Argument> Arguments { get; set; }
    }
}