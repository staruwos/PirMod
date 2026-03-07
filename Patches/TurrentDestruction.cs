using HarmonyLib;
using UnityEngine;

namespace PirMod.Patches
{
    internal class TurretTweaks
    {
        private static int mapHazardLayer = LayerMask.GetMask("MapHazards");

        [HarmonyPatch(typeof(Shovel), "HitShovel")] // Target class AND method specified here
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

        [HarmonyPatch(typeof(Turret), "ToggleTurretClientRpc")] // Target class AND method specified here
        [HarmonyPostfix]
        private static void ForceStopAudioOnNetwork(Turret __instance, bool enabled)
        {
            if (!enabled)
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
