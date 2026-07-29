using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using BetterDrag.ShipConfiguration;

using HarmonyLib;

namespace BetterDrag;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("Sailwind.exe")]
internal class Plugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "com.AugSphere.BetterDrag";
    private const string PLUGIN_NAME = "BetterDrag";
    private const string PLUGIN_VERSION = "1.4.0";

    internal static new ManualLogSource? Logger;

    internal static ConfigEntry<float>? GlobalViscousDragMultiplier;
    internal static ConfigEntry<float>? GlobalWaveMakingDragMultiplier;
    internal static ConfigEntry<float>? GlobalShipLengthMultiplier;
    internal static ConfigEntry<float>? GlobalBuoyancyMultiplier;
    internal static ConfigEntry<float>? GlobalMassMultiplier;
    internal static ConfigEntry<float>? GlobalOffAxisDragMultiplier;
    internal static ConfigEntry<bool>? EnableDuringSleep;
    internal static ConfigEntry<bool>? EnableForceSmoothing;
    internal static Dictionary<string, ShipDragPerformanceData> ShipOverrides = [];
#if DEBUG
    internal static ConfigEntry<int>? DebugPrintPeriod;
#endif

    internal void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);

        GlobalViscousDragMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalViscousDragMultiplier),
            1.0f,
            new ConfigDescription(
                "Viscous drag multiplier. Relevant at all speeds. Higher values make ships slower.",
                new AcceptableValueRange<float>(0.0f, 5.0f)
            )
        );

        GlobalWaveMakingDragMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalWaveMakingDragMultiplier),
            1.0f,
            new ConfigDescription(
                "Wave-making drag multiplier. Mostly matters at high speeds. Higher values make ships slower.",
                new AcceptableValueRange<float>(0.0f, 5.0f)
            )
        );

        GlobalShipLengthMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalShipLengthMultiplier),
            1.0f,
            new ConfigDescription(
                "Ship length multiplier. Higher values raise the maximum speed.",
                new AcceptableValueRange<float>(0.1f, 5.0f)
            )
        );

        GlobalBuoyancyMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalBuoyancyMultiplier),
            1.0f,
            new ConfigDescription(
                "Buoyancy multiplier. Higher values make ships sit higher in the water.",
                new AcceptableValueRange<float>(0.1f, 5.0f)
            )
        );

        GlobalMassMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalMassMultiplier),
            1.0f,
            new ConfigDescription(
                "Mass multiplier. Higher values result in heavier ship hulls.",
                new AcceptableValueRange<float>(0.1f, 5.0f)
            )
        );

        GlobalOffAxisDragMultiplier = Config.Bind(
            "--------- Global Multipliers ---------",
            nameof(GlobalOffAxisDragMultiplier),
            150f,
            new ConfigDescription(
                "Viscous drag multiplier for vertical and lateral movement. Higher values make ships less prone to drifting sideways.",
                new AcceptableValueRange<float>(50f, 250.0f)
            )
        );

        EnableDuringSleep = Config.Bind(
            "--------- Misc ---------",
            nameof(EnableDuringSleep),
            true,
            new ConfigDescription(
                "Keep mod physics on during sleep. Set to false in case of ships being thrown around while sleeping."
            )
        );

        EnableForceSmoothing = Config.Bind(
            "--------- Misc ---------",
            nameof(EnableForceSmoothing),
            false,
            new ConfigDescription(
                "Smooth forces on the ship. Reduces the small vibrations of the ship, but can create unrealistic slow oscillations."
            )
        );

#if DEBUG
        DebugPrintPeriod = Config.Bind(
            "--------Ω Debug Ω--------",
            nameof(DebugPrintPeriod),
            500,
            new ConfigDescription(
                "How frequently debug data is printed to harmony.log.txt.",
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
        catch (Exception e)
            when (e is FileNotFoundException
                || e is SerializationException
                || e.InnerException is XmlException
            )
        {
            if (e is FileNotFoundException)
            {
                Logger!.LogWarning($"No configuration file found at {filePath}");
            }
            else
            {
                Logger!.LogError($"Invalid JSON formatting of {filePath}");
            }

            ShipOverrides["BOAT Example 1"] = new ShipDragPerformanceData(lengthMultiplier: 1.2f);
            ShipOverrides["BOAT Example 2"] = new ShipDragPerformanceData(
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
                serializer.WriteObject(w, ShipOverrides);
            }

            Logger!.LogInfo($"Wrote example ship configurations to {filePath}");
        }
    }
}