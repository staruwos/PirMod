using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace PirMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PirMod : BaseUnityPlugin
{
    public static PirMod Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public static ConfigEntry<bool> cfgInfiniteSprint = null!;
    public static ConfigEntry<bool> cfgMineMonster = null!;
    public static ConfigEntry<bool> cfgStarterShovel = null!;
    public static ConfigEntry<bool> cfgTurretTweaks = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        cfgInfiniteSprint = Config.Bind("Features", "InfiniteSprint", false, "Enable infinite stamina.");
        cfgMineMonster = Config.Bind("Features", "MineMonster", true, "Monsters can step on and trigger landmines.");
        cfgStarterShovel = Config.Bind("Features", "StarterShovel", false, "Spawn a shovel when the game starts.");
        cfgTurretTweaks = Config.Bind("Features", "TurretTweaks", true, "Allow hitting turrets with a shovel to disable them.");

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("[PirMod] Patching...");

        Logger.LogInfo("MURILO VIADO");

        Harmony.PatchAll();

        Logger.LogDebug("[PirMod] Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("[PirMod] Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("[PirMod] Finished unpatching!");
    }
}
