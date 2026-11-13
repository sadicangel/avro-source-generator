namespace AvroSourceGenerator.Compiler;

internal static class ResultExtensions
{
    extension<T>(Result<Option<T>> result)
    {
        public Result<Option<TResult>> Traverse<TResult>(Func<T, Result<TResult>> map) =>
            result.Bind(option => option.Traverse(map));

        public Result<Option<TResult>> TraverseBind<TResult>(Func<T, Result<Option<TResult>>> bind) =>
            result.Bind(option => option.TraverseBind(bind));
    }
}
