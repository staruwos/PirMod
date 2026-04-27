using HarmonyLib;
using UnityEngine;
using Unity.Netcode; // Needed for network syncing (IsServer)
using GameNetcodeStuff;

namespace PirMod.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class MineMonsterPatch
    {
        // We hook into OnTriggerEnter, which fires whenever *anything* touches the mine
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix] // Run BEFORE the game's normal logic
        private static void MonsterTrigger(Landmine __instance, Collider other)
        {
            if (!PirMod.cfgMineMonster.Value) return;

            // Safety Checks
            // If the mine has already blown up, do nothing.
            if (__instance.hasExploded) return;

            // Only the HOST should calculate this.
            // If we let clients do it, the mine might explode 4 times (once for each player).
            if (!NetworkManager.Singleton.IsServer) return;

            // Check if the thing that stepped on it is an Enemy
            // We use GetComponentInParent because the collider might be on the foot/leg/body
            EnemyAI enemy = other.gameObject.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                // Make sure the enemy is actually alive
                if (!enemy.isEnemyDead)
                {
                    Debug.Log($"[MyFirstMod] {enemy.enemyType.enemyName} stepped on a mine! BOOM!");

                    // Explode the Mine
                    // We call the ServerRpc to tell everyone "This mine exploded"
                    __instance.ExplodeMineServerRpc();

                    // Kill or Damage the Enemy
                    // 'KillEnemyOnOwnerClient' forces the enemy to die properly (play death anim, stop moving)
                    // The 'true' overrides any invincibility.
                    enemy.KillEnemyServerRpc(false);
                }
            }
        }
    }
}
