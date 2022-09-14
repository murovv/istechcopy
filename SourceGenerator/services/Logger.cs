using System.IO;

namespace SourceGenerator.services
{
    public class Logger
    {
        private StreamWriter logFile;
        public Logger()
        {
            logFile = new StreamWriter("/home/paperblade/RiderProjects/Lab2/Lab2/services/parse.log");
        }

        public void Log(PhpToken token)
        {
            logFile.Write(token.TokenName+" "+token.Text);
        }
    }
}