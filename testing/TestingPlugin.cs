using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using QFSW.QC;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using ZeroFormatter;
using UnityEngine.SceneManagement;
using PSS;

[BepInPlugin("devopsdinosaur.sunhaven.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.testing");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent) {
		Transform child;
		string indent_string = "";
		for (int counter = 0; counter < indent; counter++) {
			indent_string += " => ";
		}
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			logger.LogInfo(indent_string + child.gameObject.name);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			list_descendants(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_descendants(child, callback);
		}
		return true;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

    [HarmonyPatch(typeof(PlayerSettings), "SetCheatsEnabled")]
    class HarmonyPatch_PlayerSettings_SetCheatsEnabled {

        private static bool Prefix(ref bool enable) {
			logger.LogInfo("Enabling cheats.");
            enable = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "RequestSleep")]
	class HarmonyPatch_Player_RequestSleep {

		private static bool Prefix(Player __instance, Bed bed, ref bool ____paused, ref UnityAction ___OnUnpausePlayer) {
			try {
				DialogueController.Instance.SetDefaultBox();
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> { "Would you like to sleep?" },
					responses = new Dictionary<int, Response> {{
						0,
						new Response
						{
							responseText = () => "Yes",
							action = delegate {
								__instance.GetType().GetTypeInfo().GetMethod("StartSleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {bed});
							}
						}
					}, {
						1,
						new Response
						{
							responseText = () => "No",
							action = delegate {
								DialogueController.Instance.CancelDialogue(animate: true, null, showActionBar: true);
							}
						}
					}
				}
				});
				____paused = true;
				___OnUnpausePlayer = (UnityAction) Delegate.Combine(___OnUnpausePlayer, (UnityAction) delegate {
					DialogueController.Instance.CancelDialogue();
				});
				return false;
			} catch (Exception e) { 
				logger.LogError("** HarmonyPatch_Player_RequestSleep.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LiamWheat), "ReceiveDamage")]
	class HarmonyPatch_LiamWheat_ReceiveDamage {

		private static bool Prefix(ref LiamWheat __instance, ref DamageHit __result) {
			AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.cropHit, __instance.transform.position);
			UnityEngine.Object.Destroy(__instance.gameObject);
			__result = new DamageHit {
				hit = true,
				damageTaken = 1f
			};
			Pickup.Spawn(
				__instance.transform.position.x + 0.5f, 
				__instance.transform.position.y + 0.707106769f, 
				__instance.transform.position.z, 
				ItemID.Wheat
			);
			return false;
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		private static bool Prefix() {
			try {
				if (Input.GetKeyDown(KeyCode.F11)) {
					//((ItemIcon) typeof(ItemIcon).GetField("_currentHoveredIcon", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null))?.DropIcon();
					Inventory.CurrentItemIcon.DropIcon();
				}
			} catch (Exception e) {
				logger.LogError("** ERROR - " + e);
			}
			return true;
		}
	}
	
	/*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {

		private static bool Prefix() {
			try {

				return false;
			} catch (Exception e) {
				logger.LogError("** ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {

		private static void Postfix() {
			try {

				
			} catch (Exception e) {
				logger.LogError("** ERROR - " + e);
			}
			return true;
		}
	}
	*/
}