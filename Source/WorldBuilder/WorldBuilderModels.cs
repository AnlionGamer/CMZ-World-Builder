using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace CMZWorldBuilder
{
    public static class WorldBuilderInfo
    {
        public const string Name = "CastleMiner Z World Builder";
        public const string Version = "1.0.4";
        public const string SupportedGameVersion = "1.9.9.8";
        public const int WorldInfoVersion = 5;
        public const int TerrainVersion = 1;
        public const int ProtocolVersion = 1;
    }

    public static class WorldBuilderManagerSettings
    {
        public static string ReadLanguageCode(WorldBuilderPaths paths)
        {
            if (paths == null || string.IsNullOrWhiteSpace(paths.ManagerSettings) || !File.Exists(paths.ManagerSettings)) return null;
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string,object> settings = js.Deserialize<Dictionary<string,object>>(File.ReadAllText(paths.ManagerSettings));
                if (settings == null) return null;
                object value;
                return settings.TryGetValue("languageCode", out value) ? Convert.ToString(value) : null;
            }
            catch { return null; }
        }
    }

    public sealed class WorldBuilderPaths
    {
        public string BaseDirectory { get; private set; }
        public string DataRoot { get; private set; }
        public string Logs { get; private set; }
        public string ScenarioJobs { get; private set; }
        public string CrashReports { get; private set; }
        public string Scenarios { get; private set; }
        public string ManagerDataRoot { get; private set; }
        public string ManagerSettings { get; private set; }
        public string ManagerCustomThemes { get; private set; }

        public WorldBuilderPaths()
            : this(null, null)
        {
        }

        public WorldBuilderPaths(string managerDataRootOverride)
            : this(managerDataRootOverride, null)
        {
        }

        // The legacy override exists so the regression harness can prove migration
        // without touching a real user's pre-RC World Builder data folder.
        public WorldBuilderPaths(string managerDataRootOverride, string legacyDataRootOverride)
        {
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string defaultManagerRoot = Path.Combine(localAppData, "CastleMinerZ", "CMZModManager");
            string managerEnvironmentOverride = Environment.GetEnvironmentVariable("CMZMM_DATA_ROOT");
            ManagerDataRoot = !string.IsNullOrWhiteSpace(managerDataRootOverride)
                ? Path.GetFullPath(managerDataRootOverride)
                : !string.IsNullOrWhiteSpace(managerEnvironmentOverride)
                    ? Path.GetFullPath(managerEnvironmentOverride)
                    : defaultManagerRoot;

            string dataOverride = Environment.GetEnvironmentVariable("CMZWB_DATA_ROOT");
            DataRoot = !string.IsNullOrWhiteSpace(dataOverride)
                ? Path.GetFullPath(dataOverride)
                : Path.Combine(ManagerDataRoot, "ToolData", "cmz.worldbuilder");

            // Standalone v1.0.4 uses the generic ToolData contract. Preserve all
            // previous World Builder user data by copying without overwrite; never
            // delete or move the previous integrated/legacy data as part of migration.
            // This also runs when --data-root/CMZWB_DATA_ROOT is supplied by the Manager;
            // otherwise the normal Manager launch path would accidentally skip migration.
            string integratedLegacy = Path.Combine(ManagerDataRoot, "WorldBuilder");
            MigrateLegacyDataRoot(integratedLegacy, DataRoot);

            string legacyRoot = !string.IsNullOrWhiteSpace(legacyDataRootOverride)
                ? Path.GetFullPath(legacyDataRootOverride)
                : Path.Combine(localAppData, "CastleMinerZWorldBuilder");
            MigrateLegacyDataRoot(legacyRoot, DataRoot);

            Logs = Path.Combine(DataRoot, "Logs");
            ScenarioJobs = Path.Combine(DataRoot, "ScenarioJobs");
            CrashReports = Path.Combine(DataRoot, "CrashReports");
            Scenarios = Path.Combine(DataRoot, "Scenarios");
            ManagerSettings = Path.Combine(ManagerDataRoot, "ManagerSettings.json");
            ManagerCustomThemes = Path.Combine(ManagerDataRoot, "Themes");

            foreach (string dir in new [] { ManagerDataRoot, DataRoot, Logs, ScenarioJobs, CrashReports, Scenarios })
                Directory.CreateDirectory(dir);
        }

        private static void MigrateLegacyDataRoot(string legacyRoot, string destinationRoot)
        {
            if (string.IsNullOrWhiteSpace(legacyRoot) || !Directory.Exists(legacyRoot)) return;

            string legacyFull = Path.GetFullPath(legacyRoot).TrimEnd(Path.DirectorySeparatorChar);
            string destinationFull = Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar);
            if (string.Equals(legacyFull, destinationFull, StringComparison.OrdinalIgnoreCase)) return;

            CopyDirectoryWithoutOverwrite(legacyFull, destinationFull);
        }

        private static void CopyDirectoryWithoutOverwrite(string sourceRoot, string destinationRoot)
        {
            Directory.CreateDirectory(destinationRoot);
            foreach (string directory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destinationRoot, relative));
            }
            foreach (string file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                string destination = Path.Combine(destinationRoot, relative);
                string parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(parent)) Directory.CreateDirectory(parent);
                if (!File.Exists(destination)) File.Copy(file, destination, false);
            }
        }

        public string MainLog
        {
            get { return Path.Combine(Logs, "WorldBuilder.log"); }
        }

        public string BundledThemesDirectory
        {
            get { return Path.Combine(BaseDirectory, "Themes"); }
        }

        public string DocumentationDirectory
        {
            get { return Path.Combine(BaseDirectory, "Documentation"); }
        }

        public string BundledLanguagesDirectory
        {
            get { return Path.Combine(BaseDirectory, "Languages"); }
        }
    }

    public sealed class WorldBuilderJson
    {
        private readonly JavaScriptSerializer _js = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
        public T Read<T>(string path) { return _js.Deserialize<T>(File.ReadAllText(path)); }
        public T ReadShared<T>(string path)
        {
            // progress.json is replaced atomically by scenario generators. On Windows,
            // a normal File.ReadAllText handle blocks delete/rename while it is open.
            // Share delete/write so the generator can replace progress.json even while
            // the World Builder is polling it.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream))
                return _js.Deserialize<T>(reader.ReadToEnd());
        }
        public T Deserialize<T>(string text) { return _js.Deserialize<T>(text); }
        public Dictionary<string,object> Dictionary(string text) { return _js.Deserialize<Dictionary<string,object>>(text); }
        public string Serialize(object value) { return _js.Serialize(value); }
        public void Write(string path, object value)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string temp = path + ".tmp";
            File.WriteAllText(temp, _js.Serialize(value));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }

    public sealed class WorldInfo
    {
        public int Version { get; set; }
        public int TerrainVersion { get; set; }
        public string Name { get; set; }
        public string OwnerGamerTag { get; set; }
        public string CreatorGamerTag { get; set; }
        public long CreatedTicks { get; set; }
        public long LastPlayedTicks { get; set; }
        public int Seed { get; set; }
        public Guid WorldID { get; set; }
        public float LastX { get; set; }
        public float LastY { get; set; }
        public float LastZ { get; set; }
        public bool InfiniteResource { get; set; }
        public string ServerMessage { get; set; }
        public string ServerPassword { get; set; }
        public int HellBosses { get; set; }
        public int MaxHellBosses { get; set; }
    }

    public sealed class SteamProfile
    {
        public string ProfileId { get; set; }
        public string OwnerGamerTag { get; set; }
        public string WorldsDirectory { get; set; }
        public int ExistingWorldCount { get; set; }
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(OwnerGamerTag)
                ? "Steam profile"
                : OwnerGamerTag;
        }
    }

    public sealed class StagedWorld : IDisposable
    {
        public string ProfileID { get; set; }
        public string Owner { get; set; }
        public string WorldName { get; set; }
        public int Seed { get; set; }
        public Guid WorldID { get; set; }
        public Guid FolderID { get; set; }
        public string StagePath { get; set; }
        public string FinalPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public WorldInfo Info { get; set; }
        public bool Committed { get; private set; }

        public void Commit()
        {
            if (Committed) return;
            if (Directory.Exists(FinalPath))
                throw new IOException("The destination world folder already exists. No files were overwritten.");
            Directory.Move(StagePath, FinalPath);
            Committed = true;
        }

        public void Dispose()
        {
            if (!Committed && !string.IsNullOrWhiteSpace(StagePath))
            {
                try { if (Directory.Exists(StagePath)) Directory.Delete(StagePath, true); }
                catch { }
            }
        }
    }

    public sealed class ScenarioManifest
    {
        public int protocolVersion { get; set; }
        public string scenarioId { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string minimumBuilderVersion { get; set; }
        public string author { get; set; }
        public string description { get; set; }
        public string outputCompatibility { get; set; }
        public string generator { get; set; }
        public string optionsFile { get; set; }
        public string documentation { get; set; }
        public Dictionary<string,string> files { get; set; }
        public string installedRoot { get; set; }
        public bool isBundled { get; set; }
        public override string ToString() { return (name ?? scenarioId) + " " + version; }
    }

    public sealed class ScenarioOptionsDocument
    {
        public int schemaVersion { get; set; }
        public ScenarioOptionSection[] sections { get; set; }
    }

    public sealed class ScenarioOptionSection
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public ScenarioOptionDefinition[] options { get; set; }
    }

    public sealed class ScenarioOptionDefinition
    {
        public string id { get; set; }
        public string type { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public object @default { get; set; }
        public bool required { get; set; }
        public ScenarioVisibleWhen visibleWhen { get; set; }
        public string warning { get; set; }
        public double? minimum { get; set; }
        public double? maximum { get; set; }
        public double? step { get; set; }
        public int? minLength { get; set; }
        public int? maxLength { get; set; }
        public ScenarioChoice[] choices { get; set; }
    }

    public sealed class ScenarioVisibleWhen
    {
        public string optionId { get; set; }
        public object equals { get; set; }
    }

    public sealed class ScenarioChoice
    {
        public string value { get; set; }
        public string label { get; set; }
        public override string ToString() { return string.IsNullOrWhiteSpace(label) ? value : label; }
    }

    public sealed class ScenarioJob
    {
        public int protocolVersion { get; set; }
        public string builderVersion { get; set; }
        public string jobId { get; set; }
        public string scenarioId { get; set; }
        public string scenarioVersion { get; set; }
        public string worldPath { get; set; }
        public string worldName { get; set; }
        public int seed { get; set; }
        public string steamProfileId { get; set; }
        public string ownerGamerTag { get; set; }
        public Dictionary<string,object> options { get; set; }
        public string progressPath { get; set; }
        public string resultPath { get; set; }
        public string logPath { get; set; }
        public string cancelPath { get; set; }
        public string createdAt { get; set; }
    }

    public sealed class ScenarioProgress
    {
        public int protocolVersion { get; set; }
        public string stage { get; set; }
        public string message { get; set; }
        public double percent { get; set; }
        public string updatedAt { get; set; }
    }

    public sealed class ScenarioResult
    {
        public int protocolVersion { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
        public string errorCode { get; set; }
        public string[] warnings { get; set; }
        public Dictionary<string,object> statistics { get; set; }
        public string[] filesChanged { get; set; }
        public string completedAt { get; set; }
    }

    public sealed class ThemeDefinition
    {
        public int themeFormat { get; set; }
        public string id { get; set; }
        public string displayName { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string baseMode { get; set; }
        public string description { get; set; }
        public Dictionary<string,string> colors { get; set; }
        public ThemeEffects effects { get; set; }
        public bool adaptive { get; set; }
        public string lightThemeId { get; set; }
        public string darkThemeId { get; set; }
    }

    public sealed class ThemeEffects
    {
        public double cornerRadius { get; set; }
        public bool accentGlow { get; set; }
        public bool subtleTexture { get; set; }
    }
}
