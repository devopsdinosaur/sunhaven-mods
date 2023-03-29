
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Reflection;
using System.Collections.Generic;
using DG.Tweening;


[BepInPlugin("devopsdinosaur.sunhaven.designated_driver", "Designated Driver", "0.0.2")]
public class DesignatedDriverPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.designated_driver");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_harmony.PatchAll();
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			logger.LogInfo((object) "devopsdinosaur.sunhaven.designated_driver v0.0.2 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}
	
	[HarmonyPatch(typeof(Player), "PassOut")]
	class HarmonyPatch_Player_PassOut {

		private static bool Prefix(Player __instance, HashSet<string> ____pauseObjects) {
			if (!m_enabled.Value || __instance.Sleeping) {
				return true;
			}
			DialogueController.Instance.CancelDialogue(animate: false);
			UIHandler.Instance?.CloseExternalUI();
			____pauseObjects.Clear();
			__instance.AddPauseObject("passout");
			DialogueController.Instance.SetDefaultBox();
			AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.passOutSound);
			Cart.currentRoom = 0;
			Cart.rewardRoom = "";
			CombatDungeon.CurrentFloor = 0;
			Utilities.UnlockAcheivement(83);
			Player.onPassOut?.Invoke();
			DialogueController.Instance.PushDialogue(
				new DialogueNode {
					dialogueText = new List<string> {"You passed out, but your invisible friend brought you home.  What a pal!"}
				}, delegate {
					__instance.GetType().GetTypeInfo().GetMethod("Sleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				}
			);
			return false;
		}
	}
}