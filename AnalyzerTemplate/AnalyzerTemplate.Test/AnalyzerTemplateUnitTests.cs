using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = AnalyzerTemplate.Test.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerTemplateAnalyzer,
    AnalyzerTemplate.AnalyzerTemplateCodeFixProvider>;

namespace AnalyzerTemplate.Test
{
    [TestClass]
    public class AnalyzerTemplateUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"using System;
using System.Globalization;

namespace AnalyzerTemplate
{
    public class TEXT
    {
        public TEXT()
        {
            Int32.Parse(" + "\"123\"" + @");
            Char.Parse(" + "\"123\"" + @");
            UInt16.Parse(" + "\"123\"" + @");
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"using System;
using System.Globalization;
namespace AnalyzerTemplate
{
    public class TEXT
    {
        public TEXT()
        {
            Convert.ToInt32(" + "\"2\"" + @");
        }
    }
}";

            string fixtest = @"using System;
using System.Globalization;
namespace AnalyzerTemplate
{
    public class TEXT
    {
        public TEXT()
        {
            Int32.Parse(" + "\"2\"" + @");
        }
    }
}";
            AnalyzerTemplateCodeFixProvider fixProvider = new AnalyzerTemplateCodeFixProvider();
            DiagnosticResult expected = VerifyCS.Diagnostic("AnalyzerTemplate").WithLocation(9, 13);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"using System.Collections.Generic;
using System.Linq;

public class Text
{
    public Text()
    {
        List<int> t = new List<int>();
        t = t.Select(x => x*2).ToList();
    }
}";
            DiagnosticResult expected1 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(9, 22);
            await VerifyCS.VerifyCodeFixAsync(test, new[] {expected1}, test);
        }

        [TestMethod]
        public async Task TestMethod4()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        List<int> t = new List<int>();
        t = t.Select({|#0:x => x * x|}).ToList();
        Func<int, short, double> p = {|#1:(x, y) => x * y|};
    }
}";

            string fixtest = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        List<int> t = new List<int>();
        t = t.Select(x => Temp0(x)).ToList();
        Func<int, short, double> p = (x, y) => Temp1(x, y);
    }
    int Temp0(int x)
    {
        return x * x;
    }
    double Temp1(int x, short y)
    {
        return x * y;
    }
}";


            DiagnosticResult expected1 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(0).WithArguments(new[] {"x => x * x"});
            DiagnosticResult expected2 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(1).WithArguments(new[] {"(x, y) => x * y"});
            await VerifyCS.VerifyCodeFixAsync(test, new[] {expected1, expected2}, fixtest);
        }

        [TestMethod]
        public async Task TestMethod5()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        Func<int,List<int>, double> p = (x,y) => x * y.First(t=>t<3);
    }
}";

            string fixtest = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        Func<int,List<int>, double> p = (x,y) => Temp0(x, y);
    }
    double Temp0(int x, System.Collections.Generic.List<int> y)
    {
        return x * y.First(t => Temp2(t));
    }
    bool Temp2(int t)
    {
        return t < 3;
    }
}";


            DiagnosticResult expected1 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(9, 41);
            DiagnosticResult expected2 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(9, 62);
            await VerifyCS.VerifyCodeFixAsync(test, new[] {expected1, expected2}, fixtest);
        }

        [TestMethod]
        public async Task TestMethod6()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        Func<int> a = () => 1;
    }
}";

            string fixtest = @"using System;
using System.Collections.Generic;
using System.Linq;

public class Text
{
    public void T()
    {
        Func<int> a = () => Temp0();
    }
    int Temp0()
    {
        return 1;
    }
}";


            DiagnosticResult expected1 = VerifyCS.Diagnostic(new AnalyzerTemplateAnalyzer().SupportedDiagnostics[1])
                .WithLocation(9, 23);
            await VerifyCS.VerifyCodeFixAsync(test, new[] {expected1}, fixtest);
        }
    }
}