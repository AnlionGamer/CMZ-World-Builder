using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CMZEcosystem
{
    public sealed class ResolvedPackageInput : IDisposable
    {
        public string PackagePath { get; internal set; }
        public string TemporaryRoot { get; internal set; }
        public bool WasArchiveWrapped { get; internal set; }
        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(TemporaryRoot))
            {
                try { if (Directory.Exists(TemporaryRoot)) Directory.Delete(TemporaryRoot, true); } catch { }
            }
        }
    }

    public static class ArchiveDiscovery
    {
        public const int MaxNestedArchiveDepth = 2;
        public const long MaxArchiveBytes = 2147483648L;
        public const long MaxExtractedBytes = 2147483648L;
        public const int MaxExtractedFiles = 20000;

        private static readonly string[] ContainerExtensions = new [] { ".zip", ".7z", ".rar" };

        public static bool IsArchiveContainer(string path)
        {
            string ext = Path.GetExtension(path ?? "");
            return ContainerExtensions.Any(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
        }

        public static ResolvedPackageInput ResolvePackage(string selectedPath, string directExtension, string rootMarker, string tempParent)
        {
            if (string.IsNullOrWhiteSpace(selectedPath)) throw new ArgumentException("No package source was selected.");
            selectedPath = Path.GetFullPath(selectedPath);
            if (File.Exists(selectedPath) && string.Equals(Path.GetExtension(selectedPath), directExtension, StringComparison.OrdinalIgnoreCase))
                return new ResolvedPackageInput { PackagePath = selectedPath };
            if (!File.Exists(selectedPath)) throw new FileNotFoundException("The selected package source does not exist.", selectedPath);
            if (!IsArchiveContainer(selectedPath))
                throw new InvalidDataException("Select a " + directExtension + " package or a supported .zip, .7z, or .rar archive containing one.");

            string temp = CreateTemp(tempParent, "archive-package-");
            try
            {
                string package = ResolveFromArchive(selectedPath, directExtension, rootMarker, temp, 0);
                return new ResolvedPackageInput { PackagePath = package, TemporaryRoot = temp, WasArchiveWrapped = true };
            }
            catch
            {
                try { Directory.Delete(temp, true); } catch { }
                throw;
            }
        }

        public static string ExtractArchiveForInspection(string archivePath, string tempParent)
        {
            archivePath = Path.GetFullPath(archivePath);
            if (!File.Exists(archivePath)) throw new FileNotFoundException("Archive was not found.", archivePath);
            if (!IsArchiveContainer(archivePath)) throw new InvalidDataException("Supported recovery archives are .zip, .7z, and .rar.");
            string temp = CreateTemp(tempParent, "archive-recovery-");
            try
            {
                ExtractSafe(archivePath, temp);
                return temp;
            }
            catch
            {
                try { Directory.Delete(temp, true); } catch { }
                throw;
            }
        }

        private static string ResolveFromArchive(string archivePath, string directExtension, string rootMarker, string temp, int depth)
        {
            if (depth > MaxNestedArchiveDepth) throw new InvalidDataException("The selected archive contains too many nested archive layers.");
            string layer = Path.Combine(temp, "layer-" + depth + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(layer);
            ExtractSafe(archivePath, layer);

            List<string> direct = Directory.GetFiles(layer, "*" + directExtension, SearchOption.AllDirectories)
                .Where(x => FolderDepth(layer, Path.GetDirectoryName(x)) <= 6)
                .ToList();
            if (direct.Count == 1) return direct[0];
            if (direct.Count > 1) throw new InvalidDataException("Multiple " + directExtension + " packages were found in the selected archive. Choose an archive containing exactly one package.");

            List<string> roots = Directory.GetFiles(layer, rootMarker, SearchOption.AllDirectories)
                .Where(x => FolderDepth(layer, Path.GetDirectoryName(x)) <= 6)
                .Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (roots.Count == 1)
            {
                string repacked = Path.Combine(temp, "resolved" + directExtension);
                if (File.Exists(repacked)) File.Delete(repacked);
                ZipFile.CreateFromDirectory(roots[0], repacked, CompressionLevel.Optimal, false);
                return repacked;
            }
            if (roots.Count > 1) throw new InvalidDataException("Multiple package roots were found in the selected archive. Choose a more specific archive.");

            List<string> nested = Directory.GetFiles(layer, "*", SearchOption.AllDirectories)
                .Where(IsArchiveContainer)
                .Where(x => FolderDepth(layer, Path.GetDirectoryName(x)) <= 6)
                .ToList();
            if (nested.Count == 1 && depth < MaxNestedArchiveDepth)
                return ResolveFromArchive(nested[0], directExtension, rootMarker, temp, depth + 1);
            if (nested.Count > 1)
                throw new InvalidDataException("Multiple nested archives were found and no unique CastleMiner Z package could be selected automatically.");
            throw new InvalidDataException("No compatible " + directExtension + " package was found in the selected archive.");
        }

        public static void ExtractSafe(string archivePath, string destination)
        {
            FileInfo fi = new FileInfo(archivePath);
            if (fi.Length > MaxArchiveBytes) throw new InvalidDataException("The selected archive is too large for automatic discovery.");
            if (string.Equals(Path.GetExtension(archivePath), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                ExtractZipSafe(archivePath, destination);
                return;
            }

            string tar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
            if (!File.Exists(tar)) throw new NotSupportedException("Windows archive support (tar.exe) is required for .7z/.rar discovery but was not found on this system.");

            string listing = Run(tar, "-tf " + Quote(archivePath), null);
            string[] entries = listing.Replace("\r", "").Split(new [] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length > MaxExtractedFiles) throw new InvalidDataException("The selected archive contains too many entries.");
            foreach (string raw in entries)
            {
                string entry = raw.Trim();
                if (entry.Length == 0) continue;
                ValidateEntryPath(entry);
            }

            Directory.CreateDirectory(destination);
            Run(tar, "-xf " + Quote(archivePath) + " -C " + Quote(destination), null);
            long total = 0; int count = 0;
            foreach (string d in Directory.GetDirectories(destination, "*", SearchOption.AllDirectories))
            {
                if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Archive extraction produced a link/reparse-point directory, which is not allowed.");
            }
            foreach (string f in Directory.GetFiles(destination, "*", SearchOption.AllDirectories))
            {
                if ((File.GetAttributes(f) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Archive extraction produced a link/reparse-point file, which is not allowed.");
                count++; if (count > MaxExtractedFiles) throw new InvalidDataException("Archive extraction exceeded the file-count limit.");
                FileInfo outInfo = new FileInfo(f); total += outInfo.Length;
                if (total > MaxExtractedBytes) throw new InvalidDataException("Archive extraction exceeded the size limit.");
                string full = Path.GetFullPath(f);
                string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Archive attempted to write outside its staging directory.");
            }
        }

        private static void ExtractZipSafe(string archivePath, string destination)
        {
            Directory.CreateDirectory(destination);
            string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            long total = 0;
            int count = 0;
            HashSet<string> outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                if (archive.Entries.Count > MaxExtractedFiles) throw new InvalidDataException("The selected archive contains too many entries.");
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string rel = (entry.FullName ?? "").Replace('\\','/');
                    if (rel.Length == 0) continue;
                    ValidateEntryPath(rel);
                    if (IsZipSymlink(entry)) throw new InvalidDataException("Archive contains a symbolic-link entry: " + rel);
                    bool directory = rel.EndsWith("/", StringComparison.Ordinal);
                    string full = Path.GetFullPath(Path.Combine(destination, rel.Replace('/', Path.DirectorySeparatorChar)));
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Archive attempted to write outside its staging directory.");
                    if (directory) { Directory.CreateDirectory(full); continue; }
                    if (!outputs.Add(full)) throw new InvalidDataException("Archive contains a duplicate output path: " + rel);
                    total += entry.Length;
                    count++;
                    if (count > MaxExtractedFiles) throw new InvalidDataException("Archive extraction exceeded the file-count limit.");
                    if (total > MaxExtractedBytes) throw new InvalidDataException("Archive extraction exceeded the size limit.");
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    using (Stream input = entry.Open())
                    using (FileStream output = File.Create(full)) input.CopyTo(output);
                }
            }
        }

        private static bool IsZipSymlink(ZipArchiveEntry entry)
        {
            int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
            return unixType == 0xA000;
        }

        private static void ValidateEntryPath(string entry)
        {
            string n = entry.Replace('\\','/');
            if (n.StartsWith("/", StringComparison.Ordinal) || n.StartsWith("~", StringComparison.Ordinal) || n.Contains(":"))
                throw new InvalidDataException("Archive contains an unsafe absolute path: " + entry);
            string[] parts = n.Split('/');
            foreach (string p in parts)
            {
                string windowsNormalized = (p ?? "").TrimEnd(' ', '.');
                if (p == ".." || windowsNormalized == ".." || windowsNormalized == ".")
                    throw new InvalidDataException("Archive contains an unsafe parent-directory path: " + entry);
            }
        }

        private static string Run(string exe, string args, string working)
        {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = exe, Arguments = args, UseShellExecute = false,
                CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = string.IsNullOrWhiteSpace(working) ? Environment.CurrentDirectory : working
            };
            using (Process p = Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) throw new InvalidDataException("The archive could not be read by Windows archive support. " + (string.IsNullOrWhiteSpace(stderr) ? "Exit code " + p.ExitCode + "." : stderr.Trim()));
                return stdout;
            }
        }

        private static string Quote(string s) { return "\"" + (s ?? "").Replace("\"", "\\\"") + "\""; }
        private static string CreateTemp(string parent, string prefix)
        {
            if (string.IsNullOrWhiteSpace(parent)) parent = Path.GetTempPath();
            Directory.CreateDirectory(parent);
            string p = Path.Combine(parent, prefix + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); return p;
        }
        private static int FolderDepth(string root, string path)
        {
            string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string p = Path.GetFullPath(path ?? root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!p.StartsWith(r, StringComparison.OrdinalIgnoreCase)) return int.MaxValue;
            string rel = p.Substring(r.Length).Trim(Path.DirectorySeparatorChar);
            return rel.Length == 0 ? 0 : rel.Split(Path.DirectorySeparatorChar).Length;
        }
    }
}
