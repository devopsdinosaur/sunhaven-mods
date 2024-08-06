using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;

[BepInPlugin("devopsdinosaur.sunhaven.sound_manager", "Sound Manager", "0.0.1")]
public class SoundManagerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.sound_manager");
	public static ManualLogSource logger;

	class ConfigDef<T> {
		public string category;
		public string key;
		public string description;
		public T default_value;
		public ConfigEntry<T> config;
	}

	enum ConfigId {
		ID_SILENCE_SKILL_NODE_ROLLOVER
	};

	private static Dictionary<ConfigId, ConfigDef<bool>> m_bool_configs = new Dictionary<ConfigId, ConfigDef<bool>>() {
		{ConfigId.ID_SILENCE_SKILL_NODE_ROLLOVER, new ConfigDef<bool>() {
			category = "Silence",
			key = "Silence - Skill Node Rollover",
			default_value = false,
			description = "Silence the dings when moving cursor over skill tree nodes"
		}},
	};

	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			foreach (KeyValuePair<ConfigId, ConfigDef<bool>> item in m_bool_configs) {
				item.Value.config = this.Config.Bind<bool>(item.Value.category, item.Value.key, item.Value.default_value, item.Value.description);
			}
			this.m_harmony.PatchAll();
			logger.LogInfo("devopsdinosaur.sunhaven.sound_manager v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(UISoundElement), "OnPointerEnter")]
	class HarmonyPatch_UISoundElement_OnPointerEnter {

		private static bool Prefix(UISoundElement __instance) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				if (m_bool_configs[ConfigId.ID_SILENCE_SKILL_NODE_ROLLOVER].config.Value && __instance.gameObject.name.StartsWith("_node(Clone)")) {
					return false;
				}
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_UISoundElement_OnPointerEnter.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}