using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.speed_boost", "Speed Boost", "0.0.1")]
public class SpeedBoostPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.speed_boost");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_movement_speed;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.speed_boost v0.0.1 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_movement_speed = this.Config.Bind<float>("General", "Movement Speed", 0.75f, "Base movement speed before applied perks (float, 0.75f = fast, 1f+ super speed!)");
		this.m_harmony.PatchAll();
    }

    [HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result) {
			if (!m_enabled.Value || stat != StatType.Movespeed) {
				return true;
			}
			__result = m_movement_speed.Value;
			if (GameSave.Exploration.GetNode("Exploration2a")) {
				__result += 0.02f + 0.02f * (float) GameSave.Exploration.GetNodeAmount("Exploration2a");
			}
			if (Player.Instance.Mounted && GameSave.Exploration.GetNode("Exploration8a")) {
				__result += 0.04f * (float) GameSave.Exploration.GetNodeAmount("Exploration8a");
			}
			if (GameSave.Exploration.GetNode("Exploration5a") && SingletonBehaviour<TileManager>.Instance.GetTileInfo(Player.Instance.Position) != 0) {
				__result += 0.05f + 0.05f * (float) GameSave.Exploration.GetNodeAmount("Exploration5a");
			}
			if (GameSave.Exploration.GetNode("Exploration6a") && Time.time < Player.Instance.lastPickupTime + 3.5f) {
				__result += 0.1f * (float) GameSave.Exploration.GetNodeAmount("Exploration6a");
			}
			if (Time.time < Player.Instance.lastPickaxeTime + 2.5f) {
				__result += Player.Instance.MiningStats.GetStat(StatType.MovementSpeedAfterRock);
			}
			return false;
		}
	}

}