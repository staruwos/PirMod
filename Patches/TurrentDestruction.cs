using HarmonyLib;
using Unity.Netcode; // Required for __0 access if parameter is networked
using UnityEngine;

namespace PirMod.Patches
{
    [HarmonyPatch] 
    internal class TurretTweaks
    {
        private static int mapHazardLayer = LayerMask.GetMask("MapHazards");

        [HarmonyPatch(typeof(Shovel), nameof(Shovel.HitShovel))]
        [HarmonyPostfix]
        private static void CheckForTurretHit(Shovel __instance)
        {
            if (!PirMod.cfgTurretTweaks.Value) return;

            var player = __instance.playerHeldBy;
            if (player == null) return;

            Vector3 hitPoint = player.gameplayCamera.transform.position;
            Vector3 hitDirection = player.gameplayCamera.transform.forward;

            if (Physics.SphereCast(hitPoint, 0.5f, hitDirection, out RaycastHit hitInfo, 1.5f, mapHazardLayer))
            {
                Turret hitTurret = hitInfo.collider.gameObject.GetComponentInParent<Turret>();

                // We also check && hitTurret.enabled so we don't 'kill' an already dead turret.
                if (hitTurret != null && hitTurret.turretActive && hitTurret.enabled)
                {
                    RoundManager.Instance.PlayAudibleNoise(hitInfo.point, 10f, 1f, 0, false, 0);
                    player.playerBodyAnimator.SetTrigger("shovelHit");
                    __instance.shovelAudio.PlayOneShot(__instance.reelUp);

                    // Tell server to turn it off globally.
                    hitTurret.ToggleTurretServerRpc(false);
                }
            }
        }

        // We use string patching here for better v80 compatibility if nameof fails on netcode.
        [HarmonyPatch(typeof(Turret), "ToggleTurretClientRpc")]
        [HarmonyPostfix]
        private static void FinalV80TurretKill(Turret __instance, bool __0) // __0 is the 'bool enabled' parameter
        {
            if (!PirMod.cfgTurretTweaks.Value) return;

            if (!__0) // If the host is disabling this turret (ServerRpc false -> ClientRpc false)
            {
                PirMod.Logger.LogInfo("V80: Aggressively killing turret and disabling script component.");

                // Force state to IDLE.
                __instance.turretMode = 0;

                // Kill all sounds aggressively.
                AudioSource[] turretSounds = __instance.gameObject.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource audio in turretSounds)
                {
                    audio.Stop(); // Stop what is playing
                    audio.clip = null; // Clear the clip entirely to prevent restart.
                }

                // THE V80 CURE: Disable the entire script component.
                // This prevents Turret.Update() from ever running again on this turret instance.
                // It cannot shoot or make noise if the code isn't running.
                __instance.enabled = false;
            }
        }
    }
}