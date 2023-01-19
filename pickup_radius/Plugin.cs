using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.pickup_radius", "Pickup Radius", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.pickup_radius");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_pickup_radius;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.pickup_radius v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_pickup_radius = this.Config.Bind<float>("General", "Pickup Radius", 10f, "Range beyond player to suck up items (float, 0 = will pick up nothing, anything >= 1f is a boost (10f recommended); the mining perk [Long Arm] is ignored, so drop that one)");
	}

	[HarmonyPatch(typeof(Pickup), "Spawn", new Type[] {
		typeof(float), typeof(float), typeof(float),
		typeof(Item), typeof(int), typeof(bool),
		typeof(float), typeof(Pickup.BounceAnimation),
		typeof(float), typeof(float), typeof(bool)
	})]
	class HarmonyPatch_Pickup_Spawn {

		private static bool Prefix(
			float x, float y, float z,
			Item item, int amount, bool homeIn,
			float homeInDelay, ref Pickup.BounceAnimation bounceAnimation,
			ref float pickupTime, float spawnForce, bool localOnly
		) {
			if (m_enabled.Value && bounceAnimation == Pickup.BounceAnimation.Fall) {
				bounceAnimation = Pickup.BounceAnimation.Normal;
				pickupTime = 0f;
			}
			return true;
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