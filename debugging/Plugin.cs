
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

		public void adjust_dayspeed_slider(float value, Slider slider, TextMeshProUGUI label) {
			slider.minValue = 0f;
			slider.maxValue = MAX_TICK;
			slider.value = (value < TICK ? 0f : value / MAX_VALUE);
			label.text = "Blah";
		}
	}

	[HarmonyPatch(typeof(PlayerSettings), "SetupUI")]
	class HarmonyPatch_PlayerSettings_SetupUI {

		/*
		private static bool Prefix(
			ref PlayerSettings __instance,
			ref Slider ____masterVolumeSlider,
			ref Slider ____musicVolumeSlider,
			ref Slider ____soundEffectsVolumeSlider,
			ref Slider ____ambientVolumeSlider,
			ref TextMeshProUGUI ___masterVolumeTMP,
			ref TextMeshProUGUI ___musicVolumeTMP,
			ref TextMeshProUGUI ___soundEffectsVolumeTMP,
			ref TextMeshProUGUI ___ambientVolumeTMP,
			ref Slider ___zoomSlider,
			ref TextMeshProUGUI ___zoomTMP,
			ref Slider ___daySpeedSlider,
			ref TextMeshProUGUI ___daySpeedTMP,
			ref Toggle ___fullscreenToggle,
			ref Toggle ___pauseDuringDialogue,
			ref Toggle ___skipTutorials,
			ref int ___targetFrameRate,
			ref Resolution[] ___resolutions,
			TMP_Dropdown ____resolutionDropdown
		) {

			//return true;

			//___daySpeedSlider.maxValue = 10;
			____masterVolumeSlider.value = Settings.MasterAudioLevel;
			____musicVolumeSlider.value = Settings.MusicAudioLevel;
			____soundEffectsVolumeSlider.value = Settings.SoundEffectsAudioLevel;
			____ambientVolumeSlider.value = Settings.AmbientSoundAudioLevel;
			___masterVolumeTMP.text = Settings.MasterAudioLevel.FormatToPercentage();
			___musicVolumeTMP.text = Settings.MusicAudioLevel.FormatToPercentage();
			___soundEffectsVolumeTMP.text = Settings.SoundEffectsAudioLevel.FormatToPercentage();
			___ambientVolumeTMP.text = Settings.AmbientSoundAudioLevel.FormatToPercentage();
			___zoomSlider.value = Mathf.InverseLerp(4f, 2f, Settings.Zoom);
			___zoomTMP.text = Settings.Zoom + "x";
			___daySpeedSlider.value = Mathf.InverseLerp(1f, 4f, Settings.DaySpeed);
			___daySpeedTMP.text = "Who knows!?"; //GetTimeByDaySpeed();
			___fullscreenToggle.isOn = Settings.Resolution.z == 1;
			___pauseDuringDialogue.SetIsOnWithoutNotify(Settings.PauseDuringDialogue);
			___skipTutorials.SetIsOnWithoutNotify(Settings.SkipTutorials);
			__instance.GetType().GetTypeInfo().GetMethod("SetCheatsEnabled", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
			___targetFrameRate = Settings.Resolution.w;
			Application.targetFrameRate = ___targetFrameRate;
			___resolutions = Screen.resolutions;
			____resolutionDropdown.ClearOptions();
			List<string> list = new List<string>();
			int value = 0;
			for (int i = 0; i < ___resolutions.Length; i++) {
				string item = ___resolutions[i].width + " x " + ___resolutions[i].height + " (" + ___resolutions[i].refreshRate + ")";
				list.Add(item);
				if (___resolutions[i].width == Screen.width && ___resolutions[i].height == Screen.height) {
					value = i;
				}
			}
			____resolutionDropdown.AddOptions(list);
			____resolutionDropdown.RefreshShownValue();
			____resolutionDropdown.value = value;
			__instance.GetType().GetTypeInfo().GetMethod("SetupKeybindButtons", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
			__instance.CloseKeyBindsPanel();
			__instance.CloseConfirmCheatMenu();
			Settings.WriteSettingsToFile();
			return false;
		}
		*/

		private static void Postfix(
			ref PlayerSettings __instance,
			ref Slider ___daySpeedSlider,
			ref TextMeshProUGUI ___daySpeedTMP
		) {
			SimulationClock.Instance.adjust_dayspeed_slider(2f, ___daySpeedSlider, ___daySpeedTMP);
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
			SimulationClock.Instance.adjust_dayspeed_slider(value, ___daySpeedSlider, ___daySpeedTMP);
			return false;
		}
	}
}