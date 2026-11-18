; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID     | Category      | Severity | Notes
------------|---------------|----------|---------------------------
AVROSG0001  | Compiler      | Error    | Unsupported source type
AVROSG0002  | Compiler      | Error    | Empty source
AVROSG0003  | Compiler      | Error    | Duplicate source path
AVROSG1000  | Compiler      | Error    | Empty JSON
AVROSG1001  | Compiler      | Error    | Trailing JSON content
AVROSG1002  | Compiler      | Error    | Unexpected JSON value
AVROSG1003  | Compiler      | Error    | Invalid character
AVROSG1004  | Compiler      | Error    | Invalid escape sequence
AVROSG1005  | Compiler      | Error    | Invalid number
AVROSG1006  | Compiler      | Error    | Unterminated string
AVROSG1007  | Compiler      | Error    | Unexpected token
AVROSG1008  | Compiler      | Error    | JSON comment not allowed
AVROSG1009  | Compiler      | Error    | Unpaired surrogate in JSON property name
AVROSG1010  | Compiler      | Error    | JSON nesting limit exceeded
AVROSG2000  | Compiler      | Error    | Unterminated documentation comment
AVROSG2001  | Compiler      | Error    | Unterminated comment
AVROSG2002  | Compiler      | Error    | Unterminated verbatim identifier
AVROSG2003  | Compiler      | Error    | Misplaced annotation
AVROSG2004  | Compiler      | Error    | Misplaced documentation
AVROSG3000  | Compiler      | Error    | Schema expected
AVROSG3001  | Compiler      | Error    | Protocol expected
AVROSG3002  | Compiler      | Error    | Missing root schema
AVROSG3003  | Compiler      | Error    | Invalid IDL document
AVROSG3004  | Compiler      | Error    | Invalid IDL declaration
AVROSG3005  | Compiler      | Error    | Duplicate IDL directive
AVROSG4000  | Compiler      | Error    | Missing schema property
AVROSG4001  | Compiler      | Error    | Invalid schema name
AVROSG4002  | Compiler      | Error    | Invalid protocol name
AVROSG4003  | Compiler      | Error    | Invalid field name
AVROSG4004  | Compiler      | Error    | Invalid request parameter name
AVROSG4005  | Compiler      | Error    | Invalid namespace
AVROSG4006  | Compiler      | Error    | Invalid aliases
AVROSG4007  | Compiler      | Error    | Invalid doc
AVROSG4008  | Compiler      | Error    | Invalid logical type
AVROSG4009  | Compiler      | Error    | Invalid decimal precision
AVROSG4010  | Compiler      | Error    | Invalid decimal scale
AVROSG4011  | Compiler      | Error    | Invalid schema type
AVROSG4012  | Compiler      | Error    | Invalid field type
AVROSG4013  | Compiler      | Error    | Invalid parameter type
AVROSG4014  | Compiler      | Error    | Invalid protocol declaration type
AVROSG4015  | Compiler      | Error    | Invalid fields
AVROSG4016  | Compiler      | Error    | Invalid field order
AVROSG4017  | Compiler      | Error    | Invalid items
AVROSG4018  | Compiler      | Error    | Invalid values
AVROSG4019  | Compiler      | Error    | Invalid symbols
AVROSG4020  | Compiler      | Error    | Invalid enum default
AVROSG4021  | Compiler      | Error    | Invalid fixed size
AVROSG4022  | Compiler      | Error    | Invalid types
AVROSG4023  | Compiler      | Error    | Invalid messages
AVROSG4024  | Compiler      | Error    | Invalid request
AVROSG4025  | Compiler      | Error    | Invalid response
AVROSG4026  | Compiler      | Error    | Invalid errors
AVROSG4027  | Compiler      | Error    | Invalid one-way
AVROSG5000  | Compiler      | Error    | Invalid schema reference
AVROSG5001  | Compiler      | Error    | Duplicate schema
AVROSG5002  | Compiler      | Error    | Missing schema references
AVROSG5003  | Compiler      | Error    | Recursive schema definition
AVROSG5004  | Compiler      | Error    | Duplicate record or error field
AVROSG5005  | Compiler      | Error    | Duplicate enum symbol
AVROSG5006  | Compiler      | Error    | Duplicate union branch
AVROSG5007  | Compiler      | Error    | Immediately nested union
AVROSG5008  | Compiler      | Error    | Invalid one-way message
AVROSG6000  | Compiler      | Error    | Import cycle
AVROSG6001  | Compiler      | Error    | Invalid import extension
AVROSG6002  | Compiler      | Error    | Missing import
AVROSG6003  | Compiler      | Error    | Invalid import target
AVROSG6004  | Compiler      | Warning  | Unused import
AVROSG7000  | Configuration | Warning  | No Avro library detected
AVROSG7001  | Configuration | Warning  | Multiple Avro libraries detected
AVROSG7002  | Configuration | Error    | Chr.Avro support removed
