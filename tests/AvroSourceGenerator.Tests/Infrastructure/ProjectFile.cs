using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace AvroSourceGenerator.Tests.Infrastructure;

public readonly record struct ProjectFile(string Content, string Extension, string? LogicalPath = null)
{
    public bool IsSource => Extension is "cs";

    public string Hash { get; } = System.IO.Path.ChangeExtension(Base64Url.EncodeToString(SHA1.HashData(Encoding.UTF8.GetBytes(Content.ReplaceLineEndings("\n")))), Extension);

    public string Path => LogicalPath ?? Hash;

    public static ProjectFile CSharp(string content, string? path = null) => new(content, "cs", path);
    public static ProjectFile Schema([StringSyntax(StringSyntaxAttribute.Json)] string content, string? path = null) => new(content, "avsc", path);
    public static ProjectFile Protocol([StringSyntax(StringSyntaxAttribute.Json)] string content, string? path = null) => new(content, "avpr", path);
    public static ProjectFile Source(string content, string? path = null) => new(content, "avdl", path);
}
