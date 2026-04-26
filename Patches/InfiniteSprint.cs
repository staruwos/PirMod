using HarmonyLib;
using GameNetcodeStuff;
using Unity.Netcode; 

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

            if (NetworkManager.Singleton == null) return;

            if (!__instance.IsOwner) return;

            __instance.sprintMeter = 1.0f;
        }
    }
}