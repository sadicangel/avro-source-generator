; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID     | Category      | Severity | Notes
------------|---------------|----------|---------------------------
AVROSG0001  | Compiler      | Error    | Invalid JSON — can't parse
AVROSG0002  | Compiler      | Error    | Invalid schema logic
AVROSG0003  | Configuration | Warning  | No Avro library detected
AVROSG0004  | Configuration | Warning  | Multiple Avro libraries detected
AVROSG0005  | Compiler      | Error    | Duplicate Avro schema
AVROSG9999  | Compiler      | Error    | Unknown generator error
