//using AvroSourceGenerator.Tests.Helpers;

//namespace AvroSourceGenerator.Tests;

//public sealed class UnnamedBaseSchemaTests
//{
//    [Fact]
//    public Task Verify_Union() => TestHelper.VerifySourceCode("""
//    [
//        {
//            "type": "record",
//            "name": "Record1",
//            "namespace": "Namespace1",
//            "fields": []
//        },
//        {
//            "type": "record",
//            "name": "Record2",
//            "namespace": "Namespace2",
//            "fields": []
//        }
//    ]
//    """);

//    [Fact]
//    public Task Verify_Union_No_Named_Schemas_Diagnostic() => TestHelper.VerifyDiagnostic("[]");

//    [Fact]
//    public Task Verify_Array() => TestHelper.VerifySourceCode("""
//    {
//        "type": "array",
//        "items": {
//            "type": "record",
//            "name": "Record1",
//            "namespace": "Namespace1",
//            "fields": []
//        }
//    }
//    """);

//    [Fact]
//    public Task Verify_Array_No_Named_Schemas_Diagnostic() => TestHelper.VerifyDiagnostic("""
//    {
//        "type": "array",
//        "items": "string"
//    }
//    """);

//    [Fact]
//    public Task Verify_Map() => TestHelper.VerifySourceCode("""
//    {
//        "type": "map",
//        "values": {
//            "type": "record",
//            "name": "Record1",
//            "namespace": "Namespace1",
//            "fields": []
//        }
//    }
//    """);

//    [Fact]
//    public Task Verify_Map_No_Named_Schemas_Diagnostic() => TestHelper.VerifyDiagnostic("""
//    {
//        "type": "array",
//        "values": "string"
//    }
//    """);
//}
