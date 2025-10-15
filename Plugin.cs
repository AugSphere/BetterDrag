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

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
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
    internal static Dictionary<string, ShipDragPerformanceData> shipOverrides = [];

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);

        draftSamplingPeriod = Config.Bind(
            "--------- Physics configuration ---------",
            "draftSamplingPeriod",
            10,
            new ConfigDescription(
                "How often ship draft is sampled, in unity fixed updates: 10 means approximately every 5 frames at 40 FPS",
                new AcceptableValueRange<int>(1, 200)
            )
        );

        globalViscousDragMultiplier = Config.Bind(
            "--------- Global Drag Multipliers ---------",
            "globalViscousDragMultiplier",
            1.0f,
            new ConfigDescription(
                "Viscous drag multiplier for all ships",
                new AcceptableValueRange<float>(0.0f, 20.0f)
            )
        );

        globalWaveMakingDragMultiplier = Config.Bind(
            "--------- Global Drag Multipliers ---------",
            "globalWaveMakingDragMultiplier",
            1.0f,
            new ConfigDescription(
                "Wave-making drag multiplier for all ships",
                new AcceptableValueRange<float>(0.0f, 20.0f)
            )
        );

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
                ShipDragDataStore.FillUserPerformance(
                    (Dictionary<string, ShipDragPerformanceData>)serializer.ReadObject(fileStream)
                );
            }

            Logger!.LogInfo($"Read user ship configurations from {PLUGIN_GUID}.shipdata.json");
        }
        catch (Exception e) when (e is FileNotFoundException || e is SerializationException)
        {
            shipOverrides["BOAT Example 1"] = new ShipDragPerformanceData()
            {
                LengthAtWaterline = 5f,
            };
            shipOverrides["BOAT Example 2"] = new ShipDragPerformanceData()
            {
                FormFactor = 1.23f,
                WaveMakingDragMultiplier = 3f,
            };

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
