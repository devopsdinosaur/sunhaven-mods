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


[BepInPlugin("devopsdinosaur.sunhaven.zone_info", "Zone Info", "0.0.3")]
public class ZoneInfoPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.zone_info");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_show_coordinates;
	private static ConfigEntry<bool> m_show_birthday;
	private const int ZONE_NAME = 1;
	private const int PLAYER_POS = 2;
	private const int BIRTHDAY = 3;
	private static InfoLabelList m_info_labels = null;
	private static Dictionary<string, NPCGiftTable> m_birthdays = new Dictionary<string, NPCGiftTable>();

	private class InfoLabelList {

		private class InfoLabel {

			private GameObject m_preceding_obj = null;
			private GameObject m_info_obj = null;
			public GameObject game_object => m_info_obj;
			private TextMeshProUGUI m_info_tmp = null;

			public static InfoLabel create(GameObject preceding_obj) {
				try {
					InfoLabel instance = new InfoLabel();
					instance.m_preceding_obj = preceding_obj;
					instance.m_info_obj = GameObject.Instantiate(instance.m_preceding_obj, instance.m_preceding_obj.transform.parent);
					instance.m_info_tmp = instance.m_info_obj.GetComponent<TextMeshProUGUI>();
					instance.m_info_tmp.enableAutoSizing = true;
					instance.m_info_tmp.enableWordWrapping = false;
					return instance;
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

		private GameObject m_preceding_obj = null;
		private Dictionary<int, InfoLabel> m_labels = new Dictionary<int, InfoLabel>();

		public InfoLabelList(GameObject preceding_obj) {
			this.m_preceding_obj = preceding_obj;
		}

		public void add_label(int key) {
			InfoLabel label = InfoLabel.create(m_preceding_obj);
			if (label == null) {
				return;
			}
			this.m_labels[key] = label;
			this.m_preceding_obj = label.game_object;
		}

		public void update(int key, string text) {
			if (this.m_labels.ContainsKey(key)) {
				this.m_labels[key].update(text);
			}
		}
	}

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_show_coordinates = this.Config.Bind<bool>("General", "Show Player Coordinates", true, "Set to false to hide player coordinates (may be distracting or annoying for some people).");
			m_show_birthday = this.Config.Bind<bool>("General", "Show NPC Birthday Info", true, "If true then this will display the name of the NPC that has a birthday today, if any.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.zone_info v0.0.3" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(NPCAI), "Awake")]
	class HarmonyPatch_NPCAI_Awake {

		private static void Postfix(NPCAI __instance, NPCGiftTable ___giftTable) {
			if (___giftTable != null) {
				m_birthdays[__instance.ActualNPCName] = ___giftTable;
			}
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;
		

		private static bool Prefix(Player __instance) {

			void ensure_labels() {
				try {
					if (m_info_labels != null) {
						return;
					}
					TextMeshProUGUI m_time_tmp = (TextMeshProUGUI) DayCycle.Instance.GetType().GetTypeInfo().GetField("_timeTMP", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DayCycle.Instance);
					m_info_labels = new InfoLabelList(m_time_tmp.gameObject);
					m_info_labels.add_label(ZONE_NAME);
					if (m_show_coordinates.Value) {
						m_info_labels.add_label(PLAYER_POS);
					}
					if (m_show_birthday.Value) {
						m_info_labels.add_label(BIRTHDAY);
					}
				} catch {
				}
			}

			try {
				if (!m_enabled.Value || __instance.IsOwner && (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
					return true;
				}
				m_elapsed = 0f;
				ensure_labels();
				m_info_labels.update(ZONE_NAME, ScenePortalManager.ActiveSceneName);
				if (m_show_coordinates.Value) {
					Vector2 pos = Player.Instance.ExactPosition;
					m_info_labels.update(PLAYER_POS, "X: " + pos.x.FormatToTwoDecimal() + ", Y: " + pos.y.FormatToTwoDecimal());
				}
				if (m_show_birthday.Value) {
					string text = "No birthdays today";
					foreach (string name in m_birthdays.Keys) {
						if (m_birthdays[name].birthMonth == DayCycle.Instance.Season && m_birthdays[name].birthDay == DayCycle.MonthDay) {
							text = "<color=green>" + name + "'s Birthday!";
							break;
						}
					}
					m_info_labels.update(BIRTHDAY, text);
				}
			} catch {
			}
			return true;
		}
	}
}