namespace AvroSourceGenerator.Tests.Data;

public static class TestSources
{
    private static readonly Dictionary<string, string> s_sources = new()
    {
        ["null"] = "schema null;",
        ["boolean"] = "schema boolean;",
        ["int"] = "schema int;",
        ["long"] = "schema long;",
        ["float"] = "schema float;",
        ["double"] = "schema double;",
        ["bytes"] = "schema bytes;",
        ["string"] = "schema string;",
        ["enum"] = """
        namespace SchemaNamespace;
        schema Enum;
        enum Enum { }
        """,
        ["enum<A,B,C>"] = """
        schema Enum;
        @namespace("SchemaNamespace")
        enum Enum {
            A,
            B,
            C
        }
        """,
        ["error"] = """
        @namespace("SchemaNamespace")
        error Error { }
        """,
        ["fixed"] = """
        schema Fixed;
        @namespace("SchemaNamespace")
        fixed Fixed(16);
        """,
        ["record"] = """
        schema Record;
        @namespace("SchemaNamespace")
        record Record { }
        """,
        ["array<string>"] = "schema array<string>;",
        ["array<record>"] = """
        schema array<Record>;
        @namespace("SchemaNamespace")
        record Record { }
        """,
        ["map<string>"] = "schema map<string>;",
        ["map<record>"] = """
        schema map<Record>;
        @namespace("SchemaNamespace")
        record Record { }
        """,
        ["[null, string]"] = "schema union { null, string };",
        ["string?"] = "schema string?;",
        ["[null, record]"] = """
        schema union { null, Record };
        @namespace("SchemaNamespace")
        record Record { }
        """,
        ["record?"] = """
        schema Record?;
        @namespace("SchemaNamespace")
        record Record { }
        """,
        ["protocol"] = """
        @namespace("SchemaNamespace")
        protocol RpcProtocol { }
        """,
        ["avdl.schema"] = """
        // Regular comments should be ignored by the scanner.
        namespace Avdl.Acceptance.Schema;
        schema User;

        /** Status enum documentation */
        @namespace("Avdl.Acceptance.Schema")
        @aliases(["OldStatus"])
        @owner("platform")
        enum Status {
            OPEN,
            CLOSED,
            FAILED
        } = OPEN;

        /** Hash bytes */
        @aliases(["OldHash"])
        fixed Md5Hash(16);

        /** Problem error */
        error Problem {
            /** Message documentation */
            string message;
        }

        /** Address documentation */
        @aliases(["PostalAddress"])
        record Address {
            string street = "Main";
            string? unit = null;
            map<string> labels = { "kind": "home" };
        }

        /** User documentation */
        @namespace("Avdl.Acceptance.Schema")
        @aliases(["Account"])
        @entity-kind("aggregate")
        record User {
            /** Identifier documentation */
            @logicalType("uuid") string id;
            @aliases(["fullName"]) string name = "Ada";
            Status status = OPEN;
            Md5Hash checksum;
            Address address;
            array<string> tags = ["admin", "active"];
            map<int> scores = { "reputation": 42 };
            union { null, string, int } searchKey = null;
            string? nickname = "ace";
            string? deletedReason = null;
            bytes payload;
            boolean active = true;
            float ratio = 1.5;
            double weight = 2.5;
            date birthDate;
            time_ms wakeUp;
            timestamp_ms createdAt;
            local_timestamp_ms localCreatedAt;
            decimal(9, 2) balance;
            Problem lastProblem;
            string @order("ignore") ignoredOrder = "ignored";
            string @field-note("kept") noted;
        }
        """,
        ["avdl.protocol"] = """
        namespace Avdl.Acceptance.Protocol;

        /** Acceptance service documentation */
        @namespace("Avdl.Acceptance.Protocol")
        @service-tier("gold")
        protocol AcceptanceService {
            /** Priority documentation */
            enum Priority {
                LOW,
                HIGH
            } = LOW;

            /** Trace identifier */
            fixed TraceId(16);

            /** Request documentation */
            @aliases(["OldSearchRequest"])
            record SearchRequest {
                @logicalType("uuid") string requestId;
                array<string> terms;
                map<string> filters;
                Priority priority = LOW;
                string? pageToken = null;
                decimal(12, 4) minimumScore;
                TraceId trace;
            }

            /** Response documentation */
            record SearchResponse {
                array<string> ids;
                union { null, string } nextPageToken = null;
                timestamp_ms generatedAt;
            }

            /** Service error documentation */
            error ServiceError {
                string message;
                int code = 500;
            }

            /** Search message documentation */
            SearchResponse search(SearchRequest request, string? pageToken = null, int limit = 10) throws ServiceError;

            /** Heartbeat message documentation */
            void heartbeat() oneway;
        }
        """
    };

    public static string Get(string schemaType) => s_sources[schemaType];
}
