; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID     | Category      | Severity | Notes
------------|---------------|----------|---------------------------
AVROSG0001  | Compiler      | Error    | Unsupported source type
AVROSG0002  | Compiler      | Error    | Empty source
AVROSG0003  | Compiler      | Error    | Duplicate source path
AVROSG0004  | Compiler      | Error    | Duplicate schema
AVROSG0005  | Compiler      | Error    | Missing schema references
AVROSG1000  | Compiler      | Error    | Invalid JSON
AVROSG1001  | Compiler      | Error    | Empty JSON
AVROSG1002  | Compiler      | Error    | Trailing JSON content
AVROSG2000  | Compiler      | Error    | Schema expected
AVROSG2001  | Compiler      | Error    | Protocol expected
AVROSG2002  | Compiler      | Error    | Missing root schema
AVROSG2003  | Compiler      | Error    | Invalid schema reference
AVROSG2004  | Compiler      | Error    | Missing schema property
AVROSG2005  | Compiler      | Error    | Invalid schema name
AVROSG2006  | Compiler      | Error    | Invalid protocol name
AVROSG2007  | Compiler      | Error    | Invalid field name
AVROSG2008  | Compiler      | Error    | Invalid request parameter name
AVROSG2009  | Compiler      | Error    | Invalid namespace
AVROSG2010  | Compiler      | Error    | Invalid schema type
AVROSG2011  | Compiler      | Error    | Invalid field type
AVROSG2012  | Compiler      | Error    | Invalid parameter type
AVROSG2013  | Compiler      | Error    | Invalid fields
AVROSG2014  | Compiler      | Error    | Invalid items
AVROSG2015  | Compiler      | Error    | Invalid values
AVROSG2016  | Compiler      | Error    | Invalid symbols
AVROSG2017  | Compiler      | Error    | Invalid aliases
AVROSG2018  | Compiler      | Error    | Invalid doc
AVROSG2019  | Compiler      | Error    | Invalid logical type
AVROSG2020  | Compiler      | Error    | Invalid types
AVROSG2021  | Compiler      | Error    | Invalid messages
AVROSG2022  | Compiler      | Error    | Invalid request
AVROSG2023  | Compiler      | Error    | Invalid response
AVROSG2024  | Compiler      | Error    | Invalid errors
AVROSG2025  | Compiler      | Error    | Invalid one-way
AVROSG2026  | Compiler      | Error    | Invalid enum default
AVROSG2027  | Compiler      | Error    | Invalid protocol declaration type
AVROSG2029  | Compiler      | Error    | Invalid fixed size
AVROSG2030  | Compiler      | Error    | Recursive schema definition
AVROSG2031  | Compiler      | Error    | Invalid one-way message
AVROSG3000  | Compiler      | Error    | Invalid character
AVROSG3001  | Compiler      | Error    | Invalid escape sequence
AVROSG3002  | Compiler      | Error    | Invalid number
AVROSG3003  | Compiler      | Error    | Unterminated documentation comment
AVROSG3004  | Compiler      | Error    | Unterminated comment
AVROSG3005  | Compiler      | Error    | Unterminated string
AVROSG3006  | Compiler      | Error    | Unterminated verbatim identifier
AVROSG3007  | Compiler      | Error    | Unexpected token
AVROSG3008  | Compiler      | Error    | Unexpected JSON value
AVROSG3009  | Compiler      | Error    | Misplaced annotation
AVROSG3010  | Compiler      | Error    | Misplaced documentation
AVROSG4000  | Compiler      | Error    | Invalid IDL document
AVROSG4001  | Compiler      | Error    | Invalid IDL declaration
AVROSG4002  | Compiler      | Error    | Invalid IDL namespace annotation
AVROSG4003  | Compiler      | Error    | Invalid IDL aliases annotation
AVROSG4004  | Compiler      | Error    | Invalid IDL logical type annotation
AVROSG4005  | Compiler      | Error    | Invalid IDL order annotation
AVROSG4006  | Compiler      | Error    | Invalid IDL enum default
AVROSG4010  | Compiler      | Error    | Invalid IDL fixed size
AVROSG4011  | Compiler      | Error    | Invalid IDL decimal precision
AVROSG4012  | Compiler      | Error    | Invalid IDL decimal scale
AVROSG4014  | Compiler      | Error    | Invalid IDL one-way message
AVROSG5000  | Compiler      | Error    | Import cycle
AVROSG5001  | Compiler      | Error    | Invalid import extension
AVROSG5002  | Compiler      | Error    | Missing import
AVROSG5003  | Compiler      | Error    | Invalid import target
AVROSG5004  | Compiler      | Warning  | Unused import
AVROSG6000  | Configuration | Warning  | No Avro library detected
AVROSG6001  | Configuration | Warning  | Multiple Avro libraries detected
