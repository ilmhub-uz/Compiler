using System.Text;
using Microsoft.AspNetCore.Razor.Language;

public class EmptyRazorProjectFileSystem : RazorProjectFileSystem
{
    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        return Array.Empty<RazorProjectItem>();
    }

    public override RazorProjectItem GetItem(string path)
    {
        throw new NotImplementedException();
    }

    public override RazorProjectItem GetItem(string path, string fileKind)
    {
        throw new NotImplementedException();
    }
}

public class MemoryRazorProjectItem : RazorProjectItem
{
    private byte[] data;

    public override string BasePath { get; }

    public override string FilePath { get; }

    public override string PhysicalPath { get; }

    public override bool Exists { get; }

    public override Stream Read()
    {
        return new MemoryStream(data);
    }

    public MemoryRazorProjectItem(string code, string basePath, string filePath, bool exists)
    {
        if(code is not null)
        {
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(code);

            data = new byte[preamble.Length + contentBytes.Length];
            preamble.CopyTo(data, 0);
            contentBytes.CopyTo(data, preamble.Length); 
        }
        Exists = exists;
        BasePath = basePath;
        FilePath = filePath;
        PhysicalPath = filePath;
    }
}