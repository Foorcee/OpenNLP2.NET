using System.IO.Compression;

namespace OpenNLP2.Build;

internal static class JarClassVersionReader
{
    public static ushort? GetClassVersion(string jarFilePath)
    {
        if (!File.Exists(jarFilePath))
            throw new FileNotFoundException($"Datei nicht gefunden: {jarFilePath}");

        ushort? classVersion = null;
        
        using var archive = ZipFile.OpenRead(jarFilePath);
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = entry.Open();
                using var reader = new BinaryReader(stream);
                reader.ReadInt32(); //Magic-Header (4 Bytes)
                reader.ReadUInt16(); // Minor Version (2 Bytes)
                    
                //Read Major-Version (2 Bytes) Big-Endian-Format
                byte[] majorVersionBytes = reader.ReadBytes(2);
                Array.Reverse(majorVersionBytes); //Fix Big-Endian

                // Convert to unsigned short
                ushort majorVersion = BitConverter.ToUInt16(majorVersionBytes, 0);
                classVersion = MaxUShort(classVersion, majorVersion);
            }
        }

        return classVersion;
    }

    private static ushort MaxUShort(ushort? a, ushort b)
    {
        if (a == null)
            return b;
        
        return a > b ? a.Value : b;
    }
}