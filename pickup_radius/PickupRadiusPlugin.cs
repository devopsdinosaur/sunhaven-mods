using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;

[BepInPlugin("devopsdinosaur.sunhaven.pickup_radius", "Pickup Radius", "0.0.4")]
public class PickupRadiusPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.pickup_radius");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_pickup_radius;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_pickup_radius = this.Config.Bind<float>("General", "Pickup Radius", 10f, "Range beyond player to suck up items (float, 0 = will pick up nothing, anything >= 1f is a boost (10f recommended); the mining perk [Long Arm] is ignored, so drop that one)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.pickup_radius v0.0.4" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}
	
	[HarmonyPatch(typeof(Pickup), "Awake")]
	class HarmonyPatch_Pickup_Awake {

		private static void Postfix(ref float ___radius) {
			if (m_enabled.Value) {
				___radius = m_pickup_radius.Value;
			}
		}
	}
}