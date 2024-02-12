using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using uSky;

namespace ValdyrSubnauticaMods
{
    [BepInPlugin(Guid, PluginName, VersionString)]
    [BepInDependency("com.snmodding.nautilus")]
    public class HarmonyPatchUnityPlugin : BaseUnityPlugin
    {
        private const string Guid = "Valdyr.TimeSpeedMod";
        private const string PluginName = "Time Speed Mod";
        private const string VersionString = "0.0.2";

        private static readonly Harmony Harmony = new Harmony(Guid);
        public static ManualLogSource Log;
        internal static Config ModConfig { get; } = OptionsPanelHandler.RegisterModOptions<Config>();

        private void Awake()
        {
            Harmony.PatchAll();

            Logger.LogInfo(PluginName + " " + VersionString + " is loaded.");
            Log = Logger;
        }
    }
}
