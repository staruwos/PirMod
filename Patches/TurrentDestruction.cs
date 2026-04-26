using HarmonyLib;
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

                if (hitTurret != null && hitTurret.turretActive)
                {
                    RoundManager.Instance.PlayAudibleNoise(hitInfo.point, 10f, 1f, 0, false, 0);
                    player.playerBodyAnimator.SetTrigger("shovelHit");
                    __instance.shovelAudio.PlayOneShot(__instance.reelUp);

                    // Tell server to turn it off
                    hitTurret.ToggleTurretServerRpc(false);
                }
            }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.ToggleTurretClientRpc))]
        [HarmonyPostfix]
        private static void ForceStopAudioOnNetwork(Turret __instance, bool __0) // Use __0 to avoid parameter renaming issues
        {
            if (!__0) // __0 represents the first argument passed (the true/false toggle)
            {
                __instance.turretMode = 0; // Force Idle

                AudioSource[] turretSounds = __instance.gameObject.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource audio in turretSounds)
                {
                    if (audio.isPlaying)
                    {
                        audio.Stop();
                    }
                }
            }
        }
    }
}