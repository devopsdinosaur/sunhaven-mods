using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.craft_speed", "Craft Speed", "0.0.2")]
public class CraftSpeedPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.craft_speed");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_craft_speed;
	
	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.craft_speed v0.0.2 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_craft_speed = this.Config.Bind<float>("General", "Craft Speed Multiplier", 10f, "Speed multiplier for item crafting (float, 1 = game default (1.2 for humans) [note: this stomps the human 20% passive; should not affect anything else])");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static void Postfix(CraftingTable __instance, ref float ___craftSpeedMultiplier) {
			if (m_enabled.Value) {
				//logger.LogInfo(__instance.name);
				___craftSpeedMultiplier = m_craft_speed.Value;
			}
		}
	}
}