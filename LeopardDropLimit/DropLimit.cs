using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using BattleTech;
using BattleTech.UI;
using Newtonsoft.Json;

namespace LeopardDropLimit
{
    // CODE FROM MORPHYUM
    public static class ReflectionHelper
    {
        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters)
        {
            var type = instance.GetType();
            var methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo.Invoke(instance, parameters);
        }

        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters, Type[] types)
        {
            var type = instance.GetType();
            var methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return methodInfo.Invoke(instance, parameters);
        }

        public static void SetPrivateProperty(object instance, string propertyname, object value)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(instance, value, null);
        }

        public static void SetPrivateField(object instance, string fieldname, object value)
        {
            var type = instance.GetType();
            var field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static object GetPrivateField(object instance, string fieldname)
        {
            var type = instance.GetType();
            var field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
                var lanceLoadoutSlot = ((LanceLoadoutSlot[]) ReflectionHelper.GetPrivateField(__instance, "loadoutSlots"))[i];

                if (lanceLoadoutSlot.SelectedMech == null) continue;

                mechs.Add(lanceLoadoutSlot.SelectedMech.MechDef);
                lanceTonnage += lanceLoadoutSlot.SelectedMech.MechDef.Chassis.Tonnage;
            }

            if (lanceTonnage <= DropLimit.Settings.MaxTonnage)
                return;

            __instance.lanceValid = false;

            var headerWidget = ReflectionHelper.GetPrivateField(__instance, "headerWidget");
            (headerWidget as LanceHeaderWidget).RefreshLanceInfo(__instance.lanceValid, "Lance cannot exceed tonnage limit", mechs);

            ReflectionHelper.SetPrivateField(__instance, "lanceErrorText", "Lance cannot exceed tonnage limit\n");

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
