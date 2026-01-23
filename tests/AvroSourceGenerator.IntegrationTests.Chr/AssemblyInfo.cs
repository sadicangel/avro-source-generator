using AvroSourceGenerator.IntegrationTests;
using Xunit.Sdk;

[assembly: AssemblyFixture(typeof(DockerFixture))]
[assembly: RegisterXunitSerializer(typeof(XUnitSerializer), typeof(FileInfo))]
