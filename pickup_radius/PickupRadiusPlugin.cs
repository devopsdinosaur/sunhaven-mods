using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;

public static class PluginInfo {

	public const string TITLE = "Pickup Radius";
	public const string NAME = "pickup_radius";

	public const string VERSION = "0.0.6";
	public static string[] CHANGELOG = new string[] {
		"v0.0.6 - Fixed issue causing player to get stuck when fishing",
		"v0.0.5 - Fixed issue causing instant spawn code to not work"
	};

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME = "sunhaven";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class PickupRadiusPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony(PluginInfo.GUID);
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_pickup_radius;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_pickup_radius = this.Config.Bind<float>("General", "Pickup Radius", 10f, "Range beyond player to suck up items (float, 0 = will pick up nothing, anything >= 1f is a boost (10f recommended); the mining perk [Long Arm] is ignored, so drop that one)");
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
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

	[HarmonyPatch(typeof(Pickup), "SpawnInternal", new Type[] {typeof(float), typeof(float), typeof(float), typeof(Item), typeof(int), typeof(bool), typeof(float), typeof(Pickup.BounceAnimation), typeof(float), typeof(float), typeof(int), typeof(short)})]
	class HarmonyPatch_Pickup_SpawnInternal {

		private static bool Prefix(ref bool homeIn, ref Pickup.BounceAnimation bounceAnimation, ref float pickupTime) {
			if (m_enabled.Value && bounceAnimation != Pickup.BounceAnimation.Fish) {
				homeIn = false;
				bounceAnimation = Pickup.BounceAnimation.Normal;
				pickupTime = 0;
			}
			return true;
		}
	}
}