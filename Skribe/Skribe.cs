namespace Skribe
{
    public static class Skribe
    {
        /// <summary>
        /// A boolean indicative of weather Skribe is started or not.
        /// </summary>
        public static bool Started { get; private set; } = false;

        /// <summary>
        /// The current configuration for Skribe, will be null if Skribe is not started.
        /// </summary>
        public static SkribeConfiguration Configuration { get; private set; }

        public static void Start(SkribeConfiguration configuration)
        {
            if (Started) return;
        }

        public static void Stop()
        {
            if (!Started) return;
        }
    }

    public class SkribeConfiguration
    {
        public LoggingHook LoggingHook { get; set; } = new ConsoleHook();
        public string SkribePath { get; set; } = "./skribe";
    }
}