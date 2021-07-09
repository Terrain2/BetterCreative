using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Terrain.Packets;
using UnityEngine;

namespace BetterCreative
{
    [BepInPlugin(Guid, Name, Version), BepInDependency(LibCommonFly.Main.Guid), BepInDependency(MuckSettings.Main.Guid), BepInDependency(Terrain.Packets.Plugin.Main.Guid)]
    public class Main : BaseUnityPlugin
    {
        public const string
            Name = "BetterCreative",
            Author = "Terrain",
            Guid = Author + "." + Name,
            Version = "1.0.0.0";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;
        public static string savefile;

        public static GameObject CreativeMenu;
        public static GameObject CreativeButton;
        public static GameObject CreativeCell;
        public static InventoryItem Precision;

        public static bool dontDestroy;
        public static OffroadPackets packets;

        public static ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "creative.cfg"), true);
        public static ConfigEntry<KeyCode> noclip = config.Bind<KeyCode>("Creative", "noclip", KeyCode.N, "Disable collision while flying.");
        public static ConfigEntry<KeyCode> precisionTriggers = config.Bind<KeyCode>("Creative", "precision-trigger", KeyCode.LeftAlt, "Hold to mark triggers with precision delete.");

        Main()
        {
            log = Logger;
            harmony = new Harmony(Guid);
            assembly = Assembly.GetExecutingAssembly();
            modFolder = Path.GetDirectoryName(assembly.Location);
            savefile = Path.Combine(modFolder, "binds");
            packets = OffroadPackets.Register<Packets>();

            var bundle = GetAssetBundle("creative");

            CreativeMenu = bundle.LoadAsset<GameObject>("Assets/PrefabInstance/CreativeMenu.prefab");
            CreativeButton = bundle.LoadAsset<GameObject>("Assets/PrefabInstance/CreativeButton.prefab");
            CreativeCell = bundle.LoadAsset<GameObject>("Assets/PrefabInstance/CreativeCell.prefab");
            Precision = bundle.LoadAsset<InventoryItem>("Assets/ScriptableObject/Items/Precision.asset");

            CreativeCell.AddComponent<CreativeCell>();
            CreativeMenu.AddComponent<CreativeUI>();
            harmony.PatchAll(assembly);
        }

        static readonly OSPlatform[] supportedPlatforms = new[] { OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX };

        static AssetBundle GetAssetBundle(string name)
        {
            foreach (var platform in supportedPlatforms) {
                if (RuntimeInformation.IsOSPlatform(platform)) {
                    name = $"{name}-{platform.ToString().ToLower()}";
                    goto load;
                }
            }

            throw new PlatformNotSupportedException("Unsupported platform, cannot load AssetBundles");

            load:
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(name));

            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }
    }

    [OffroadPacket(Main.Guid)]
    public class Packets
    {
        [OffroadPacket]
        public static void DontDestroyNeighbors(BinaryReader reader)
        {
            Main.dontDestroy = reader.ReadBoolean();
            if (Main.dontDestroy)
            {
                ChatBox.Instance.AppendMessage(-1, "<color=#018786>Breaking structures will no longer destroy their neighbors<color=white>", "");
            }
            else
            {
                ChatBox.Instance.AppendMessage(-1, "<color=#018786>Breaking structures will now destroy their neighbors<color=white>", "");
            }
        }

        public static void DontDestroyNeighbors(bool value)
        {
            using (Main.packets.WriteToAll(nameof(DontDestroyNeighbors), out var writer))
            {
                writer.Write(value);
            }
        }
    }
}