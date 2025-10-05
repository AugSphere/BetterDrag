using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    /// <summary>
    /// A class holding drag performance setting overrides for a single ship.
    ///
    /// <para>
    /// All entries are optional, leave <c>null</c> for the ones you do not want to override.
    /// </para>
    ///
    /// <para>
    /// All units are metric. Unit reference: cog's LWL is approximately 12.39m.
    /// </para>
    /// </summary>
    [Serializable]
    public class ShipDragPerformanceData
    {
        /// <summary>
        /// Length of the hull at waterline in metres.
        /// </summary>
        public float? LengthAtWaterline = null;

        /// <summary>
        /// Form factor of the hull for ITTC 57 friction line.
        ///
        /// <para>
        /// Represents additional drag caused by a ship's hull form compared to a flat plate of the same wetted surface area.
        /// </para>
        /// <para>
        /// Typical values range from 0.05 to 0.30, higher means more resistance.
        /// </para>
        /// </summary>
        public float? FormFactor = null;

        /// <summary>
        /// Ship-specific drag multiplier for viscous resistance.
        /// </summary>
        public float? ViscousDragMultiplier = null;

        /// <summary>
        /// Ship-specific drag multippier for wave-making resistance.
        /// </summary>
        public float? WaveMakingDragMultiplier = null;

        /// <summary>
        /// Custom force function type.
        /// </summary>
        /// <param name="forwardVelocity">Absolute forward component of ship velocity in default unity metres/second.</param>
        /// <param name="lengthAtWaterline">Length at waterline in metres. Specified in ship's configuration.</param>
        /// <param name="formFactor">Form factor of the ship. Specified in ship's configuration.</param>
        /// <param name="displacement">Ship's displacement in m^3. Calculated by the mod.</param>
        /// <param name="wettedArea">Ship's wetted surface area in m^2. Calculated by the mod.</param>
        /// <returns>Absolute water drag force magnitude in N.</returns>
        public delegate float DragForceFunction(
            float forwardVelocity,
            float lengthAtWaterline,
            float formFactor,
            float displacement,
            float wettedArea
        );

        /// <summary>
        /// An optional custom viscous drag force curve as a function of velocity and ship characteristics.
        /// <para>
        /// Input speed is non-negative in m/s (around 5 for 10 chip log knots), typical outputs are on the order of 500 for a small ship at 5m/s.
        /// </para>
        /// </summary>
        [NonSerialized]
        public DragForceFunction? CalculateViscousDragForce = null;

        /// <summary>
        /// Same as <see cref="CalculateViscousDragForce"/>, but for wave-making drag.
        /// </summary>
        [NonSerialized]
        public DragForceFunction? CalculateWaveMakingDragForce = null;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ShipDragPerformanceData(LWL={this.LengthAtWaterline}, FormFactor={this.FormFactor}, ViscousDragMultiplier={this.ViscousDragMultiplier}, WaveMakingDragMultiplier={this.WaveMakingDragMultiplier})";
        }

        internal static ShipDragPerformanceData Merge(
            ShipDragPerformanceData highPriority,
            ShipDragPerformanceData lowPriority
        )
        {
            return new ShipDragPerformanceData()
            {
                LengthAtWaterline = highPriority.LengthAtWaterline ?? lowPriority.LengthAtWaterline,
                FormFactor = highPriority.FormFactor ?? lowPriority.FormFactor,
                ViscousDragMultiplier =
                    highPriority.ViscousDragMultiplier ?? lowPriority.ViscousDragMultiplier,
                WaveMakingDragMultiplier =
                    highPriority.WaveMakingDragMultiplier ?? lowPriority.WaveMakingDragMultiplier,
                CalculateViscousDragForce =
                    highPriority.CalculateViscousDragForce ?? lowPriority.CalculateViscousDragForce,
                CalculateWaveMakingDragForce =
                    highPriority.CalculateWaveMakingDragForce
                    ?? lowPriority.CalculateWaveMakingDragForce,
            };
        }
    };

    /// <summary>
    /// Storage class for mod's ship configurations.
    /// </summary>
    public static class ShipDragDataStore
    {
        private static readonly ConditionalWeakTable<
            GameObject,
            FinalShipDragPerformanceData
        > dataCache = new();
        private static Dictionary<String, ShipDragPerformanceData> userPerformance = [];
        private static readonly Dictionary<String, ShipDragPerformanceData> customPerformance = [];

        /// <summary>
        /// Store custom performance data for a ship.
        /// <para>Existing data is overwritten.</para>
        /// </summary>
        /// <param name="shipName">The name of the ship object. Can be found in a <see  href="https://docs.google.com/spreadsheets/d/12ndyNEJiD8HcoesP820oOKChHkRptmAVZpposfEcEaY/edit?usp=sharing">community spreadsheet</see>.</param>
        /// <param name="data">Ship's peformance overrides.</param>
        /// <returns>`true` if custom performace was successfully set.</returns>
        public static bool SetCustomPerformance(string? shipName, ShipDragPerformanceData? data)
        {
            if (shipName is null || data is null)
                return false;
            customPerformance[shipName] = data;
            return true;
        }

        /// <summary>
        /// Return drag performance for a ship.
        /// <para>Priority: user config > custom performance > default performance.</para>
        /// </summary>
        internal static FinalShipDragPerformanceData GetPerformanceData(GameObject ship)
        {
            dataCache.TryGetValue(ship, out var data);
            if (data is not null)
                return data;

            var finalData = MergeConfigs(ship);
            dataCache.Add(ship, finalData);
            return finalData;
        }

        private static FinalShipDragPerformanceData MergeConfigs(GameObject ship)
        {
            ShipDragPerformanceData? userData = GetPerformance(ship, userPerformance);
            ShipDragPerformanceData? customData = GetCustomPerformance(ship);
            ShipDragPerformanceData defaultData = (ShipDragPerformanceData)GetDefaultPerformance(
                ship
            );

            ShipDragPerformanceData?[] dataList = [userData, customData, defaultData];
            var mergedData = dataList
                .OfType<ShipDragPerformanceData>()
                .Aggregate((higher, lower) => ShipDragPerformanceData.Merge(higher, lower));
            var finalData = FinalShipDragPerformanceData.FillWithDefaults(mergedData);

#if DEBUG
            FileLog.Log($"Ship data not in cache: {ship.name}");
            FileLog.Log(
                $"Default data: length {defaultData.LengthAtWaterline}, form factor {defaultData.FormFactor}"
            );
            FileLog.Log(
                $"Selected data: length {finalData.LengthAtWaterline}, form factor {finalData.FormFactor}\n"
            );
#endif
            return finalData;
        }

        internal static ShipDragPerformanceData? GetCustomPerformance(GameObject ship)
        {
            return GetPerformance(ship, customPerformance);
        }

        internal static void FillUserPerformance(
            Dictionary<string, ShipDragPerformanceData> userConfig
        )
        {
            userPerformance = userConfig;
        }

        internal static FinalShipDragPerformanceData GetDefaultPerformance(GameObject ship)
        {
            var shipName = GetNormalizedShipName(ship);
            return GetDefaultPerformanceByName(shipName);
        }

        internal static FinalShipDragPerformanceData GetDefaultPerformanceByName(string shipName)
        {
            return (shipName) switch
            {
                "BOAT dhow small (10)" => new() { LengthAtWaterline = 12f, FormFactor = 0.11f },
                "BOAT dhow medium (20)" => new() { LengthAtWaterline = 22f, FormFactor = 0.10f },
                "BOAT medi small (40)" => new() { LengthAtWaterline = 12.39f, FormFactor = 0.10f },
                "BOAT medi medium (50)" => new() { LengthAtWaterline = 25.31f, FormFactor = 0.11f },
                "BOAT junk large (70)" => new() { LengthAtWaterline = 28f, FormFactor = 0.13f },
                "BOAT junk medium (80)" => new() { LengthAtWaterline = 24f, FormFactor = 0.13f },
                "BOAT junk small singleroof(90)" => new()
                {
                    LengthAtWaterline = 12f,
                    FormFactor = 0.13f,
                },
                "BOAT Shroud Small" => new() { LengthAtWaterline = 14.77f, FormFactor = 0.07f },
                "BOAT Shroud Large" => new() { LengthAtWaterline = 34.56f, FormFactor = 0.06f },
                "BOAT GLORIANA (182)" => new() { LengthAtWaterline = 30f, FormFactor = 0.15f },
                "BOAT CHRONIAN (187)" => new() { LengthAtWaterline = 35f, FormFactor = 0.15f },
                "BOAT CAELANOR (192)" => new() { LengthAtWaterline = 20f, FormFactor = 0.20f },
                "BOAT GALLUS (197)" => new() { LengthAtWaterline = 7f, FormFactor = 0.08f },
                _ => new(),
            };
        }

        private static string GetNormalizedShipName(GameObject ship)
        {
            var shipName = ship.name;
            var suffix = "(Clone)";

            if (shipName.EndsWith(suffix))
            {
                return shipName.Substring(0, shipName.Length - suffix.Length);
            }
            return ship.name;
        }

        private static ShipDragPerformanceData? GetPerformance(
            GameObject ship,
            Dictionary<String, ShipDragPerformanceData> store
        )
        {
            var isPresent = store.TryGetValue(
                GetNormalizedShipName(ship),
                out ShipDragPerformanceData data
            );
            return isPresent ? data : null;
        }
    }

    /// <summary>
    /// Same as <see cref="ShipDragPerformanceData"/>, but all values are not null.
    /// </summary>
    internal class FinalShipDragPerformanceData
    {
        public float LengthAtWaterline;
        public float FormFactor;
        public float ViscousDragMultiplier;
        public float WaveMakingDragMultiplier;
        public ShipDragPerformanceData.DragForceFunction CalculateViscousDragForce;
        public ShipDragPerformanceData.DragForceFunction CalculateWaveMakingDragForce;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"FinalShipDragPerformanceData(LWL={this.LengthAtWaterline}, FormFactor={this.FormFactor}, ViscousDragMultiplier={this.ViscousDragMultiplier}, WaveMakingDragMultiplier={this.WaveMakingDragMultiplier})";
        }

        public FinalShipDragPerformanceData()
        {
            LengthAtWaterline = 20;
            FormFactor = 0.15f;
            ViscousDragMultiplier = 1.0f;
            WaveMakingDragMultiplier = 1.0f;
            CalculateViscousDragForce = DragCalculation.CalculateViscousDragForce;
            CalculateWaveMakingDragForce = DragCalculation.CalculateWaveMakingDragForce;
        }

        public static FinalShipDragPerformanceData FillWithDefaults(ShipDragPerformanceData data)
        {
            var defaultData = new FinalShipDragPerformanceData();
            return new()
            {
                LengthAtWaterline = data.LengthAtWaterline ?? defaultData.LengthAtWaterline,
                FormFactor = data.FormFactor ?? defaultData.FormFactor,
                ViscousDragMultiplier =
                    data.ViscousDragMultiplier ?? defaultData.ViscousDragMultiplier,
                WaveMakingDragMultiplier =
                    data.WaveMakingDragMultiplier ?? defaultData.WaveMakingDragMultiplier,
                CalculateViscousDragForce =
                    data.CalculateViscousDragForce ?? defaultData.CalculateViscousDragForce,
                CalculateWaveMakingDragForce =
                    data.CalculateWaveMakingDragForce ?? defaultData.CalculateWaveMakingDragForce,
            };
        }

        public static explicit operator ShipDragPerformanceData(FinalShipDragPerformanceData data)
        {
            return new()
            {
                LengthAtWaterline = data.LengthAtWaterline,
                FormFactor = data.FormFactor,
                ViscousDragMultiplier = data.ViscousDragMultiplier,
                WaveMakingDragMultiplier = data.WaveMakingDragMultiplier,
                CalculateViscousDragForce = data.CalculateViscousDragForce,
                CalculateWaveMakingDragForce = data.CalculateWaveMakingDragForce,
            };
        }
    }
}
