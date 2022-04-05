using Microsoft.CodeAnalysis;

public interface ICompileService
{
    List<string> CompileLog { get; set; }
    List<MetadataReference> References { get; set; }
    Task<string> CompileAndRun(string code);
    Task<Type> CompileBlazor(string code);
    Task Init();
}