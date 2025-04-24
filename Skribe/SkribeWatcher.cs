using System.IO;
using System.Threading.Tasks;
using Skribe.Language;

namespace Skribe
{
    public class SkribeWatcher
    {
        public Task Task;
        public void WatchDirectory()
        {
            Task = Task.Run(InternalWatch);
        }

        private void InternalWatch()
        {
            var watcher = new FileSystemWatcher(Skribe.Configuration.SkribePath);
            while (Skribe.Started)
            {
                var waitForChanged = watcher.WaitForChanged(WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed);
                GlobalHooks.LoggingHook.Warning($"Change detected, reloading, functions and global variables will NOT be flushed.");
                
                LanguageLoader.Reload();
            }
        }
    }
}