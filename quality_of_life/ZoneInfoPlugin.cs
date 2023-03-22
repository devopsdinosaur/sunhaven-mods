using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using TMPro;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.zone_info", "Zone Info", "0.0.1")]
public class ZoneInfoPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.zone_info");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_show_coordinates;
	private const int ZONE_NAME = 1;
	private const int PLAYER_POS = 2;
	private static Dictionary<int, InfoLabel> m_info_labels = new Dictionary<int, InfoLabel>();


	private class InfoLabel {

		private int m_key;
		private GameObject m_preceding_obj = null;
		private GameObject m_info_obj = null;
		public GameObject game_object => m_info_obj;
		private TextMeshProUGUI m_info_tmp = null;

		public static InfoLabel create(int key, GameObject preceding_obj) {
			try {
				InfoLabel instance = new InfoLabel();
				instance.m_key = key;
				instance.m_preceding_obj = preceding_obj;
				instance.m_info_obj = GameObject.Instantiate(instance.m_preceding_obj, instance.m_preceding_obj.transform.parent);
				instance.m_info_tmp = instance.m_info_obj.GetComponent<TextMeshProUGUI>();
				instance.m_info_tmp.enableAutoSizing = true;
				instance.m_info_tmp.enableWordWrapping = false;
				return (m_info_labels[key] = instance);
			} catch {
			}
			return null;
		}

		public void update(string text) {
			if (this.m_info_tmp == null) {
				return;
			}
			this.m_info_obj.transform.position = this.m_preceding_obj.transform.position + Vector3.down * this.m_preceding_obj.GetComponent<RectTransform>().rect.height * 2;
			this.m_info_tmp.text = text;
		}
	}


	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.zone_info v0.0.1 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_show_coordinates = this.Config.Bind<bool>("General", "Show Player Coordinates", true, "Set to false to hide player coordinates (may be distracting or annoying for some people).");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;
		

		private static bool Prefix() {

			void ensure_labels() {
				try {
					if (m_info_labels.Keys.Count != 0) {
						return;
					}
					TextMeshProUGUI m_time_tmp = (TextMeshProUGUI) DayCycle.Instance.GetType().GetTypeInfo().GetField("_timeTMP", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DayCycle.Instance);
					InfoLabel.create(ZONE_NAME, m_time_tmp.gameObject);
					InfoLabel.create(PLAYER_POS, m_info_labels[ZONE_NAME].game_object);
				} catch {
				}
			}

			try {
				if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
					return true;
				}
				m_elapsed = 0f;
				ensure_labels();
				m_info_labels[ZONE_NAME].update(ScenePortalManager.ActiveSceneName);
				if (m_show_coordinates.Value) {
					Vector2 pos = Player.Instance.ExactPosition;
					m_info_labels[PLAYER_POS].update("X: " + pos.x.FormatToTwoDecimal() + ", Y: " + pos.y.FormatToTwoDecimal());
				}
			} catch {
				// ignorable nullref exceptions will get thrown for a bit when game is starting/dying/in menu
			}
			return true;
		}
	}
}