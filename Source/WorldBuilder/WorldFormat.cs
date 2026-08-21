using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace CMZWorldBuilder
{
    public static class WorldFormat
    {
        private const int SaveMagic = unchecked((int)0x44535452); // "RTSD" little-endian
        private const int SaveVersion = 5;
        private const uint SaveFlags = 3; // compressed + protected
        private const int MaxProtectedPayload = 64 * 1024 * 1024;
        private static readonly object SeedSync = new object();
        private static readonly Random SeedRandom = new Random();

        public static byte[] SerializeWorldInfo(WorldInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(info.Version);
                w.Write(info.TerrainVersion);
                WriteDotNetString(w, info.Name ?? "");
                WriteDotNetString(w, info.OwnerGamerTag ?? "");
                WriteDotNetString(w, info.CreatorGamerTag ?? "");
                w.Write(info.CreatedTicks);
                w.Write(info.LastPlayedTicks);
                w.Write(info.Seed);
                w.Write(info.WorldID.ToByteArray());
                w.Write(info.LastX);
                w.Write(info.LastY);
                w.Write(info.LastZ);

                // These three fields exist in CMZ's v5 world.info layout even
                // though the old World Builder did not expose them in its model.
                w.Write(0);
                w.Write(0);
                w.Write(0);

                w.Write(info.InfiniteResource);
                WriteDotNetString(w, info.ServerMessage ?? "");
                WriteDotNetString(w, info.ServerPassword ?? "");
                w.Write(info.HellBosses);
                w.Write(info.MaxHellBosses);
                w.Flush();
                return ms.ToArray();
            }
        }

        public static WorldInfo ParseWorldInfo(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader r = new BinaryReader(ms, Encoding.UTF8))
            {
                WorldInfo info = new WorldInfo();
                info.Version = r.ReadInt32();
                info.TerrainVersion = r.ReadInt32();
                info.Name = ReadDotNetString(r);
                info.OwnerGamerTag = ReadDotNetString(r);
                info.CreatorGamerTag = ReadDotNetString(r);
                info.CreatedTicks = r.ReadInt64();
                info.LastPlayedTicks = r.ReadInt64();
                info.Seed = r.ReadInt32();
                info.WorldID = new Guid(ReadExactly(r, 16));
                info.LastX = r.ReadSingle();
                info.LastY = r.ReadSingle();
                info.LastZ = r.ReadSingle();
                r.ReadInt32();
                r.ReadInt32();
                r.ReadInt32();
                info.InfiniteResource = r.ReadBoolean();
                info.ServerMessage = ReadDotNetString(r);
                info.ServerPassword = ReadDotNetString(r);
                info.HellBosses = r.ReadInt32();
                info.MaxHellBosses = r.ReadInt32();
                return info;
            }
        }

        public static byte[] ProtectSave(byte[] rawWorldInfo, string profileId)
        {
            if (rawWorldInfo == null) throw new ArgumentNullException("rawWorldInfo");
            ValidateProfileId(profileId);

            byte[] compressed = CompressPayload(rawWorldInfo);
            byte[] encrypted = EncryptPayload(compressed, profileId);
            byte[] hash;
            using (MD5 md5 = MD5.Create()) hash = md5.ComputeHash(encrypted);

            using (MemoryStream blockStream = new MemoryStream())
            using (BinaryWriter block = new BinaryWriter(blockStream))
            {
                block.Write(hash.Length);
                block.Write(hash);
                block.Write(encrypted.Length);
                block.Write(encrypted);
                block.Flush();
                byte[] protectedBlock = blockStream.ToArray();

                using (MemoryStream outputStream = new MemoryStream())
                using (BinaryWriter output = new BinaryWriter(outputStream))
                {
                    output.Write(SaveMagic);
                    output.Write(SaveVersion);
                    output.Write(SaveFlags);
                    output.Write(protectedBlock.Length);
                    output.Write(protectedBlock);
                    output.Flush();
                    return outputStream.ToArray();
                }
            }
        }

        public static byte[] UnprotectSave(byte[] envelope, string profileId)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");
            ValidateProfileId(profileId);
            using (MemoryStream ms = new MemoryStream(envelope, false))
            using (BinaryReader r = new BinaryReader(ms))
            {
                if (r.ReadInt32() != SaveMagic)
                    throw new InvalidDataException("world.info has an invalid protected-save magic value.");
                int version = r.ReadInt32();
                if (version < 3 || version > 5)
                    throw new InvalidDataException("Unsupported protected-save version: " + version + ".");
                uint flags = r.ReadUInt32();
                int blockLength = r.ReadInt32();
                if (blockLength < 0 || blockLength > MaxProtectedPayload || blockLength > ms.Length - ms.Position)
                    throw new InvalidDataException("world.info protected block length is invalid.");
                byte[] protectedBlock = ReadExactly(r, blockLength);

                byte[] payload;
                using (MemoryStream bs = new MemoryStream(protectedBlock, false))
                using (BinaryReader br = new BinaryReader(bs))
                {
                    int hashLength = br.ReadInt32();
                    if (hashLength != 16) throw new InvalidDataException("world.info MD5 length is invalid.");
                    byte[] expectedHash = ReadExactly(br, hashLength);
                    int encryptedLength = br.ReadInt32();
                    if (encryptedLength < 0 || encryptedLength > MaxProtectedPayload || encryptedLength > bs.Length - bs.Position)
                        throw new InvalidDataException("world.info encrypted payload length is invalid.");
                    byte[] encrypted = ReadExactly(br, encryptedLength);
                    byte[] actualHash;
                    using (MD5 md5 = MD5.Create()) actualHash = md5.ComputeHash(encrypted);
                    if (!ConstantEquals(expectedHash, actualHash))
                        throw new InvalidDataException("world.info integrity hash does not match.");
                    payload = (flags & 2) != 0 ? DecryptPayload(encrypted, profileId) : encrypted;
                }

                if ((flags & 1) != 0)
                    payload = DecompressPayload(payload);
                if (payload.Length > MaxProtectedPayload)
                    throw new InvalidDataException("world.info exceeds the supported protected-save size.");
                return payload;
            }
        }

        public static WorldInfo ReadWorldInfo(string path, string profileId)
        {
            return ParseWorldInfo(UnprotectSave(File.ReadAllBytes(path), profileId));
        }

        public static void ValidateWorldInfoFile(string path, string profileId, WorldInfo expected)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Staged world.info is missing.", path);
            WorldInfo actual = ReadWorldInfo(path, profileId);
            if (actual.Version != WorldBuilderInfo.WorldInfoVersion || actual.TerrainVersion != WorldBuilderInfo.TerrainVersion)
                throw new InvalidDataException("Staged world.info has an unexpected format version.");
            if (expected != null)
            {
                if (actual.WorldID != expected.WorldID) throw new InvalidDataException("Staged world ID changed unexpectedly.");
                if (!string.Equals(actual.Name, expected.Name, StringComparison.Ordinal)) throw new InvalidDataException("Staged world name did not read back correctly.");
                if (actual.Seed != expected.Seed) throw new InvalidDataException("Staged world seed did not read back correctly.");
                if (!string.Equals(actual.OwnerGamerTag, expected.OwnerGamerTag, StringComparison.Ordinal)) throw new InvalidDataException("Staged world owner did not read back correctly.");
            }
        }

        public static int ParseSeed(string text, out bool generated)
        {
            string value = (text ?? "").Trim();
            if (value.Length == 0)
            {
                generated = true;
                lock (SeedSync) return SeedRandom.Next();
            }

            generated = false;
            int seed;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seed) || seed < 0 || seed > 2147483646)
                throw new FormatException("Seed must be blank or a whole number from 0 through 2,147,483,646.");
            return seed;
        }

        public static long DotNetTicks(DateTime value)
        {
            return value.Ticks;
        }

        public static void ValidateProfileId(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || profileId.Any(c => c < '0' || c > '9'))
                throw new ArgumentException("Steam profile ID must contain digits only.");
            ulong ignored;
            if (!ulong.TryParse(profileId, NumberStyles.None, CultureInfo.InvariantCulture, out ignored))
                throw new ArgumentException("Steam profile ID is not a valid numeric profile identifier.");
        }

        private static byte[] CompressPayload(byte[] raw)
        {
            using (MemoryStream plain = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(plain, Encoding.UTF8, true))
                {
                    w.Write(raw.Length);
                    w.Write(raw);
                }
                plain.Position = 0;
                using (MemoryStream compressed = new MemoryStream())
                {
                    using (DeflateStream deflate = new DeflateStream(compressed, CompressionMode.Compress, true))
                        plain.CopyTo(deflate);
                    return compressed.ToArray();
                }
            }
        }

        private static byte[] DecompressPayload(byte[] compressed)
        {
            using (MemoryStream input = new MemoryStream(compressed, false))
            using (DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (MemoryStream plain = new MemoryStream())
            {
                deflate.CopyTo(plain);
                byte[] withLength = plain.ToArray();
                using (BinaryReader r = new BinaryReader(new MemoryStream(withLength, false)))
                {
                    int length = r.ReadInt32();
                    if (length < 0 || length > MaxProtectedPayload || length > withLength.Length - 4)
                        throw new InvalidDataException("Decompressed world.info length is invalid.");
                    return ReadExactly(r, length);
                }
            }
        }

        private static byte[] EncryptPayload(byte[] payload, string profileId)
        {
            byte[] block;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write(payload.Length);
                w.Write(payload);
                w.Flush();
                int padded = ((int)ms.Length + 15) / 16 * 16;
                block = new byte[padded];
                Buffer.BlockCopy(ms.ToArray(), 0, block, 0, (int)ms.Length);
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey(profileId);
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform transform = aes.CreateEncryptor())
                    return transform.TransformFinalBlock(block, 0, block.Length);
            }
        }

        private static byte[] DecryptPayload(byte[] encrypted, string profileId)
        {
            if (encrypted.Length == 0 || encrypted.Length % 16 != 0)
                throw new InvalidDataException("Encrypted world.info payload is not block-aligned.");
            byte[] block;
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey(profileId);
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform transform = aes.CreateDecryptor())
                    block = transform.TransformFinalBlock(encrypted, 0, encrypted.Length);
            }
            using (BinaryReader r = new BinaryReader(new MemoryStream(block, false)))
            {
                int length = r.ReadInt32();
                if (length < 0 || length > MaxProtectedPayload || length > block.Length - 4)
                    throw new InvalidDataException("Decrypted world.info payload length is invalid.");
                return ReadExactly(r, length);
            }
        }

        private static byte[] DeriveKey(string profileId)
        {
            using (MD5 md5 = MD5.Create())
                return md5.ComputeHash(Encoding.UTF8.GetBytes(profileId + "CMZ778"));
        }

        private static void WriteDotNetString(BinaryWriter w, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
            Write7BitEncodedInt(w, bytes.Length);
            w.Write(bytes);
        }

        private static string ReadDotNetString(BinaryReader r)
        {
            int length = Read7BitEncodedInt(r);
            if (length < 0 || length > 1024 * 1024) throw new InvalidDataException("world.info string length is invalid.");
            return Encoding.UTF8.GetString(ReadExactly(r, length));
        }

        private static void Write7BitEncodedInt(BinaryWriter w, int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                w.Write((byte)(v | 0x80));
                v >>= 7;
            }
            w.Write((byte)v);
        }

        private static int Read7BitEncodedInt(BinaryReader r)
        {
            int count = 0;
            int shift = 0;
            while (shift != 35)
            {
                byte b = r.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
                if ((b & 0x80) == 0) return count;
            }
            throw new FormatException("Invalid 7-bit encoded integer.");
        }

        private static byte[] ReadExactly(BinaryReader r, int count)
        {
            byte[] data = r.ReadBytes(count);
            if (data.Length != count) throw new EndOfStreamException();
            return data;
        }

        private static bool ConstantEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }

    public sealed class WorldProfileService
    {
        private readonly string _gameExePath;

        public WorldProfileService(string gameExePath)
        {
            _gameExePath = gameExePath;
        }

        public static string ResolveCastleMinerZRoot()
        {
            string overrideRoot = Environment.GetEnvironmentVariable("CMZWB_CMZ_ROOT");
            if (!string.IsNullOrWhiteSpace(overrideRoot)) return Path.GetFullPath(overrideRoot);
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CastleMinerZ");
        }

        public string CastleMinerZRoot
        {
            get { return ResolveCastleMinerZRoot(); }
        }

        public List<SteamProfile> DiscoverProfiles()
        {
            List<SteamProfile> result = new List<SteamProfile>();
            string root = CastleMinerZRoot;
            if (!Directory.Exists(root)) return result;
            foreach (string dir in Directory.GetDirectories(root))
            {
                string id = Path.GetFileName(dir);
                try { WorldFormat.ValidateProfileId(id); }
                catch { continue; }
                string worlds = Path.Combine(dir, "Worlds");
                string persona = ResolveSteamPersonaName(_gameExePath, id);
                string fromWorld = Directory.Exists(worlds) ? DetectOwner(id, worlds) : null;
                int existingCount = Directory.Exists(worlds)
                    ? Directory.GetDirectories(worlds).Count(x => !Path.GetFileName(x).StartsWith(".cmzwb-stage-", StringComparison.OrdinalIgnoreCase))
                    : 0;
                SteamProfile profile = new SteamProfile {
                    ProfileId = id,
                    WorldsDirectory = worlds,
                    OwnerGamerTag = !string.IsNullOrWhiteSpace(persona) ? persona : fromWorld,
                    ExistingWorldCount = existingCount
                };
                result.Add(profile);
            }
            return result.OrderByDescending(x => x.ExistingWorldCount).ThenBy(x => x.ProfileId).ToList();
        }

        public static string ResolveSteamPersonaName(string gameExePath, string profileId)
        {
            WorldFormat.ValidateProfileId(profileId);
            foreach (string root in CandidateSteamRoots(gameExePath))
            {
                try
                {
                    string vdf = Path.Combine(root, "config", "loginusers.vdf");
                    string name = ParsePersonaName(File.Exists(vdf) ? File.ReadAllText(vdf) : null, profileId);
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
                catch { }
            }
            return null;
        }

        public static string ParsePersonaName(string vdfText, string profileId)
        {
            if (string.IsNullOrWhiteSpace(vdfText) || string.IsNullOrWhiteSpace(profileId)) return null;
            string currentId = null;
            int currentDepth = -1;
            int depth = 0;
            using (StringReader reader = new StringReader(vdfText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string t = line.Trim();
                    if (!string.IsNullOrWhiteSpace(currentId) && t.StartsWith("\"PersonaName\"", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] pair = ParseVdfPair(t);
                        if (pair != null && string.Equals(pair[0], "PersonaName", StringComparison.OrdinalIgnoreCase))
                            return pair[1];
                    }
                    else if (t.StartsWith("\"") && t.EndsWith("\"", StringComparison.Ordinal) && t.Length >= 2 && t.IndexOf('"', 1) == t.Length - 1)
                    {
                        string keyOnly = UnquoteVdf(t);
                        if (string.Equals(keyOnly, profileId, StringComparison.Ordinal))
                        {
                            currentId = keyOnly;
                            currentDepth = depth;
                        }
                    }

                    for (int i = 0; i < t.Length; i++)
                    {
                        if (t[i] == '{') depth++;
                        else if (t[i] == '}')
                        {
                            depth--;
                            if (currentId != null && depth <= currentDepth)
                            {
                                currentId = null;
                                currentDepth = -1;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static IEnumerable<string> CandidateSteamRoots(string gameExePath)
        {
            HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DirectoryInfo current = !string.IsNullOrWhiteSpace(gameExePath)
                    ? new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(gameExePath)))
                    : null;
                while (current != null)
                {
                    if (string.Equals(current.Name, "steamapps", StringComparison.OrdinalIgnoreCase) && current.Parent != null)
                    {
                        roots.Add(current.Parent.FullName);
                        break;
                    }
                    current = current.Parent;
                }
            }
            catch { }

            foreach (RegistryView view in new [] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                    using (RegistryKey key = baseKey.OpenSubKey(@"Software\Valve\Steam"))
                    {
                        object value = key == null ? null : key.GetValue("SteamPath");
                        if (value != null && Directory.Exists(Convert.ToString(value))) roots.Add(Path.GetFullPath(Convert.ToString(value)));
                    }
                }
                catch { }
            }
            return roots;
        }

        private static string[] ParseVdfPair(string line)
        {
            List<string> values = new List<string>();
            int i = 0;
            while (i < line.Length && values.Count < 2)
            {
                while (i < line.Length && line[i] != '"') i++;
                if (i >= line.Length) break;
                int start = ++i;
                StringBuilder b = new StringBuilder();
                bool escaped = false;
                for (; i < line.Length; i++)
                {
                    char c = line[i];
                    if (escaped) { b.Append(c); escaped = false; continue; }
                    if (c == '\\') { escaped = true; continue; }
                    if (c == '"') break;
                    b.Append(c);
                }
                values.Add(b.ToString());
                i++;
            }
            return values.Count == 2 ? values.ToArray() : null;
        }

        private static string UnquoteVdf(string value)
        {
            string[] pair = ParseVdfPair(value + " \"\"");
            return pair == null ? null : pair[0];
        }

        private static string DetectOwner(string profileId, string worlds)
        {
            IEnumerable<string> infos;
            try
            {
                infos = Directory.GetDirectories(worlds)
                    .Where(x => !Path.GetFileName(x).StartsWith(".cmzwb-stage-", StringComparison.OrdinalIgnoreCase))
                    .Select(x => Path.Combine(x, "world.info"))
                    .Where(File.Exists)
                    .OrderByDescending(File.GetLastWriteTimeUtc);
            }
            catch { return null; }

            foreach (string path in infos.Take(40))
            {
                try
                {
                    WorldInfo info = WorldFormat.ReadWorldInfo(path, profileId);
                    if (!string.IsNullOrWhiteSpace(info.OwnerGamerTag)) return info.OwnerGamerTag;
                    if (!string.IsNullOrWhiteSpace(info.CreatorGamerTag)) return info.CreatorGamerTag;
                }
                catch { }
            }
            return null;
        }
    }

    public sealed class WorldCreationService
    {
        public StagedWorld Prepare(string profileId, string owner, string worldName, int seed)
        {
            WorldFormat.ValidateProfileId(profileId);
            owner = (owner ?? "").Trim();
            worldName = (worldName ?? "").Trim();
            if (owner.Length == 0) throw new ArgumentException("Owner / creator name is required.");
            if (worldName.Length == 0) throw new ArgumentException("World name is required.");
            if (worldName.Length > 128) throw new ArgumentException("World name must be 128 characters or fewer.");
            if (seed < 0 || seed > 2147483646) throw new ArgumentOutOfRangeException("seed");

            string worldsRoot = Path.Combine(WorldProfileService.ResolveCastleMinerZRoot(), profileId, "Worlds");
            Directory.CreateDirectory(worldsRoot);

            Guid worldId = Guid.NewGuid();
            Guid folderId = Guid.NewGuid();
            string folderText = folderId.ToString().ToLowerInvariant();
            string stage = Path.Combine(worldsRoot, ".cmzwb-stage-" + folderText);
            string final = Path.Combine(worldsRoot, folderText);
            if (Directory.Exists(stage) || Directory.Exists(final))
                throw new IOException("A generated world folder already exists. No files were overwritten.");

            DateTime now = DateTime.Now;
            long ticks = WorldFormat.DotNetTicks(now);
            WorldInfo info = new WorldInfo {
                Version = WorldBuilderInfo.WorldInfoVersion,
                TerrainVersion = WorldBuilderInfo.TerrainVersion,
                Name = worldName,
                OwnerGamerTag = owner,
                CreatorGamerTag = owner,
                CreatedTicks = ticks,
                LastPlayedTicks = ticks,
                Seed = seed,
                WorldID = worldId,
                LastX = 8.0f,
                LastY = 128.0f,
                LastZ = -8.0f,
                InfiniteResource = false,
                ServerMessage = owner + "'s Server",
                ServerPassword = "",
                HellBosses = 0,
                MaxHellBosses = 0
            };

            Directory.CreateDirectory(stage);
            try
            {
                byte[] raw = WorldFormat.SerializeWorldInfo(info);
                byte[] protectedBytes = WorldFormat.ProtectSave(raw, profileId);
                string infoPath = Path.Combine(stage, "world.info");
                File.WriteAllBytes(infoPath, protectedBytes);
                WorldFormat.ValidateWorldInfoFile(infoPath, profileId, info);
            }
            catch
            {
                try { Directory.Delete(stage, true); } catch { }
                throw;
            }

            return new StagedWorld {
                ProfileID = profileId,
                Owner = owner,
                WorldName = worldName,
                Seed = seed,
                WorldID = worldId,
                FolderID = folderId,
                StagePath = stage,
                FinalPath = final,
                CreatedAt = now,
                Info = info
            };
        }

        public string CreateAndCommit(string profileId, string owner, string worldName, int seed)
        {
            using (StagedWorld stage = Prepare(profileId, owner, worldName, seed))
            {
                stage.Commit();
                return stage.FinalPath;
            }
        }

        public static bool IsGameRunning()
        {
            try { return Process.GetProcessesByName("CastleMinerZ").Length > 0; }
            catch { return false; }
        }
    }
}
