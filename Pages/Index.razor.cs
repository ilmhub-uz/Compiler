using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace CsharpCompiler.Pages
{
    public partial class Index
    {
        public string Output = "";
        const string DefaultCode = @"using System;
var a = Console.ReadLine().Split();
Console.WriteLine(a[2]);
";

        [Inject] private HttpClient _httpClient { get; set; }
        [Inject] private Monaco Monaco { get; set; }

        protected override Task OnInitializedAsync()
        {
            Compiler.InitializeMetadataReferences(_httpClient);
            return base.OnInitializedAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                Monaco.Initialize("container", DefaultCode, "csharp");
                Run();
            }
        }

        public Task Run()
        {
            return Compiler.WhenReady(RunInternal);
        }

        async Task RunInternal()
        {
            Output = "";
            Console.WriteLine("Compiling and Running code");
            var sw = Stopwatch.StartNew();
            var bytes = Encoding.UTF8.GetBytes(await _httpClient.GetStringAsync("sample-data/1/1.in"));
            var reader  = new StreamReader(new MemoryStream(bytes));
            Console.SetIn(reader);
            
            var currentOut = Console.Out;
            
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                var (success, asm) = Compiler.LoadSource(Monaco.GetCode("container"));
                if (success)
                {
                    var entry = asm.EntryPoint;
                    if (entry.Name == "<Main>") // sync wrapper over async Task Main
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for the async Task Main
                    }
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            var result1 = writer.ToString();
            var result2 = (await _httpClient.GetStringAsync("sample-data/1/1.out"));
            Output=result1;
            //tekshiruv qismi sample datadan keladi
            // if((result1.TrimEnd() == result2.TrimEnd()))
            // {
            //     Output = "Success";
            // }
            // else if(exception != null)
            // {
            //     Output = "Runtime error\r\n" + exception.ToString();
            // }
            // else
            // {
            //     Output = "Wrong answer";
            // }
        
            Console.SetOut(currentOut);
            sw.Stop();
            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");
        }
    }
}