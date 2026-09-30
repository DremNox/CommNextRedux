using BepInEx.Configuration;
using ReduxLib.Logging;

namespace CommNext
{
    public sealed class CommNextPlugin
    {
        public const string ModGuid = "CommNextRedux";
        public const string ModName = "CommNext Redux";
        public const string ModVer = "0.1.0-preview.6";

        public static CommNextPlugin Instance { get; } = new CommNextPlugin();

        public ConfigFile Config { get; } = new ConfigFile();

        public ILogger SWLogger => CommNextRedux.CommNetBridge.Log;
    }
}
