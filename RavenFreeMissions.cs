using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using Ravenfield.Trigger;
using System.Reflection;

namespace RavenFreeMissions
{
    [BepInPlugin("com.personperhaps.ravenfreemissions", "ravenfreemissions", "1.0")]
    public class RavenFreeMissions : BaseUnityPlugin
    {
        public static RavenFreeMissions pluginInstance;
        void Awake()
        {
            pluginInstance = this;
            enablePlugin = Config.Bind("General.Toggles",
                                                "Enable Plugin",
                                                true,
                                                "Self Explanatory");

            clearMutators = Config.Bind("General.Toggles",
                                                "Disable Clearing Mutators",
                                                true,
                                                "Scripted Missions might disable mutators. enable to prevent this from happening");
            

            loadOfficial = Config.Bind("General.Toggles",
                                                "Disable Auto Load Official",
                                                true,
                                                "Scripted Missions will load the vanilla vehicles forcefully. enable to prevent this from happening");
            forceTeam = Config.Bind("General.Toggles",
                                                "Disable Force Team",
                                                true,
                                                "Scripted Missions will force the player on a certain team. enable to prevent this from happening");
            forceAllowLoadoutPick = Config.Bind("General.Toggles",
                                                "Force Allow Loadout Pick",
                                                true,
                                                "Allows players to get their premade loadout");

            Debug.LogWarning("");
            Debug.Log("ravenfreemissions: Loading!");
            Harmony harmony = new Harmony("ravenfreemissions");
            harmony.PatchAll();
        }
        public ConfigEntry<bool> enablePlugin;
        public ConfigEntry<bool> clearMutators;
        public ConfigEntry<bool> loadOfficial;
        public ConfigEntry<bool> forceAllowLoadoutPick;
        public ConfigEntry<bool> forceTeam;

        [HarmonyPatch(typeof(ScriptedGameMode), "SetupGameConfig")]
        public class LoadConfigPatch
        {
            public static bool Prefix(ScriptedGameMode __instance)
            {
                if (pluginInstance.enablePlugin.Value)
                {
                    if (__instance.instantActionGameConfig == ScriptedGameMode.InstantActionGameConfig.Official && !pluginInstance.loadOfficial.Value)
                    {
                        GameManager.instance.gameInfo.LoadOfficial();
                    }
                    if (__instance.instantActionMutatorConfig == ScriptedGameMode.InstantActionMutatorConfig.None && !pluginInstance.clearMutators.Value)
                    {
                        GameManager.instance.gameInfo.ClearMutators();
                    }
                    if (!pluginInstance.forceTeam.Value)
                    {
                        ScriptedGameMode.TeamConfig teamConfig = __instance.playerTeam;
                        if (teamConfig == ScriptedGameMode.TeamConfig.Blue)
                        {
                            GameManager.GameParameters().playerTeam = 0;
                            return false;
                        }
                        if (teamConfig != ScriptedGameMode.TeamConfig.Red)
                        {
                            return false;
                        }
                        GameManager.GameParameters().playerTeam = 1;
                    }

                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GameModeBase), nameof(GameModeBase.OnOpenLoadoutPressed))]
        public class LoadoutPatch
        {
            public static bool Prefix(GameModeBase __instance)
            {
                if (pluginInstance.enablePlugin.Value && pluginInstance.forceAllowLoadoutPick.Value)
                {
                    if (GameModeBase.activeGameMode == null)
                    {
                        return false;
                    }
                    if (GameModeBase.activeGameMode.GetType() == typeof(ScriptedGameMode))
                    {
                        FpsActorController instance = FpsActorController.instance;
                        Actor playerActor = (instance != null) ? instance.actor : null;
                        if (LoadoutUi.IsOpen())
                        {
                            LoadoutUi.Hide(false);
                            return false;
                        }
                        LoadoutUi.Show(false);
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GameModeBase), nameof(GameModeBase.StartGame))]
        public class StartGamePatch
        {
            public static bool Prefix(GameModeBase __instance)
            {
                if (pluginInstance.enablePlugin.Value && pluginInstance.forceAllowLoadoutPick.Value)
                {
                    if (GameModeBase.activeGameMode.GetType() == typeof(ScriptedGameMode))
                    {
                        LoadoutUi.Show(false);
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(LoadoutUi), "ShowDeploymentTab")]
        public class LDeploymentTabFix
        {
            public static bool Prefix(LoadoutUi __instance)
            {
                if (pluginInstance.enablePlugin.Value && pluginInstance.forceAllowLoadoutPick.Value)
                {
                    if (GameModeBase.activeGameMode.GetType() == typeof(ScriptedGameMode))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Actor), nameof(Actor.SpawnAt))]
        public class SpawnPlayerPatch
        {
            public static void Prefix(Actor __instance, ref WeaponManager.LoadoutSet forcedLoadout)
            {
                if (pluginInstance.enablePlugin.Value && pluginInstance.forceAllowLoadoutPick.Value)
                {
                    if (GameModeBase.activeGameMode.GetType() == typeof(ScriptedGameMode) && __instance == ActorManager.instance.player)
                    {
                        WeaponManager.LoadoutSet loadout = LoadoutUi.instance.loadout;
                        forcedLoadout = loadout;
                        LoadoutUi.Hide(false);
                    }
                }
            }
        }
    }
}
