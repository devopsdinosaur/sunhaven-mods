
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using TMPro;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Mirror;
using UnityEngine.UI;
using QFSW.QC;
using System.Diagnostics;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class DDDebuggingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enable_cheats;

	private void Awake() {
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		m_enable_cheats = this.Config.Bind<bool>("General", "Enable Cheats", true, "Determines whether console cheats are enabled (without that weird key combination thingy)");
		this.m_harmony.PatchAll();
		
		foreach (string key in BepInEx.Bootstrap.Chainloader.PluginInfos.Keys) {
			PluginInfo plugin_info = BepInEx.Bootstrap.Chainloader.PluginInfos[key];
			logger.LogInfo(key + " - " + plugin_info.ToString());
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

    [HarmonyPatch(typeof(PlayerSettings), "Initialize")]
    class HarmonyPatch_PlayerSettings_Initialize {

        private static void Postfix(PlayerSettings __instance) {
            __instance.SetCheatsEnabled(true);
        }
    }

    [HarmonyPatch(typeof(Player), "RequestSleep")]
	class HarmonyPatch_Player_RequestSleep {

		private static bool Prefix(Player __instance, Bed bed, ref bool ____paused, ref UnityAction ___OnUnpausePlayer) {
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
			}});
			____paused = true;
			___OnUnpausePlayer = (UnityAction) Delegate.Combine(___OnUnpausePlayer, (UnityAction) delegate {
				DialogueController.Instance.CancelDialogue();
			});
			return false;
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

}