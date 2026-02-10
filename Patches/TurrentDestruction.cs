using HarmonyLib;
using UnityEngine;

namespace PirMod.Patches
{
    [HarmonyPatch(typeof(Shovel))]
    internal class TurretDestructionPatch
    {
        // Define the "MapHazards" layer (where Turrets and Mines exist)
        private static int mapHazardLayer = LayerMask.GetMask("MapHazards");

        [HarmonyPatch("HitShovel")]
        [HarmonyPostfix]
        private static void CheckForTurretHit(Shovel __instance)
        {
            // Get the player holding the shovel
            // We use the 'playerHeldBy' field from the Shovel (GrabbableObject) class
            var player = __instance.playerHeldBy;

            // Safety check: if no player is holding it, stop.
            if (player == null) return;

            // Determine where we are looking and hitting
            Vector3 hitPoint = player.gameplayCamera.transform.position;
            Vector3 hitDirection = player.gameplayCamera.transform.forward;

            // Cast a ray (shoot a laser) forward to see if we hit a Turret
            // We use a SphereCast (thick laser) to make hitting easier. Range is 1.5f.
            if (Physics.SphereCast(hitPoint, 0.5f, hitDirection, out RaycastHit hitInfo, 1.5f, mapHazardLayer))
            {
                // Try to find the Turret script on the object we hit
                Turret hitTurret = hitInfo.collider.gameObject.GetComponentInParent<Turret>();

                if (hitTurret != null && hitTurret.turretActive)
                {
                    // === TURRET FOUND! ===

                    // Access RoundManager via its global Instance, not through the shovel
                    RoundManager.Instance.PlayAudibleNoise(hitInfo.point, 10f, 1f, 0, false, 0);

                    // Trigger the visual "hit" animation on the player
                    player.playerBodyAnimator.SetTrigger("shovelHit");

                    // Play the metal clank sound (using the shovel's built-in audio source)
                    __instance.shovelAudio.PlayOneShot(__instance.reelUp);

                    // Disable the turret
                    hitTurret.turretActive = false;
                    hitTurret.ToggleTurretEnabled(false);

                    Debug.Log("Bonk! Turret destroyed.");
                }
            }
        }
    }
}
