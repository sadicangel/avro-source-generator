namespace AvroSourceGenerator.Parsing;

[Flags]
internal enum AvroLibraryFlags
{
    None = 0,
    Apache = 1 << 0,
}

internal static class AvroLibraryFlagsExtensions
{
    extension(AvroLibraryFlags flags)
    {
        public bool HasMultiple => ((int)flags & ((int)flags - 1)) != 0;

        public IEnumerable<AvroLibrary> EnumerateFlags()
        {
            if ((flags & AvroLibraryFlags.Apache) != 0)
                yield return AvroLibrary.Apache;
        }

        public IEnumerable<string> EnumerateFlagNames()
        {
            if ((flags & AvroLibraryFlags.Apache) != 0)
                yield return "Apache (Apache.Avro)";
        }

        public bool TryGetSingleOrDefault(out AvroLibrary avroLibrary)
        {
            if (flags.HasMultiple)
            {
                avroLibrary = AvroLibrary.None;
                return false;
            }

            avroLibrary = flags switch
            {
                AvroLibraryFlags.Apache => AvroLibrary.Apache,
                _ => AvroLibrary.None,
            };

            return true;
        }
    }
}
