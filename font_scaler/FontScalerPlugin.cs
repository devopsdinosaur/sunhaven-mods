using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using TMPro;

[BepInPlugin("devopsdinosaur.sunhaven.font_scaler", "Font Scaler", "0.0.2")]
public class FontScalerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.font_scaler");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_font_scale_factor;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_font_scale_factor = this.Config.Bind<float>("General", "Base Font Size Multiplier", 1.25f, "Float value multiplied times base font size to increase/decrease all text size (float).");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.font_scaler v0.0.2" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
	class HarmonyPatch_TextMeshProUGUI_Awake {

		private static void Postfix(TextMeshProUGUI __instance) {
			try {
				__instance.fontSize *= m_font_scale_factor.Value;
				__instance.fontSizeMax *= m_font_scale_factor.Value;
				__instance.fontSizeMin *= m_font_scale_factor.Value;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_TextMeshProUGUI_Awake.Postfix ERROR - " + e);
			}
		}
	}
}