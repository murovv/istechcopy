using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lab2.services
{
    public class Parser
    {
        public List<string> ParseStructure(string root)
        {
            List<string> t = new List<string>(Directory.GetFiles(root, "*.*", SearchOption.AllDirectories));
            Regex regex = new Regex(" *idea| *.json| *services");
            t = t.Where(e=>!regex.IsMatch(e)).Select(e => Regex.Replace(e, "/home/paperblade/PhpstormProjects/php_rest_2/*", String.Empty)).ToList();
            return t;
        }
    }
}