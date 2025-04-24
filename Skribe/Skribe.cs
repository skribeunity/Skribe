using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Skribe
{
    public static class Skribe
    {
        /// <summary>
        /// A boolean indicative of weather Skribe is started or not.
        /// </summary>
        public static bool Started { get; private set; } = false;
        
        public static List<Assembly> Assemblies { get; private set; } = new List<Assembly>();

        /// <summary>
        /// The current configuration for Skribe, will be null if Skribe is not started.
        /// </summary>
        public static SkribeConfiguration Configuration { get; private set; }
        
        public static SkribeWatcher Watcher { get; private set; }

        public static void Start(SkribeConfiguration configuration = null)
        {
            if (Started) return;
            if (configuration == null)
            {
                Configuration = new SkribeConfiguration();
            }
            else
            {
                Configuration = configuration;
            }
            
            // Create directory if it doesnt exist
            Directory.CreateDirectory(Path.GetFullPath(Configuration.SkribePath));
            GlobalHooks.LoggingHook.Info($"{Path.GetFullPath(Configuration.SkribePath)}");
            
            LanguageLoader.Load();
            if (Configuration.SkribeDirectoryWatch)
            {
                Watcher = new SkribeWatcher();
                Watcher.WatchDirectory();
            }

            Started = true;
        }

        public static void Stop()
        {
            if (!Started) return;
            Configuration = null;
            Started = false;
        }
    }

    public class SkribeConfiguration
    {
        public LoggingHook LoggingHook { get; set; } = new ConsoleHook();
        public string SkribePath { get; set; } = "./skribe";
        public bool SkribeDirectoryWatch { get; set; } = true;
    }

    public static class GlobalHooks
    {
        public static LoggingHook LoggingHook => Skribe.Configuration.LoggingHook;
    }
}