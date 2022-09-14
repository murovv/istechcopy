using System.Collections.Generic;

namespace SourceGenerator.Entities
{
    public static class TypeTransformer
    {
        private static Dictionary<string, string> functions = new Dictionary<string, string>(){{"intval", "int"},{"explode","int[]"}};

        public static string Transform(string toTransform)
        {
            if (functions.ContainsKey(toTransform))
            {
                return functions[toTransform];
            }

            return toTransform;
        }
    }
}