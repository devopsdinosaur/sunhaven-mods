using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Reflection;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.earless", "Earless", "0.0.1")]
public class EarlessPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.earless");
	public static ManualLogSource logger;

	private static string[] LAYERS = {
		"head",
        "topArms",
        "bottomArm",
        "leg",
        "chest",
        "mouth",
        "eye",
        "gloves",
        "backGloves",
        "sleeves",
        "backSleeves",
        "hat",
        "hair",
        "ears",
        "chestArmor",
        "pants",
        "tail",
        "back",
        "overlay",
        "face",
        "hatGlow",
        "eyeGlow",
        "wingGlow",
        "hairGlow"
	};

	private static ConfigEntry<bool> m_enabled;
	private static Dictionary<string, ConfigEntry<bool>> m_hidden_layers;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_hidden_layers = new Dictionary<string, ConfigEntry<bool>>();
			foreach (string key in LAYERS) {
				m_hidden_layers[key] = this.Config.Bind<bool>("General", "Hide " + key + " Layer", false, "Set to true to hide the '" + key + "' sprite layer.");
			}
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.earless v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(PlayerAnimationLayers), "UpdateBodyPart")]
	class HarmonyPatch_PlayerAnimationLayers_UpdateBodyPart {

		private static bool Prefix(PlayerAnimationLayers __instance, MeshGenerator renderer, ref int index) {
			foreach (string key in m_hidden_layers.Keys) {
				if (!m_hidden_layers[key].Value) {
					continue;
				}
				if (renderer == (MeshGenerator) __instance.GetType().GetField("_" + key, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance)) {
					index = -1;
					return true;
				}
			}
			return true;
		}
	}
}