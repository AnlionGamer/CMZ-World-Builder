using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CMZLocalization;

namespace CMZWorldBuilder
{
    public static class WorldBuilderStandaloneProgram
    {
        private static Mutex _mutex;

        [STAThread]
        public static int Main(string[] args)
        {
            bool created;
            _mutex = new Mutex(true, @"Local\CMZWorldBuilder.SingleInstance", out created);
            if (!created)
            {
                MessageBox.Show("CastleMiner Z World Builder is already running.", WorldBuilderInfo.Name,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _mutex.Dispose();
                _mutex = null;
                return 2;
            }

            try
            {
                Dictionary<string,string> options = ParseArguments(args);
                string managerRoot = GetOption(options, "--manager-data-root");
                string dataRoot = GetOption(options, "--data-root");
                if (!string.IsNullOrWhiteSpace(managerRoot)) Environment.SetEnvironmentVariable("CMZMM_DATA_ROOT", managerRoot);
                if (!string.IsNullOrWhiteSpace(dataRoot)) Environment.SetEnvironmentVariable("CMZWB_DATA_ROOT", dataRoot);

                WorldBuilderPaths paths = new WorldBuilderPaths(managerRoot);
                string language = GetOption(options, "--language");
                if (string.IsNullOrWhiteSpace(language)) language = WorldBuilderManagerSettings.ReadLanguageCode(paths);
                if (string.IsNullOrWhiteSpace(language)) language = SupportedLanguages.DetectCurrent();
                LocalizationManager localization = new LocalizationManager(paths.BundledLanguagesDirectory, language);
                LocalizationHub.Current = localization;

                string gameExe = GetOption(options, "--game-path");
                if (string.IsNullOrWhiteSpace(gameExe)) gameExe = ReadManagerGameExecutable(paths.ManagerSettings);
                string themeId = ReadManagerString(paths.ManagerSettings, "themeId") ?? "builtin.dark";

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                WorldBuilderTheme.Apply(app, paths.BundledThemesDirectory, themeId);
                WorldBuilderStandaloneWindow window = new WorldBuilderStandaloneWindow(paths, gameExe);
                app.MainWindow = window;
                return app.Run(window);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "CastleMiner Z World Builder startup failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            finally
            {
                if (_mutex != null)
                {
                    try { _mutex.ReleaseMutex(); } catch { }
                    _mutex.Dispose();
                }
            }
        }

        private static Dictionary<string,string> ParseArguments(string[] args)
        {
            Dictionary<string,string> result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; args != null && i < args.Length; i++)
            {
                string key = args[i];
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("--", StringComparison.Ordinal)) continue;
                string value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "";
                result[key] = value;
            }
            return result;
        }

        private static string GetOption(Dictionary<string,string> options, string key)
        {
            string value;
            return options != null && options.TryGetValue(key, out value) ? value : null;
        }

        private static string ReadManagerGameExecutable(string settingsPath)
        {
            string install = ReadManagerString(settingsPath, "gameInstallPath");
            if (string.IsNullOrWhiteSpace(install)) return null;
            try
            {
                string full = Path.GetFullPath(install);
                if (File.Exists(full) && string.Equals(Path.GetFileName(full), "CastleMinerZ.exe", StringComparison.OrdinalIgnoreCase)) return full;
                return Path.Combine(full, "CastleMinerZ.exe");
            }
            catch { return null; }
        }

        private static string ReadManagerString(string settingsPath, string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settingsPath) || !File.Exists(settingsPath)) return null;
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string,object> data = js.Deserialize<Dictionary<string,object>>(File.ReadAllText(settingsPath));
                object value;
                return data != null && data.TryGetValue(key, out value) ? Convert.ToString(value) : null;
            }
            catch { return null; }
        }
    }

    public sealed class WorldBuilderStandaloneWindow : Window
    {
        public WorldBuilderStandaloneWindow(WorldBuilderPaths paths, string gameExePath)
        {
            Title = WorldBuilderInfo.Name + " — " + WorldBuilderInfo.Version;
            Width = 1180;
            Height = 760;
            MinWidth = 960;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SetResourceReference(BackgroundProperty, "WindowBackground");
            TrySetIcon();

            Border shell = new Border { Margin = new Thickness(18) };
            shell.Child = new WorldBuilderWorkspace(paths, gameExePath, this, null);
            Content = shell;
        }

        private void TrySetIcon()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "CMZWorldBuilder.ico");
                if (File.Exists(path)) Icon = new BitmapImage(new Uri(path));
            }
            catch { }
        }
    }
}
