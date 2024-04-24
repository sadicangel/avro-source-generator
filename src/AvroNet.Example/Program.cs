//using AvroNet.Example;

//var user = new User
//{
//    Name = "John Doe",
//    Age = 69,
//    Description = "👌",
//};

var c = new Class();

Console.WriteLine(GetHi(c));
SetHi(c, "Hey");
Console.WriteLine(GetHi(c));


static string GetHi(Class c)
{
    return StaticPrivateMethod(c);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = $"get_{nameof(Class.Hi)}")]
    extern static string StaticPrivateMethod(Class c);
}
static void SetHi(Class c, string h)
{
    StaticPrivateMethod(c, h);

    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = $"set_{nameof(Class.Hi)}")]
    extern static void StaticPrivateMethod(Class c, string h);
}


public class Class
{
    public string Hi { get; init; } = "Hi";
}