
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	/*
	[HarmonyPatch(typeof(NPCManager), "FixedUpdate")]
	class HarmonyPatch_NPCManager_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = 0f;
		static bool m_done = false;

		private static bool Prefix(ref NPCManager __instance, bool ___initialized, Dictionary<string, NPCAI> ____npcs) {
			if ((m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return true;
			}
			m_elapsed = 0f;
			if (SceneManager.sceneCount < 2 || !___initialized || GameManager.ApplicationQuitting) {
				return true;
			}
			if (m_done) {
				return true;
			}
			foreach (NPCAI npc in ____npcs.Values) {
				logger.LogInfo(npc.ActualNPCName);
			}
			m_done = true;
			return true;
		}
	}
	*/
}