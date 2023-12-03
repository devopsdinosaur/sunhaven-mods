using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using QFSW.QC;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using ZeroFormatter;
using UnityEngine.SceneManagement;


[BepInPlugin("devopsdinosaur.sunhaven.font_scaler", "Font Scaler", "0.0.1")]
public class ActionSpeedPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.font_scaler");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_font_scale_factor;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_font_scale_factor = this.Config.Bind<float>("General", "Font Size Multiplier", 1.5f, "Float value multiplied times base font size to increase/decrease all text size.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.font_scaler v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(GameManager), "LateUpdate")]
	class HarmonyPatch_GameManager_Update {
		 
		const float CHECK_FREQUENCY = 5.0f;
		static float m_elapsed = CHECK_FREQUENCY;
		static Dictionary<int, bool> m_modified = new Dictionary<int, bool>();
		
		private static bool Prefix() {
			try {
				if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
					return true;
				}
				m_elapsed = 0f;
				foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>()) {
					if (m_modified.ContainsKey(obj.GetHashCode())) {
						continue;
					}
					m_modified[obj.GetHashCode()] = true;
					foreach (Component component in obj.GetComponents<Component>()) {
						if (component is TMPro.TMP_Text text) {
							text.fontSize *= m_font_scale_factor.Value;
						}
					}
				}

			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Water), "Start")]
	class HarmonyPatch_Water_Start {
		private static void Postfix(Water __instance, Material ____liquidMaterial, LiquidType ___liquidType) {
			GameObject.Destroy(__instance.transform.GetComponent<BoxCollider2D>());
		}
	}

}