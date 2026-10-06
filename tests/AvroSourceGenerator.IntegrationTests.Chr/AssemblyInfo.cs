using AvroSourceGenerator.IntegrationTests;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(XUnitSerializer), typeof(FileInfo))]
