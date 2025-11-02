using System;
using System.Collections.Generic;
using UnityEngine;
#if DEBUG
using System.Reflection;
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
            return nameof(ShipDragPerformanceData) + "(" + this.FieldRepr() + ")";
        }

        internal string FieldRepr()
        {
#if !DEBUG
            return "";
#else
            static string FuncRepr(DragForceFunction? func)
            {
                if (func is null)
                    return "";

                var info = func.GetMethodInfo();
                return info.DeclaringType.Name + "." + info.Name;
            }
            return String.Join(
                ", ",
                $"LWL={this.LengthAtWaterline}",
                $"FormFactor={this.FormFactor}",
                $"ViscousDragMultiplier={this.ViscousDragMultiplier}",
                $"WaveMakingDragMultiplier={this.WaveMakingDragMultiplier}",
                $"CalculateViscousDragForce={FuncRepr(this.CalculateViscousDragForce)}",
                $"CalculateWaveMakingDragForce={FuncRepr(this.CalculateWaveMakingDragForce)}"
            );
#endif
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
    /// Storage class for this mod's ship configurations.
    /// </summary>
    public static class ShipDragDataStore
    {
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
            ShipDragPerformanceData? userData = GetPerformance(ship, userPerformance);
            ShipDragPerformanceData? customData = GetPerformance(ship, customPerformance);
            ShipDragPerformanceData defaultData = (ShipDragPerformanceData)GetDefaultPerformance(
                ship
            );

            ShipDragPerformanceData mergedData = defaultData;

            if (customData is not null)
                mergedData = ShipDragPerformanceData.Merge(customData, mergedData);

            if (userData is not null)
                mergedData = ShipDragPerformanceData.Merge(userData, mergedData);

            var finalData = FinalShipDragPerformanceData.FillWithDefaults(mergedData);

#if DEBUG
            BetterDragDebug.LogLinesBuffered(
                [
                    $"\nMerging data for: {ship.name}",
                    $"User data: {userData}",
                    $"Custom data: {customData}",
                    $"Default data: {defaultData}",
                    $"Merged data: {finalData}\n",
                ]
            );
#endif
            return finalData;
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
                "BOAT dhow small (10)" => new(12f, 0.25f),
                "BOAT dhow medium (20)" => new(22f, 0.21f),
                "BOAT medi small (40)" => new(12.39f, 0.24f),
                "BOAT medi medium (50)" => new(25.31f, 0.19f),
                "BOAT junk large (70)" => new(28f, 0.23f),
                "BOAT junk medium (80)" => new(24f, 0.22f),
                "BOAT junk small singleroof(90)" => new(12f, 0.25f),
                "BOAT Shroud Small" => new(14.77f, 0.08f, 0.9f, 0.95f),
                "BOAT Shroud Large" => new(34.56f, 0.05f, 0.85f, 0.9f),
                "BOAT GLORIANA (182)" => new(30f, 0.18f),
                "BOAT CHRONIAN (187)" => new(35f, 0.20f),
                "BOAT CAELANOR (192)" => new(20f, 0.26f),
                "BOAT GALLUS (197)" => new(7f, 0.20f),
                _ => new(),
            };
        }

        private static string GetNormalizedShipName(GameObject ship)
        {
            var shipName = ship.name;
            var suffix = "(Clone)";

            while (shipName.EndsWith(suffix))
            {
                shipName = shipName.Substring(0, shipName.Length - suffix.Length);
            }
            return shipName;
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
    internal class FinalShipDragPerformanceData(
        float lengthAtWaterline = 20,
        float formFactor = 0.20f,
        float viscousDragMultiplier = 1.0f,
        float waveMakingDragMultiplier = 1.0f
    )
    {
        public float LengthAtWaterline = lengthAtWaterline;
        public float FormFactor = formFactor;
        public float ViscousDragMultiplier = viscousDragMultiplier;
        public float WaveMakingDragMultiplier = waveMakingDragMultiplier;
        public ShipDragPerformanceData.DragForceFunction CalculateViscousDragForce =
            DragModel.CalculateViscousDragForce;
        public ShipDragPerformanceData.DragForceFunction CalculateWaveMakingDragForce =
            DragModel.CalculateWaveMakingDragForce;

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(FinalShipDragPerformanceData)
                + "("
                + ((ShipDragPerformanceData)this).FieldRepr()
                + ")";
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
