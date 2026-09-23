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
AVROSG2003  | Compiler      | Error    | Invalid schema value
AVROSG2004  | Compiler      | Error    | Object expected
AVROSG2005  | Compiler      | Error    | Missing schema property
AVROSG2006  | Compiler      | Error    | Invalid string property
AVROSG2007  | Compiler      | Error    | Invalid array property
AVROSG2008  | Compiler      | Error    | Invalid object property
AVROSG2009  | Compiler      | Error    | Invalid boolean property
AVROSG2010  | Compiler      | Error    | Invalid string array element
AVROSG2011  | Compiler      | Error    | Invalid fixed size
AVROSG2012  | Compiler      | Error    | Invalid Avro name
AVROSG2013  | Compiler      | Error    | Unknown schema type
AVROSG2014  | Compiler      | Error    | Recursive schema definition
AVROSG2015  | Compiler      | Error    | Invalid one-way message
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
AVROSG4002  | Compiler      | Error    | Invalid IDL type
AVROSG4003  | Compiler      | Error    | Invalid IDL schema declaration
AVROSG4004  | Compiler      | Error    | Invalid IDL primitive
AVROSG4005  | Compiler      | Error    | Invalid IDL fixed size
AVROSG4006  | Compiler      | Error    | Invalid IDL decimal precision
AVROSG4007  | Compiler      | Error    | Invalid IDL decimal scale
AVROSG4008  | Compiler      | Error    | Invalid IDL logical type
AVROSG4009  | Compiler      | Error    | Invalid IDL one-way message
AVROSG5000  | Compiler      | Error    | Import cycle
AVROSG5001  | Compiler      | Error    | Invalid import extension
AVROSG5002  | Compiler      | Error    | Missing import
AVROSG5003  | Compiler      | Error    | Invalid import target
AVROSG6000  | Configuration | Warning  | No Avro library detected
AVROSG6001  | Configuration | Warning  | Multiple Avro libraries detected
