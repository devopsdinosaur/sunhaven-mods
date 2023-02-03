
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


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enable_cheats;


	public Plugin() {
	}

	private static bool enum_descendants_callback(Transform transform) {
		logger.LogInfo(transform);
		return true;
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enable_cheats = this.Config.Bind<bool>("General", "Enable Cheats", true, "Determines whether console cheats are enabled (without that weird key combination thingy)");
		
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

	// ======================================================================================
	// Stuff for granting all race implicits
	// ======================================================================================

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static bool Prefix(ref float ___craftSpeedMultiplier) {
			// the CraftSpeedMultiplier property multiplies this by 1.2 for humans,
			// so this grants the 20% to the other races
			___craftSpeedMultiplier = (GameSave.CurrentCharacter.race != (int) Race.Human ? 1.2f : 1f);
			return true;
		}
	}

	[HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result, ref float ___lastJumpTimer) {
			if (stat == StatType.CrossbowPower) {
				__result = 0f;
				if (GameSave.Combat.GetNode("Combat3b") && Time.time <= ___lastJumpTimer + 2f) {
					__result += (float) GameSave.Combat.GetNodeAmount("Combat3b") * 0.05f;
				}
				__result += 0.1f;
				return false;
			}
			return true;
		}
	}

	// ======================================================================================
	// ======================================================================================

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

	public class SimulationClock {

		private static SimulationClock m_instance = null;
		public static SimulationClock Instance {
			get {
				if (m_instance == null) {
					m_instance = new SimulationClock();
				}
				return m_instance;
			}
		}
		public const float TICK = 0.05f;
		public const float MAX_VALUE = 20f;
		public const float MAX_TICK = MAX_VALUE / TICK;

		//public void create_settings_slider

		public void adjust_dayspeed_slider(float value, Slider slider, TextMeshProUGUI label) {
			slider.minValue = 0f;
			slider.maxValue = MAX_TICK;
			slider.value = (value < TICK ? 0f : value / MAX_VALUE);
			label.text = "Blah";
		}
	}

	[HarmonyPatch(typeof(PlayerSettings), "SetupUI")]
	class HarmonyPatch_PlayerSettings_SetupUI {

		private static void Postfix(
			ref PlayerSettings __instance,
			ref Slider ___daySpeedSlider,
			ref TextMeshProUGUI ___daySpeedTMP
		) {
			___daySpeedTMP.color = new Color(1, 0, 0);
			//SimulationClock.Instance.adjust_dayspeed_slider(2f, ___daySpeedSlider, ___daySpeedTMP);
		}
	}

	[HarmonyPatch(typeof(PlayerSettings), "SetDaySpeed")]
	class HarmonyPatch_PlayerSettings_SetDaySpeed {

		private static bool Prefix(
			ref float value,
			ref PlayerSettings __instance,
			ref Slider ___daySpeedSlider,
			ref TextMeshProUGUI ___daySpeedTMP
		) {
			//SimulationClock.Instance.adjust_dayspeed_slider(value, ___daySpeedSlider, ___daySpeedTMP);
			return true;
		}
	}
}