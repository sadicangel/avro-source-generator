using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace AvroSourceGenerator.Tests.Infrastructure;

public readonly record struct ProjectFile(string Content, string Extension)
{
    public bool IsSource => Extension is "cs";

    public string Hash { get; } = Path.ChangeExtension(Base64Url.EncodeToString(SHA1.HashData(Encoding.UTF8.GetBytes(Content.ReplaceLineEndings("\n")))), Extension);

    public static ProjectFile CSharp(string content) => new ProjectFile(content, "cs");
    public static ProjectFile Schema(string content) => new ProjectFile(content, "avsc");
    public static ProjectFile Subject(string content) => new ProjectFile(content, "subject.json");
}
