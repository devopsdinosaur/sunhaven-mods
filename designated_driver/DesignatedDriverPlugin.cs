﻿
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.designated_driver", "Designated Driver", "0.0.4")]
public class DesignatedDriverPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.designated_driver");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_cheat_death;

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_harmony.PatchAll();
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_cheat_death = this.Config.Bind<bool>("General", "No Death Penalty", false, "Your invisible buddy whisks you home just before death.");
			logger.LogInfo((object) "devopsdinosaur.sunhaven.designated_driver v0.0.4 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}
	
	[HarmonyPatch(typeof(Player), "PassOut")]
	class HarmonyPatch_Player_PassOut {

		private static bool Prefix(Player __instance, HashSet<string> ____pauseObjects) {
			try {
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
                __instance.EndInteraction();
                DialogueController.Instance.PushDialogue(
					new DialogueNode {
						dialogueText = new List<string> {"You passed out, but your invisible friend brought you home.  What a pal!"}
					}, delegate {
						__instance.GetType().GetTypeInfo().GetMethod("Sleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
					}
				);
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_PassOut.Prefix ERROR - " + e);
			}
			return true;
		}
	}
	
    [HarmonyPatch(typeof(Player), "Die")]
	class HarmonyPatch_Player_Die {

		private static bool Prefix(Player __instance) {
			try {
				if (!(m_enabled.Value && m_cheat_death.Value)) {
					return true;
				}
				if (__instance.Dying) {
					return false;
				}
				Cart.currentRoom = 0;
				Cart.rewardRoom = "";
				if (PlaySettingsManager.PlaySettings.allowDeath) {
					if (CombatDungeon.CurrentFloor > 0) {
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(136.48f, 193.92f), "CombatDungeonEntrance", delegate {
							__instance.RemovePauseObject("death");
							__instance.Health = __instance.MaxHealth / 2f;
                            __instance.GetType().GetProperty("Dying", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty).SetValue(__instance, false);
                            __instance.overrideFacingDirection = false;
							__instance.facingDirection = Direction.South;
							__instance.Invincible = false;
							__instance.GetType().GetMethod("AddDeath", BindingFlags.Instance| BindingFlags.NonPublic).Invoke(__instance, new object[] { });
							__instance.partyState = 0;
						}, null, null, SceneFadeType.Fade, 2.5f);
					} else {
						__instance.PassOut();
					}
                } else {
					__instance.Health = __instance.MaxHealth;
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Die_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}