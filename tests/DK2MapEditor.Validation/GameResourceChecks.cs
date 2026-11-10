using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class GameResourceChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action, string message)
    {
        var rejected = false;
        try { action(); } catch (Exception exception) when (exception is InvalidDataException or ArgumentException) { rejected = true; }
        Expect(rejected, message);
    }
    private static void U32(byte[] data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
    private static void U16(byte[] data, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DK2MapEditor.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
    public static void Run()
    {
        var root = Root(); Catalogue(root); Texts(root); Sounds(); Files(); RealResources(root);
    }
    private static void Catalogue(string root)
    {
        ushort[] expected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
            27, 28, 29, 30, 31, 32, 33, 34, 26, 35, 36, 37, 38, 39, 25, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59];
        Expect(MapTextCatalogues.Tables.Select(row => row.Identifier).SequenceEqual(expected), "Text-table IDs/order must not be inferred from list indexes.");
        var source = File.ReadAllText(Path.Combine(root, "Source", "Sources", "Datas", "Datas_Map.c"));
        var rows = Regex.Matches(source, "\\(char \\*\\)(\\d+),\\s*\"[^\"]*\",\\s*(NULL|\"[^\"]*\"),\\s*(NULL|\"[^\"]*\")");
        Expect(rows.Count == 59, "Independent source text-table count.");
        for (var i = 0; i < rows.Count; i++)
        {
            var row = MapTextCatalogues.Tables[i];
            Expect(row.CurrentFile == (rows[i].Groups[2].Value == "NULL" ? null : rows[i].Groups[2].Value.Trim('"'))
                && row.BriefingFile == (rows[i].Groups[3].Value == "NULL" ? null : rows[i].Groups[3].Value.Trim('"')), "Source STR filenames, including special briefing spellings.");
            Expect(row.Name("en").Length > 0 && row.Name("ru").Length > 0, "Both catalogue languages.");
        }
        Expect(MapTextCatalogues.Tables.Single(row => row.Identifier == 26).Russian == "Мой питомник 1"
            && MapTextCatalogues.Tables.Single(row => row.Identifier == 41).English == "Multiplayer 2", "Nonordinal family names.");
        var creatures = new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(group => LegacyCompilerRules.Elements(group)).ToArray();
        Expect(MapTextCatalogues.IntroCreatures.Select(row => row.Value).SequenceEqual(new[] { "0" }.Concat(creatures.Select(row => row.ID.ToString(System.Globalization.CultureInfo.InvariantCulture)))), "Intro replacements include source elites in order.");
        Expect(MapTextCatalogues.SpeechTextFile("speech_mentor") == "Speech.STR" && MapTextCatalogues.SpeechTextFile("speech_level6a") == "level6a.STR"
            && MapTextCatalogues.SpeechTextFile("plain") == "plain.STR", "Original corresponding speech text-file rule.");
        foreach (var unsafeName in new[] { "", " ", "..", "../LEVEL1.STR", "a\\b", "C:\\a", "x\0y", "SPEECH_" })
            Reject(() => MapTextCatalogues.SpeechTextFile(unsafeName), "Unsafe speech filename must fail.");
        foreach (var unsafeName in new[] { "", "..", "a/b", "a\\b", "D:a", "x\0y" })
            Reject(() => MapTextCatalogues.TextResourcePath(root, unsafeName), "Unsafe text filename must fail.");
    }
    private static void Texts(string root)
    {
        // Independently encode the original absolute count/index and 12-relative entry offsets.
        byte[] glyphs = [0xD4, 0xB4, 0x9B, 0x0C, 0x02, 0x0B, 0x15, 0x2D, 0xE5, 0x00];
        var data = new byte[20 + 4 + glyphs.Length]; "BFST"u8.CopyTo(data); U32(data, 4, 0x420); U32(data, 8, 2);
        U32(data, 12, 8); U32(data, 16, 8); data[20] = 1; U16(data, 21, (ushort)glyphs.Length); data[23] = 0xA7; glyphs.CopyTo(data, 24);
        var original = data.ToArray(); var resource = GameTextResource.Parse(data);
        Expect(resource.HeaderValue == 0x420 && resource.Entries.Count == 2 && resource.Entry(1).Preview == "Aaz 10...é!$00*"
            && resource.Entry(2).UnknownGlyphCount == 1 && resource.Entry(1).Reserved == 0xA7, "STR indexed preview and retained header bytes.");
        Expect(resource.DiagnosticPreview(1).EndsWith("$00*", StringComparison.Ordinal) && resource.ReleasePreview(1) == "Aaz 10...é!*"
            && resource.RawGlyphs(1).SequenceEqual(glyphs), "Configured/release unknown glyph diagnostic and exact raw data.");
        data[24] = 0; var detached = resource.RawGlyphs(1); detached[0] = 0;
        Expect(resource.Snapshot().SequenceEqual(original), "Immutable resource snapshot and copied raw data.");
        foreach (var id in new[] { -1, 0, 3, int.MaxValue }) Reject(() => resource.Entry(id), "Invalid text ID must fail.");
        for (var length = 0; length < original.Length; length++) Reject(() => GameTextResource.Parse(original.AsSpan(0, length)), "Every truncated STR fixture must fail.");
        foreach (var offset in new uint[] { 0, 4, uint.MaxValue, (uint)original.Length })
        { var bad = original.ToArray(); U32(bad, 12, offset); Reject(() => GameTextResource.Parse(bad), "Outside STR offset must fail."); }
        var malformed = original.ToArray(); U32(malformed, 8, uint.MaxValue); Reject(() => GameTextResource.Parse(malformed), "Huge STR count.");
        malformed = original.ToArray(); U16(malformed, 21, ushort.MaxValue); Reject(() => GameTextResource.Parse(malformed), "Huge STR length.");
        malformed = original.ToArray(); malformed[0] = 0; Reject(() => GameTextResource.Parse(malformed), "Unknown STR signature.");
        var empty = new byte[12]; "BFST"u8.CopyTo(empty); Expect(GameTextResource.Parse(empty).Entries.Count == 0, "Empty bounded STR.");
        // Original mapping read independently as CP1252, including accented glyphs.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var decoder = File.ReadAllText(Path.Combine(root, "Source", "Sources", "Properties", "Map", "Properties_Map_Texts.c"), Encoding.GetEncoding(1252));
        var header = File.ReadAllText(Path.Combine(root, "Source", "Includes", "Editor.h"));
        Expect(Regex.IsMatch(header, @"#define\s+DEBUG_TEXTS\s+1\b") && GameTextResource.OriginalDebugPreview, "Source-configured unknown glyph diagnostics are enabled.");
        var cases = Regex.Matches(decoder, @"case (0x[0-9A-F]+): Charset = '([^']+)';").ToDictionary(match => Convert.ToByte(match.Groups[1].Value[2..], 16), match => match.Groups[2].Value);
        cases[0x15] = "..."; cases[0xE0] = "'";
        for (var value = 0; value < 256; value++)
        {
            var expected = value is >= 0x9B and <= 0xB4 ? ((char)(0xB4 - value + 'a')).ToString()
                : value is >= 0xBB and <= 0xD4 ? ((char)(0xD4 - value + 'A')).ToString()
                : value is >= 2 and <= 10 ? ((char)(value - 2 + '1')).ToString() : cases.GetValueOrDefault((byte)value, "*");
            Expect(GameTextResource.DecodeOriginal([(byte)value], false) == expected, "All 256 original release glyph mappings.");
            var known = value is >= 0x9B and <= 0xB4 or >= 0xBB and <= 0xD4 or >= 2 and <= 10 || cases.ContainsKey((byte)value);
            Expect(GameTextResource.DecodeOriginal([(byte)value]) == (known ? expected : "$" + value.ToString("X2") + "*"), "All 256 source-configured glyph mappings.");
        }
        // Shared offsets are legal but must not multiply decoded work without a bound.
        var bomb = new byte[12 + 129 * 4 + 4 + ushort.MaxValue]; "BFST"u8.CopyTo(bomb); U32(bomb, 8, 129);
        for (var i = 0; i < 129; i++) U32(bomb, 12 + i * 4, 129 * 4);
        U16(bomb, 12 + 129 * 4 + 1, ushort.MaxValue); Reject(() => GameTextResource.Parse(bomb), "Shared-offset STR amplification bound.");
    }
    private static void Sounds()
    {
        byte[] payload = [0xFF, 0xFD, 0xA5, 0x00, 0x10];
        var data = new byte[12 + 40 + payload.Length]; U32(data, 0, 2); U32(data, 4, 12); U32(data, 8, 12);
        U32(data, 12, 40); U32(data, 16, (uint)payload.Length); Encoding.ASCII.GetBytes("sample").CopyTo(data, 20); payload.CopyTo(data, 52);
        var original = data.ToArray(); var bank = GameSpeechBank.Parse(data);
        Expect(bank.Entries.Count == 2 && bank.Entry(1).Name == "sample" && bank.Entry(2).Offset == 12 && bank.AudioPayload(2).SequenceEqual(payload), "Independent SDT absolute-offset/header/payload layout.");
        data[52] ^= 1; var copy = bank.AudioPayload(1); copy[0] ^= 1;
        Expect(bank.Snapshot().SequenceEqual(original), "Bank and extracted payload must be immutable snapshots.");
        foreach (var id in new[] { int.MinValue, -1, 0, 3, int.MaxValue }) Reject(() => bank.AudioPayload(id), "Zero/negative/out-of-range sound IDs.");
        for (var length = 0; length < original.Length; length++) Reject(() => GameSpeechBank.Parse(original.AsSpan(0, length)), "Every truncated SDT fixture must fail.");
        foreach (var offset in new uint[] { 0, 4, 8, uint.MaxValue, (uint)original.Length })
        { var bad = original.ToArray(); U32(bad, 4, offset); Reject(() => GameSpeechBank.Parse(bad), "Outside SDT offset."); }
        var malformed = original.ToArray(); U32(malformed, 0, uint.MaxValue); Reject(() => GameSpeechBank.Parse(malformed), "Huge SDT count.");
        malformed = original.ToArray(); U32(malformed, 16, uint.MaxValue); Reject(() => GameSpeechBank.Parse(malformed), "Huge SDT payload.");
        malformed = original.ToArray(); U32(malformed, 12, 38); var future = GameSpeechBank.Parse(malformed);
        Expect(!future.Entry(1).PlayableHeader, "Unknown SDT entry retained for listing."); Reject(() => future.AudioPayload(1), "Unknown header cannot be played.");
        malformed = original.ToArray(); Array.Fill(malformed, (byte)'X', 20, 32);
        Expect(GameSpeechBank.Parse(malformed).Entry(1).Name == new string('X', 32), "Unterminated SDT name must be bounded at exactly 32 bytes.");
        Expect(GameSpeechBank.Parse(new byte[4]).Entries.Count == 0, "Empty SDT.");
    }
    private static void Files()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "dk2-game-resources-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            var path = Path.Combine(temporary, "oversized.bin");
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) file.SetLength(GameSpeechBank.MaximumFileBytes + 1L);
            Reject(() => GameSpeechBank.Load(path), "Oversized file refused before allocation."); Reject(() => GameTextResource.Load(path), "Oversized STR file refused before allocation.");
            var folder = Path.Combine(temporary, "Data", "Sound", "sfx"); Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Path.Combine(folder, "speech_z")); Directory.CreateDirectory(Path.Combine(folder, "speech_a")); Directory.CreateDirectory(Path.Combine(folder, "Global"));
            var banks = MapTextCatalogues.SpeechBanks(temporary);
            Expect(banks.Select(row => row.Name).SequenceEqual(new[] { "SPEECH_A", "SPEECH_Z" }) && banks.All(row => !File.Exists(row.FilePath)),
                "Original speech-prefixed folders remain choices even when their bank is missing; no fabricated audio.");
            Expect(MapTextCatalogues.SpeechBanks(Path.Combine(temporary, "absent")).Count == 0, "Missing installation resources are an empty catalogue, not map mutation.");
        }
        finally { Directory.Delete(temporary, true); }
    }
    private static void RealResources(string root)
    {
        var game = Path.Combine(Directory.GetParent(root)!.FullName, "Dungeon Keeper 2");
        var textPath = Path.Combine(game, "Data", "Text", "Default"); var soundPath = Path.Combine(game, "Data", "Sound", "sfx");
        if (!Directory.Exists(textPath) || !Directory.Exists(soundPath)) { Console.WriteLine("      installed game resource corpus unavailable; synthetic/source checks only"); return; }
        var textCount = 0; var soundCount = 0; long entries = 0; long audioBytes = 0;
        foreach (var file in Directory.EnumerateFiles(textPath, "*.str"))
        {
            var bytes = File.ReadAllBytes(file); var before = SHA256.HashData(bytes); var text = GameTextResource.Load(file);
            Expect(text.Snapshot().SequenceEqual(bytes), "Installed STR snapshot changed.");
            var count = BitConverter.ToUInt32(bytes, 8); Expect(text.Entries.Count == count, "Installed STR original index count.");
            foreach (var entry in text.Entries)
            {
                var offset = checked(12 + (int)BitConverter.ToUInt32(bytes, 12 + (entry.Identifier - 1) * 4));
                var length = BitConverter.ToUInt16(bytes, offset + 1);
                Expect(entry.Offset == offset && entry.Length == length && text.RawGlyphs(entry.Identifier).SequenceEqual(bytes.AsSpan(offset + 4, length).ToArray()), "Installed STR exact original index/payload.");
            }
            Expect(SHA256.HashData(File.ReadAllBytes(file)).SequenceEqual(before), "Installed STR file mutated."); textCount++; entries += count;
        }
        foreach (var file in Directory.EnumerateFiles(soundPath, "*HD.SDT", SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(file); var before = SHA256.HashData(bytes); var bank = GameSpeechBank.Load(file);
            Expect(bank.Snapshot().SequenceEqual(bytes) && bank.Entries.Count == BitConverter.ToUInt32(bytes, 0), "Installed SDT snapshot/index.");
            foreach (var entry in bank.Entries)
            {
                var offset = checked((int)BitConverter.ToUInt32(bytes, 4 + (entry.Identifier - 1) * 4));
                Expect(entry.Offset == offset && entry.Length == BitConverter.ToUInt32(bytes, offset + 4), "Installed SDT independent header.");
                if (entry.PlayableHeader) Expect(bank.AudioPayload(entry.Identifier).SequenceEqual(bytes.AsSpan(offset + 40, entry.Length).ToArray()), "Installed audio payload exact; not re-encoded.");
                audioBytes += entry.Length;
            }
            Expect(SHA256.HashData(File.ReadAllBytes(file)).SequenceEqual(before), "Installed SDT file mutated."); soundCount++;
        }
        var banks = MapTextCatalogues.SpeechBanks(game);
        var expectedBanks = Directory.EnumerateDirectories(soundPath).Where(directory => (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0
            && Path.GetFileName(directory).StartsWith("speech_", StringComparison.OrdinalIgnoreCase)).Select(directory => Path.GetFileName(directory).ToUpperInvariant()).Order(StringComparer.OrdinalIgnoreCase);
        Expect(banks.Count > 0 && banks.Select(row => row.Name).SequenceEqual(expectedBanks), "Installed original speech directory selection, including missing-bank directories.");
        Console.WriteLine($"      read-only installed resources: STR={textCount} glyph entries={entries}; SDT={soundCount} audio bytes={audioBytes}; speech choices={banks.Count}; missing speech banks={banks.Count(row => !File.Exists(row.FilePath))}");
    }
}
