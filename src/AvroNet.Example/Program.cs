using AutoFixture;
using AvroNet.Example;


var test = new Fixture().Create<Test>();

Console.WriteLine(test);