using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace LeopardDropLimit
{
    [HarmonyPatch(typeof(LanceConfiguratorPanel), "ValidateLance")]
    public static class LanceConfiguratorPanel_ValidateLance_Patch
    {
        public static void Postfix(LanceConfiguratorPanel __instance, ref bool __result)
        {
            float lanceTonnage = 0;

            if (DropLimit.Settings.OnlyInSimGame && !__instance.IsSimGame)
                return;

            var mechs = new List<MechDef>();
            for (var i = 0; i < __instance.maxUnits; i++)
            {
                var lanceLoadoutSlot = Traverse.Create(__instance).Field("loadoutSlots").GetValue<LanceLoadoutSlot[]>()[i];

                if (lanceLoadoutSlot.SelectedMech == null)
                    continue;

                mechs.Add(lanceLoadoutSlot.SelectedMech.MechDef);
                lanceTonnage += lanceLoadoutSlot.SelectedMech.MechDef.Chassis.Tonnage;
            }

            if (lanceTonnage <= DropLimit.Settings.MaxTonnage)
                return;

            __instance.lanceValid = false;

            var headerWidget = Traverse.Create(__instance).Field("headerWidget").GetValue<LanceHeaderWidget>();
            headerWidget.RefreshLanceInfo(__instance.lanceValid, "Lance cannot exceed tonnage limit", mechs);

            Traverse.Create(__instance).Field("lanceErrorText").SetValue("Lance cannot exceed tonnage limit\n");

            __result = __instance.lanceValid;
        }
    }


    internal class ModSettings
    {
        public float MaxTonnage = 300;
        public bool OnlyInSimGame = true;
    }

    public static class DropLimit
    {
        internal static ModSettings Settings = new ModSettings();
        public static void Init(string directory, string settingsJSON)
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.DropLimit");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // read settings
            try
            {
                Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON);
            }
            catch (Exception)
            {
                Settings = new ModSettings();
            }
        }
    }
}
