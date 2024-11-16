using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class PluginInfo {

	public const string TITLE = "Sound Manager";
	public const string NAME = "sound_manager";
	public const string SHORT_DESCRIPTION = "Adds more granularity to sound control. Silence or increase/decrease volume of specific game sounds. Add custom sounds.";

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
public class SoundManagerPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(UISoundElement), "OnPointerEnter")]
	class HarmonyPatch_UISoundElement_OnPointerEnter {

		private static bool Prefix(UISoundElement __instance) {
			try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				if (Settings.m_bool_configs[Settings.ConfigId.ID_SILENCE_SKILL_NODE_ROLLOVER].config.Value && __instance.gameObject.name.StartsWith("_node(Clone)")) {
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