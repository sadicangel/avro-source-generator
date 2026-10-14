---
title: Kafka and Schema Registry
nav_order: 7
---

# Kafka and Schema Registry

Your application works with generated C# models. The Kafka Avro serializer handles schema registration and the message format. Configure it for the same runtime you selected for generation, then produce and consume your model as usual.

## Before you start

Use the `User` schema from [Getting started]({{ '/getting-started/' | relative_url }}), a development Kafka broker, and Schema Registry. Create a dedicated `users` topic for this example. The defaults are `localhost:9092` and `http://localhost:8081`; set `KAFKA_BOOTSTRAP_SERVERS` and `SCHEMA_REGISTRY_URL` to use other endpoints.

If you have the Kafka CLI installed, create the topic with:

```bash
kafka-topics --bootstrap-server localhost:9092 --create --topic users --partitions 1 --replication-factor 1
```

Use the Apache.Avro integration below.

## Apache.Avro

Keep Apache.Avro selected for generation and install Confluent's Avro serializer:

```bash
dotnet add package Confluent.SchemaRegistry.Serdes.Avro --version 2.15.1
```

The generated `User` implements `ISpecificRecord`, which is the contract this serializer expects. The following round-trip example configures a typed producer and consumer, sends a `User`, and verifies that the matching message comes back with the same values:

```csharp
{% include examples/ApacheKafka.cs %}
```

The example prints:

```text
Round trip: Ada <ada@example.com>
```

The unique message key lets the consumer find the message sent by this run. A 45-second timeout prevents the example from waiting indefinitely. Each run uses a fresh consumer group and reads from the start of the demo topic; use your application's normal group and offset policy in production.

## Schemas and registration

This development example allows automatic schema registration. If schemas are provisioned separately in your environment, configure the serializer's registration behavior accordingly. Producer and consumer must agree on the Avro schema contract, and the runtime must support your logical types and union branches.

## Complete examples with local containers

The repository includes complete Kafka and Schema Registry examples, including Testcontainers setup:

- [Apache.Avro Kafka sample](https://github.com/sadicangel/avro-source-generator/tree/{{ site.docs_version.source_ref }}/samples/AvroSourceGenerator.ConfluentKafka).

This link matches the selected package version. Repository samples reference the generator project and use the SDK selected by their checkout's `global.json`; their build requirements can differ from an application consuming the NuGet package. Docker with Linux containers is required for the container examples.
