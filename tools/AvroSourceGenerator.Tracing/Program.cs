using Microsoft.Diagnostics.Tracing.Session;

Console.WriteLine("TraceEventSession started.");

// https://www.meziantou.net/measuring-performance-of-roslyn-source-generators.htm
using var session = new TraceEventSession("roslyn-sg");
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("TraceEventSession ended.");
    session.Dispose();
};

session.Source.Dynamic.AddCallbackForProviderEvent(
    "Microsoft-CodeAnalysis-General",
    "SingleGeneratorRunTime/Stop",
    traceEvent =>
    {
        var generatorName = (string)traceEvent.PayloadByName("generatorName");
        var ticks = (long)traceEvent.PayloadByName("elapsedTicks");
        // var id = (string)traceEvent.PayloadByName("id");
        // var assemblyPath = (string)traceEvent.PayloadByName("assemblyPath");

        if (generatorName == "AvroSourceGenerator.AvroSourceGenerator")
            Console.WriteLine($"{generatorName}: {TimeSpan.FromTicks(ticks).TotalMilliseconds:N0}ms");

        // As suggested by Lucas Trzesniewski (https://twitter.com/Lucas_Trz/status/1631739866915434497)
        // you can use Console.Beep() to get a sound when a source generator is executed.
        // If this is too noisy, you know one of the source generators doesn't use the cache correctly.
    });

session.EnableProvider("Microsoft-CodeAnalysis-General");
session.Source.Process();
