using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using TMPro;
using System.Reflection;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.quality_of_life", "Quality of Life", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.quality_of_life");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.quality_of_life v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;
		private static GameObject m_mine_info_obj = null;
		private static TextMeshProUGUI m_mine_info_tmp = null;

		private static bool Prefix() {
			if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return true;
			}
			m_elapsed = 0f;
			TextMeshProUGUI _timeTMP = (TextMeshProUGUI) DayCycle.Instance.GetType().GetTypeInfo().GetField("_timeTMP", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DayCycle.Instance);
			if (m_mine_info_obj == null) {
				m_mine_info_obj = GameObject.Instantiate(_timeTMP.gameObject, _timeTMP.gameObject.transform.parent);
			}
			m_mine_info_obj.transform.position = _timeTMP.transform.position + Vector3.down * _timeTMP.GetComponent<RectTransform>().rect.height * 2;
			m_mine_info_tmp = m_mine_info_obj.GetComponent<TextMeshProUGUI>();
			m_mine_info_tmp.text = SceneSettingsManager.Instance.GetCurrentSceneSettings.sceneName;
			return true;
		}
	}
}