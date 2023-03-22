using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System.Collections.Generic;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.player_stats", "Player Stats", "0.0.1")]
public class PlayerStatsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.player_stats");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static Dictionary<StatType, ConfigEntry<float>> m_stats;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.player_stats v0.0.1 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_stats = new Dictionary<StatType, ConfigEntry<float>>();
		foreach (string stat_name in System.Enum.GetNames(typeof(StatType))) {
			m_stats[(StatType) System.Enum.Parse(typeof(StatType), stat_name)] = this.Config.Bind<float>("General", "Delta " + stat_name, 0f, "[float] Amount to increment/decrement the '" + stat_name + "' player stat (only during gameplay with mod enabled; not permanent).");
		}
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Player), "GetStat")]
	class HarmonyPatch_Player_GetStat {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (m_enabled.Value) {
					__result += m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}

	[HarmonyPatch(typeof(Player), "GetMyStat")]
	class HarmonyPatch_Player_GetMyStat {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (m_enabled.Value) {
					__result += m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}

	[HarmonyPatch(typeof(Player), "GetStatWithoutSkills")]
	class HarmonyPatch_Player_GetStatWithoutSkills {

		private static void Postfix(Player __instance, ref float __result, StatType stat) {
			try {
				if (m_enabled.Value) {
					__result += m_stats[stat].Value;
				}
			} catch (Exception) {
			}
		}
	}
}