using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using UnityEngine.EventSystems;


[BepInPlugin("devopsdinosaur.sunhaven.mouseover_tooltip", "Mouseover Tooltip", "0.0.1")]
public class MouseoverTooltipPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.mouseover_tooltip");
	public static ManualLogSource logger;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.mouseover_tooltip v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(MouseAndControllerInputModule), "ProcessMouseEvent")]
	class HarmonyPatch_MouseAndControllerInputModule_ProcessMouseEvent {

		static GameObject m_prev_object = null;

		private static void Postfix(GameObject ___m_CurrentFocusedGameObject) {
			logger.LogInfo("HarmonyPatch_MouseAndControllerInputModule_ProcessMouseEvent");
			if (___m_CurrentFocusedGameObject == null || ___m_CurrentFocusedGameObject == m_prev_object) {
				return;
			}
			m_prev_object = ___m_CurrentFocusedGameObject;
			logger.LogInfo(___m_CurrentFocusedGameObject);
		}
	}

}