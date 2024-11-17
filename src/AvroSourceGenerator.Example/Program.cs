using AutoFixture;
using AvroSourceGenerator.Example;

static ExampleFixed MakeExampleFixed()
{
    var random = new Random();
    var @fixed = new ExampleFixed();
    random.NextBytes(@fixed.Value);
    return @fixed;
}

var fixture = new Fixture()
    .Build<Test>()
    .With(f => f.fixed_field, MakeExampleFixed)
    .With(f => f.null_fixed_field, MakeExampleFixed);

var test = fixture.Create<Test>();

Console.WriteLine(test);