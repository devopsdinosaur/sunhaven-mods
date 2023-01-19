using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.action_speed", "Action Speed", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.action_speed");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_action_speed;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.action_speed v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_action_speed = this.Config.Bind<float>("General", "Action Speed", 2f, "Base action/attack speed for tools and weapons (float, 1 = game default, 2 = mod default [double speed], anything higher = super speed!)");
	}

	[HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result) {
			if (m_enabled.Value && stat == StatType.AttackSpeed) {
				__result = m_action_speed.Value;
				return false;
			}
			return true;
		}
	}
}