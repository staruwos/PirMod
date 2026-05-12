using GameNetcodeStuff;
using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PirMod.Patches
{
    [HarmonyPatch]
    internal class WeedSprayBeesPatch
    {
        private static float checkTimer = 0f;
        private const string SPRAY_BEES_MSG = "PirMod_SprayBees";
        private const string SPRAY_HIVE_MSG = "PirMod_SprayHive";

        // =====================================================================
        // 1. REGISTER THE RADIO FREQUENCIES
        // =====================================================================
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        private static void RegisterMessages()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SPRAY_BEES_MSG, OnServerReceiveBeeKill);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SPRAY_HIVE_MSG, OnServerReceiveHiveKill);
            }
        }

        // =====================================================================
        // 2. THE LOCAL SPRAY CHECK 
        // =====================================================================
        [HarmonyPatch(typeof(GrabbableObject), "Update")]
        [HarmonyPostfix]
        private static void SprayCheck(GrabbableObject __instance)
        {
            if (!PirMod.cfgWeedSprayBees.Value) return;

            if (__instance.itemProperties == null || __instance.playerHeldBy == null) return;
            if (!__instance.playerHeldBy.IsOwner) return;

            if (!__instance.itemProperties.itemName.Contains("Weed killer")) return;
            if (!__instance.isBeingUsed) return;

            checkTimer += Time.deltaTime;
            if (checkTimer < 0.25f) return;
            checkTimer = 0f;

            PlayerControllerB player = __instance.playerHeldBy;
            Vector3 sprayPos = player.gameplayCamera.transform.position;
            Vector3 sprayDir = player.gameplayCamera.transform.forward;

            // --- SCENARIO A: RUIN THE HIVE (DECOUPLED) ---
            // We now search for the Hive directly as an item, ignoring the bees entirely!
            GrabbableObject[] allItems = Object.FindObjectsOfType<GrabbableObject>();
            foreach (var item in allItems)
            {
                if (item.itemProperties != null && item.itemProperties.itemName == "Hive" && item.IsSpawned)
                {
                    float distToHive = Vector3.Distance(sprayPos, item.transform.position);
                    if (distToHive < 4.5f)
                    {
                        if (NetworkManager.Singleton.IsServer)
                        {
                            DestroyHiveOnServer(item);
                        }
                        else
                        {
                            FastBufferWriter writer = new FastBufferWriter(8, Allocator.Temp);
                            writer.WriteValueSafe(item.GetComponent<NetworkObject>().NetworkObjectId);
                            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SPRAY_HIVE_MSG, NetworkManager.ServerClientId, writer);
                        }
                    }
                }
            }

            // --- SCENARIO B: EXTERMINATE THE BEES ---
            RedLocustBees[] allBees = Object.FindObjectsOfType<RedLocustBees>();
            foreach (var bees in allBees)
            {
                if (!bees.isEnemyDead)
                {
                    float distToBees = Vector3.Distance(sprayPos, bees.transform.position);

                    if (distToBees < 6.0f)
                    {
                        Vector3 dirToBees = (bees.transform.position - sprayPos).normalized;
                        float dotProduct = Vector3.Dot(sprayDir, dirToBees);

                        if (dotProduct > 0.2f)
                        {
                            if (NetworkManager.Singleton.IsServer)
                            {
                                KillBeesOnServer(bees);
                            }
                            else
                            {
                                FastBufferWriter writer = new FastBufferWriter(8, Allocator.Temp);
                                writer.WriteValueSafe(bees.GetComponent<NetworkObject>().NetworkObjectId);
                                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SPRAY_BEES_MSG, NetworkManager.ServerClientId, writer);
                            }
                        }
                    }
                }
            }
        }

        // =====================================================================
        // 3. THE SERVER RECEIVERS
        // =====================================================================
#pragma warning disable Harmony003
        private static void OnServerReceiveBeeKill(ulong senderId, FastBufferReader payload)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            payload.ReadValueSafe(out ulong netId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject netObj))
            {
                RedLocustBees bees = netObj.GetComponent<RedLocustBees>();
                if (bees != null && !bees.isEnemyDead) KillBeesOnServer(bees);
            }
        }

        private static void OnServerReceiveHiveKill(ulong senderId, FastBufferReader payload)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            payload.ReadValueSafe(out ulong netId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject netObj))
            {
                GrabbableObject hive = netObj.GetComponent<GrabbableObject>();
                if (hive != null) DestroyHiveOnServer(hive);
            }
        }
#pragma warning restore Harmony003

        // =====================================================================
        // 4. THE EXECUTORS
        // =====================================================================
        private static void KillBeesOnServer(RedLocustBees bees)
        {
            PirMod.Logger.LogInfo("[PirMod] Weed spray exterminated the bees!");

            bees.isEnemyDead = true; // Safety toggle for other scripts
            bees.gameObject.GetComponent<NetworkObject>().Despawn(true);
        }

        private static void DestroyHiveOnServer(GrabbableObject hive)
        {
            PirMod.Logger.LogInfo("[PirMod] Weed spray contaminated the hive! Destroying it.");
            hive.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}