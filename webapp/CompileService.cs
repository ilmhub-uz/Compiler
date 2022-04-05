using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class CompileService : ICompileService
{
    private readonly HttpClient _client;
    private readonly NavigationManager _uriHelper;
    public List<string> CompileLog { get; set; }
    public List<MetadataReference> References { get; set; }

    public CompileService(HttpClient client, NavigationManager uriHelper)
    {
        _client = client;
        _uriHelper = uriHelper;
    }

    public async Task Init()
    {
        if (References is null)
        {
            References = new List<MetadataReference>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }
                var name = assembly.GetName().Name + ".dll";

                Console.WriteLine($"{name}");

                References.Add(MetadataReference.CreateFromStream(await _client.GetStreamAsync(_uriHelper.BaseUri + "/_framework/_bin/" + name)));
            }
        }
    }

    public async Task<Type> CompileBlazor(string code)
    {
        CompileLog.Add("Create fileSystem");
        var fileSystem = new EmptyRazorProjectFileSystem();

        CompileLog.Add("Create engine");
        var engine = RazorProjectEngine.Create(RazorConfiguration.Create(RazorLanguageVersion.Version_3_0, "Blazor", new RazorExtension[0]), fileSystem);

        CompileLog.Add("Create file");
        var file = new MemoryRazorProjectItem(code, "/App", "/App/App.razor", true);

        CompileLog.Add("File process and GetCSharpDocument");
        var doc = engine.Process(file).GetCSharpDocument();

        CompileLog.Add("Get GeneratedCode");
        var csCode = doc.GeneratedCode;

        CompileLog.Add("Read Diagnostics");
        foreach (var diagnostic in doc.Diagnostics)
        {
            CompileLog.Add(diagnostic.ToString());
        }

        if (doc.Diagnostics.Any(i => i.Severity == RazorDiagnosticSeverity.Error))
        {
            return null;
        }

        CompileLog.Add(csCode);
        CompileLog.Add("Compile assembly");

        var assembly = await Compile(csCode);

        if (assembly is not null)
        {
            CompileLog.Add("Search Blazor component");
            return assembly.GetExportedTypes().FirstOrDefault(i => i.IsSubclassOf(typeof(ComponentBase)));
        }

        return null;
    }

    private async Task<Assembly> Compile(string code)
    {
        await Init();

        var syntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));

        foreach (var diagnostic in syntaxTree.GetDiagnostics())
        {
            CompileLog.Add(diagnostic.ToString());
        }
        if (syntaxTree.GetDiagnostics().Any(i => i.Severity == DiagnosticSeverity.Error))
        {
            CompileLog.Add("Parse SyntaxTree Error!");
            return null;
        }

        CompileLog.Add("Parse SyntaxTree Success");

        var compilation = CSharpCompilation.Create("webapp.Demo", new[] { syntaxTree },
                                References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();

        var result = compilation.Emit(stream);

        foreach (var diagnostic in result.Diagnostics)
        {
            CompileLog.Add(diagnostic.ToString());
        }

        if (!result.Success)
        {
            CompileLog.Add("Compilation error");
            return null;
        }

        CompileLog.Add("Compilation Success");

        stream.Seek(0, SeekOrigin.Begin);

        var assembly = AppDomain.CurrentDomain.Load(stream.ToArray());
        return assembly;
    }

    public async Task<string> CompileAndRun(string code)
    {
        await Init();

        var assembly = await Compile(code);

        if (assembly is not null)
        {
            var type = assembly.GetExportedTypes().FirstOrDefault();
            var methodInfo = type.GetMethod("Run");
            var instance = Activator.CreateInstance(type);
            return methodInfo.Invoke(instance, new object[] { "my UserName", 12 }).ToString();
        }
        return null;
    }
}