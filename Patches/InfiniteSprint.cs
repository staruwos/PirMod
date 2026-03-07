using HarmonyLib;
using GameNetcodeStuff;

namespace PirMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class InfiniteSprintPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void InfiniteStamina(PlayerControllerB __instance)
        {
            if (!PirMod.cfgInfiniteSprint.Value) return;
            __instance.sprintMeter = 1.0f; // Keeps stamina full always
        }
    }
}
