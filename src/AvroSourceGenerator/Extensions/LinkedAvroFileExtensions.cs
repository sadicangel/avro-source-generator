using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Extensions;

public static class LinkedAvroFileExtensions
{
    extension(LinkedAvroFile)
    {
        public static LinkedAvroFile Link((AvroFile File, SymbolTable SymbolTable) input, CancellationToken cancellationToken) =>
            LinkedAvroFile.Link(input.File, input.SymbolTable, cancellationToken);
    }
}
