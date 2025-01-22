using BepInEx;
using HarmonyLib;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Wish;

public static class PluginInfo {

    public const string TITLE = "Player Stats";
    public const string NAME = "player_stats";
    public const string SHORT_DESCRIPTION = "Adjust players stats and skill procs using configurable values.";
	public const string EXTRA_DETAILS = "This mod does not make any permanent changes to any skills or proc chances.  It simply modifies the stats in memory for the duration of the game.  Removing the mod and restarting the game will revert everything to its default state.";

	public const string VERSION = "0.0.2";

    public const string AUTHOR = "devopsdinosaur";
    public const string GAME_TITLE = "Sun Haven";
    public const string GAME = "sunhaven";
    public const string GUID = AUTHOR + "." + GAME + "." + NAME;
    public const string REPO = "sunhaven-mods";

    public static Dictionary<string, string> to_dict() {
        Dictionary<string, string> info = new Dictionary<string, string>();
        foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
            info[field.Name.ToLower()] = (string) field.GetValue(null);
        }
        return info;
    }
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class TestingPlugin : DDPlugin {
	private static TestingPlugin m_instance = null;
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
        logger = this.Logger;
        try {
			m_instance = this;
            this.m_plugin_info = PluginInfo.to_dict();
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

	[HarmonyPatch(typeof(GameManager), "Awake")]
	class HarmonyPatch_GameManager_Awake {
		private static void Postfix() {
			try {
				Settings.Instance.load(m_instance, null);
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_GameManager_Awake.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(Player), "GetStat")]
	class HarmonyPatch_Player_GetStat {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (Settings.m_enabled.Value) {
					__result += Settings.m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}

	[HarmonyPatch(typeof(Player), "GetMyStat")]
	class HarmonyPatch_Player_GetMyStat {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (Settings.m_enabled.Value) {
					__result += Settings.m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}

	[HarmonyPatch(typeof(Player), "GetStatWithoutSkills")]
	class HarmonyPatch_Player_GetStatWithoutSkills {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (Settings.m_enabled.Value) {
					__result += Settings.m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}
}