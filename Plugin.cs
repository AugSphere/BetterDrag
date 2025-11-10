using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BetterDrag;

#pragma warning disable CA2243
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
#pragma warning restore CA2243
[BepInProcess("Sailwind.exe")]
internal class Plugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "com.AugSphere.BetterDrag";
    private const string PLUGIN_NAME = "BetterDrag";
    private const string PLUGIN_VERSION = "1.0.1";

    internal static new ManualLogSource? Logger;

    internal static ConfigEntry<int>? draftSamplingPeriod;
    internal static ConfigEntry<float>? globalViscousDragMultiplier;
    internal static ConfigEntry<float>? globalWaveMakingDragMultiplier;
    internal static ConfigEntry<float>? globalShipLengthMultiplier;
    internal static Dictionary<string, ShipDragPerformanceData> shipOverrides = [];
#if DEBUG
    internal static ConfigEntry<int>? debugPrintPeriod;
#endif

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);

        draftSamplingPeriod = Config.Bind(
            "--------- Physics configuration ---------",
            "draftSamplingPeriod",
            5,
            new ConfigDescription(
                "How often ship draft is sampled, in unity fixed updates: 10 means approximately every 5 frames at 40 FPS",
                new AcceptableValueRange<int>(1, 50)
            )
        );

        globalViscousDragMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            "globalViscousDragMultiplier",
            1.0f,
            new ConfigDescription(
                "Viscous drag multiplier for all ships",
                new AcceptableValueRange<float>(0.0f, 5.0f)
            )
        );

        globalWaveMakingDragMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            "globalWaveMakingDragMultiplier",
            1.0f,
            new ConfigDescription(
                "Wave-making drag multiplier for all ships",
                new AcceptableValueRange<float>(0.0f, 5.0f)
            )
        );

        globalShipLengthMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            "globalShipLengthMultiplier",
            1.0f,
            new ConfigDescription(
                "Ship length multiplier, higher values raise the maximum speed",
                new AcceptableValueRange<float>(0.1f, 5.0f)
            )
        );

#if DEBUG
        debugPrintPeriod = Config.Bind(
            "--------Ω Debug Ω--------",
            "debugPrintPeriod",
            500,
            new ConfigDescription(
                "How frequently debug data is printed to harmony.log.txt",
                new AcceptableValueRange<int>(1, 500)
            )
        );
#endif

        ManageShipConfiguration();

        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
    }

    private static void ManageShipConfiguration()
    {
        var assemblyDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
        var filePath = Path.Combine(assemblyDir.FullName, $"{PLUGIN_GUID}.shipdata.json");
        var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
        var serializer = new DataContractJsonSerializer(
            typeof(Dictionary<string, ShipDragPerformanceData>),
            settings
        );

        try
        {
            using (var fileStream = File.Open(filePath, FileMode.Open))
            {
                ShipDragConfigManager.FillUserPerformance(
                    (Dictionary<string, ShipDragPerformanceData>)serializer.ReadObject(fileStream)
                );
            }

            Logger!.LogInfo($"Read user ship configurations from {PLUGIN_GUID}.shipdata.json");
        }
        catch (Exception e) when (e is FileNotFoundException || e is SerializationException)
        {
            shipOverrides["BOAT Example 1"] = new ShipDragPerformanceData(lengthAtWaterline: 5f);
            shipOverrides["BOAT Example 2"] = new ShipDragPerformanceData(
                formFactor: 1.23f,
                waveMakingDragMultiplier: 3f
            );

            using (var stream = File.Open(filePath, FileMode.Create))
            {
                using var w = JsonReaderWriterFactory.CreateJsonWriter(
                    stream,
                    Encoding.UTF8,
                    ownsStream: true,
                    indent: true,
                    indentChars: "  "
                );
                serializer.WriteObject(w, shipOverrides);
            }

            Logger!.LogInfo($"Wrote example ship configurations to {filePath}");
        }
    }
}
