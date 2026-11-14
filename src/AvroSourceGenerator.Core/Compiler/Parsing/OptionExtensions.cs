namespace AvroSourceGenerator.Compiler.Parsing;

internal static class OptionExtensions
{
    extension<T>(Option<Result<T>> option)
    {
        public Result<Option<T>> Transpose()
            => option.Traverse(static result => result);
    }

    extension<T>(T? value) where T : struct
    {
        public Option<T> ToOption()
        => value.HasValue
            ? Option.Some(value.Value)
            : Option.None<T>();
    }

    extension<T>(T? value) where T : class
    {
        public Option<T> ToOption()
        => value is not null
            ? Option.Some(value)
            : Option.None<T>();
    }
}
