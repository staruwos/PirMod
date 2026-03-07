using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace PirMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StarterShovelPatch
    {
        // CHANGED: We now hook into "StartGame" (The Lever Pull) 
        // instead of "ResetShip" so it's easier to test.
        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        private static void SpawnItemOnLanding(StartOfRound __instance)
        {
            if (!PirMod.cfgStarterShovel.Value) return;

            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
                return;

            // Example: Spawn a Shovel
            Item itemToSpawn = __instance.allItemsList.itemsList.FirstOrDefault(i => i.itemName == "Shovel");

            if (itemToSpawn != null)
            {
                // Spawn it slightly in the air so it doesn't get stuck
                Vector3 spawnPos = __instance.playerSpawnPositions[0].position + new Vector3(0f, 1f, 0f);

                GameObject itemObj = Object.Instantiate(itemToSpawn.spawnPrefab, spawnPos, Quaternion.identity);
                itemObj.GetComponent<GrabbableObject>().fallTime = 0f;
                itemObj.GetComponent<NetworkObject>().Spawn();

                Debug.Log("TESTING: Shovel spawned on landing!");
            }
        }
    }
}
