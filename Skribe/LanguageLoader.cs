using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Skribe.Language;
using Skribe.Language.Parsing;

namespace Skribe
{
    public static class LanguageLoader
    {
        public static void Reload()
        {
            Unload();
            Load();
        }

        public static void Unload()
        {
            SkribeEngine.Instance.Flush();
        }

        public static void Load()
        {
            var successes = 0;
            var failures = 0;
            var total = 0;
            List<(string, string)> failedFiles = new List<(string, string)>();
            var files = Directory.EnumerateFiles(Skribe.Configuration.SkribePath).Where(e => e.EndsWith(".skribe"));
            foreach (var file in files)
            {
                GlobalHooks.LoggingHook.Info(file);
                failedFiles.Add(LoadFile(file));
                total++;
            }
            failedFiles = failedFiles.Where(e => e.Item1 != "n" || e.Item2 != "n").ToList();
            failures = failedFiles.Count;
            successes = total - failures;
            if (failures == 0)
            {
                GlobalHooks.LoggingHook.Info($"All {total} skribes loaded successfully!");
            }
            else if (successes == 0)
            {
                GlobalHooks.LoggingHook.Error($"No skribes loaded successfully!");
            }
            else
            {
                GlobalHooks.LoggingHook.Warning("Only some skribes loaded sucessfully!");
            }
            GlobalHooks.LoggingHook.Info($"Successful: {successes}, failures: {failures}");
            if (failures == 0) return;
            foreach (var (item1, item2) in failedFiles)
            {
                GlobalHooks.LoggingHook.Warning($"Failed item: {item1}, reason: {item2}");
            }
        }

        public static (string, string) LoadFile(string name)
        {
            var engine = SkribeEngine.Instance;
            try
            {
                var script = File.ReadAllText($"{name}");
                engine.Execute(script);
            }
            catch (Exception e)
            {
                return (name, e.Message);
                
            }

            return ("n", "n");
        }
    }
}