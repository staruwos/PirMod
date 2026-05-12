using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace PirMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StarterShovelPatch
    {
        [HarmonyPatch(nameof(StartOfRound.StartGame))]
        [HarmonyPostfix]
        private static void SpawnItemOnLanding(StartOfRound __instance)
        {
            if (!PirMod.cfgStarterShovel.Value) return;

            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
                return;

            //  Shovel to spawn
            Item itemToSpawn = __instance.allItemsList.itemsList.FirstOrDefault(i => i.itemName == "Shovel");

            if (itemToSpawn != null)
            {
                Vector3 spawnPos = __instance.playerSpawnPositions[0].position + new Vector3(0f, 1f, 0f);

                GameObject itemObj = Object.Instantiate(itemToSpawn.spawnPrefab, spawnPos, Quaternion.identity);
                itemObj.GetComponent<GrabbableObject>().fallTime = 0f;

                // Parent the item to the ship so it doesn't clip through the floor while moving
                itemObj.transform.SetParent(__instance.elevatorTransform, true);

                itemObj.GetComponent<NetworkObject>().Spawn();

                PirMod.Logger.LogInfo("[PirMod] Starter Shovel spawned successfully!");
            }
        }
    }
}