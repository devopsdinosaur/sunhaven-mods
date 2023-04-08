
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;
using System.Reflection;
using System.Diagnostics;
using DG.Tweening;


[BepInPlugin("devopsdinosaur.sunhaven.time_management", "Time Management", "0.0.9")]
public class TimeManagementPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.time_management");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<string> m_hotkey_modifier;
	private static ConfigEntry<string> m_hotkey_time_stop_toggle;
	private static ConfigEntry<string> m_hotkey_time_speed_up;
	private static ConfigEntry<string> m_hotkey_time_speed_down;
	public static ConfigEntry<float> m_time_speed;
	private static ConfigEntry<float> m_time_speed_delta;
	private static ConfigEntry<bool> m_show_time_factor;
	private static ConfigEntry<bool> m_twenty_four_hour_format;
	private static ConfigEntry<bool> m_use_time_scale;
	private static ConfigEntry<bool> m_pause_in_ui;
	private static ConfigEntry<bool> m_realtime_craft_info;
	private static ConfigEntry<int> m_passout_hour;
	private static ConfigEntry<string> m_weekdays;

	private const int HOTKEY_MODIFIER = 0;
	private const int HOTKEY_TIME_STOP_TOGGLE = 1;
	private const int HOTKEY_TIME_SPEED_UP = 2;
	private const int HOTKEY_TIME_SPEED_DOWN = 3;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

	private const int SLEEPY_HOUR_STOP = 6;
	private const int ALMOST_SLEEPY_MINUTE_START = 50;
	private const int DARK_HOUR_START = 20;

	public static float m_time_stop_multiplier = 1f;
	public static bool m_is_ui_visible = false;
	private static float m_outside_light_intensity = 1f;
	private static string[] m_localized_weekdays;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_hotkey_modifier = this.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_hotkey_time_stop_toggle = this.Config.Bind<string>("General", "Time Start/Stop Toggle Hotkey", "Alpha0,Keypad0", "Comma-separated list of Unity Keycodes, any of which will toggle the passage of time.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_hotkey_time_speed_up = this.Config.Bind<string>("General", "Time Scale Increment Hotkey", "Equals,KeypadPlus", "Comma-separated list of Unity Keycodes, any of which will increase the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_hotkey_time_speed_down = this.Config.Bind<string>("General", "Time Scale Decrement Hotkey", "Minus,KeypadMinus", "Comma-separated list of Unity Keycodes, any of which will decrease the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_time_speed = this.Config.Bind<float>("General", "Initial Time Scale", 0.25f, "Initial time scale (float, the default time scale equaling the number of game minutes that elapse per real-time second)");
			m_time_speed_delta = this.Config.Bind<float>("General", "Time Scale Delta", 0.05f, "Change in time scale with each up/down hotkey tick (float).");
			m_twenty_four_hour_format = this.Config.Bind<bool>("General", "24-hour Time Format", false, "If true then display time in 24-hour format, if false then display as game default AM/PM.");
			m_show_time_factor = this.Config.Bind<bool>("General", "Display Time Scale", true, "If true then the game time display will show a '[XX m/s]' time factor postfix representing the current game speed in gametime minutes per realtime seconds.  This value is calculated every realtime second based on simulation time vs real time, so it will show that, for example, the clock pauses when the UI is displayed.  Some people might want the option to hide this, so it's here.");
			m_use_time_scale = this.Config.Bind<bool>("General", "Use Time Scale", true, "Setting this option to false will disable the primary function of this mod, disabling the time scaling and using the usual simulation clock.  It is here for users desiring only to use the Pause in Chests / Crafting functionality and should always be true otherwise.  Note that the time scale will still be displayed on the clock and will represent the Day Speed setting in the game options.");
			m_pause_in_ui = this.Config.Bind<bool>("General", "Pause in Chests / Crafting", true, "This should always be true unless you want time to continue when opening chests and crafting tables.");
			m_realtime_craft_info = this.Config.Bind<bool>("General", "Realtime Craft Times", true, "If true then craft times will be displayed as real-time representing the current time scale.  If false then it will display the game default of simulation time.");
			m_passout_hour = this.Config.Bind<int>("General", "End of Day Hour", 3, "This is the hour representing the end of the day (int, must be between 1 and 5; if set to 0 then the late-night functionality will be disabled)");
			if (m_passout_hour.Value < 0 || m_passout_hour.Value >= SLEEPY_HOUR_STOP - 1) {
				m_passout_hour.Value = 3;
			}
			m_weekdays = this.Config.Bind<string>("General", "Weekdays", "Sun,Mon,Tue,Wed,Thu,Fri,Sat", "The days of the week printed on time label; for language localization (comma-separated list of days)");
			m_localized_weekdays = new string[7];
			int index = 0;
			foreach (string key in m_weekdays.Value.Split(',')) {
				string trimmed_key = key.Trim();
				if (trimmed_key != "") {
					m_localized_weekdays[index] = trimmed_key;
				}
				if (++index >= 7) {
					break;
				}
			}
			m_hotkeys = new Dictionary<int, List<KeyCode>>();
			set_hotkey(m_hotkey_modifier.Value, HOTKEY_MODIFIER);
			set_hotkey(m_hotkey_time_stop_toggle.Value, HOTKEY_TIME_STOP_TOGGLE);
			set_hotkey(m_hotkey_time_speed_up.Value, HOTKEY_TIME_SPEED_UP);
			set_hotkey(m_hotkey_time_speed_down.Value, HOTKEY_TIME_SPEED_DOWN);
			m_is_ui_visible = true;
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.time_management v0.0.9" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void set_hotkey(string keys_string, int key_index) {
		m_hotkeys[key_index] = new List<KeyCode>();
		foreach (string key in keys_string.Split(',')) {
			string trimmed_key = key.Trim();
			if (trimmed_key != "") {
				m_hotkeys[key_index].Add((KeyCode) System.Enum.Parse(typeof(KeyCode), trimmed_key));
			}
		}
	}

	private static bool is_modifier_hotkey_down() {
		if (m_hotkeys[HOTKEY_MODIFIER].Count == 0) {
			return true;
		}
		foreach (KeyCode key in m_hotkeys[HOTKEY_MODIFIER]) {
			if (Input.GetKey(key)) {
				return true;
			}
		}
		return false;
	}

	private static bool is_hotkey_down(int key_index) {
		foreach (KeyCode key in m_hotkeys[key_index]) {
			if (Input.GetKeyDown(key)) {
				return true;
			}
		}
		return false;
	}

	private static void notify(string message) {
		logger.LogInfo(message);
		NotificationStack.Instance.SendNotification(message);
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		private static bool Prefix(ref Player __instance) {
			try {
				if (!m_enabled.Value || !is_modifier_hotkey_down() || !__instance.IsOwner) {
					return true;
				}
				bool changed = false;
				if (is_hotkey_down(HOTKEY_TIME_STOP_TOGGLE)) {
					m_time_stop_multiplier = (m_time_stop_multiplier == 1f ? 0f : 1f);
					changed = true;
				} else if (is_hotkey_down(HOTKEY_TIME_SPEED_UP)) {
					m_time_speed.Value += m_time_speed_delta.Value;
					changed = true;
				} else if (is_hotkey_down(HOTKEY_TIME_SPEED_DOWN)) {
					m_time_speed.Value -= m_time_speed_delta.Value;
					changed = true;
				}
				m_time_speed.Value = (float) System.Math.Round(m_time_speed.Value, 3);
				if (m_time_speed.Value < 0.0001f) {
					m_time_speed.Value = 0f;
				}
				if (changed) {
					if (m_use_time_scale.Value) {
						notify("Time Factor: " + m_time_speed.Value + " (real sec / game min) [Paused: " + (m_time_stop_multiplier == 0f ? "True" : "False") + "]");
					} else {
						notify("Time Factor: <disabled in config> [Paused: " + (m_time_stop_multiplier == 0f ? "True" : "False") + "]");
					}
				}
				//} catch {
				//	// ignorable nullref exceptions will get thrown for a bit when game is starting/dying/in menu
				//}
			} catch (Exception e) {
				logger.LogError("** Player.Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Settings))]
	[HarmonyPatch("DaySpeedMultiplier", MethodType.Getter)]
	class HarmonyPatch_Wish_Settings_DaySpeedMultiplier {

		private static bool Prefix(ref float __result) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				MethodBase calling_method = new StackFrame(2).GetMethod();
				ParameterInfo[] params_info = calling_method.GetParameters();
				if (calling_method.Name == "Craft" || (calling_method.Name == "Prefix" && params_info.Length > 1 && params_info[1].ParameterType == typeof(Recipe))) {
					if (!m_use_time_scale.Value) {
						return true;
					}
					// the correct time scale will be used in the patched Recipe.GetHoursToCraft method
					__result = 1f;
					return false;
				}
				if (!m_use_time_scale.Value) {
					if (m_pause_in_ui.Value && !m_is_ui_visible) {
						__result = 0f;
						return false;
					}
					return true;
				}
				__result = (m_pause_in_ui.Value && !m_is_ui_visible ? 0f : m_time_speed.Value * m_time_stop_multiplier);
				return false;
			} catch (Exception e) {
				logger.LogError("Settings.DaySpeedMultiplier_Getter_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Recipe), "GetHoursToCraft")]
	class HarmonyPatch_Recipe_GetHoursToCraft {
	
		private static bool Prefix(Recipe __instance, ref float __result, float speedMultiplier) {
			try {
				if (!(m_enabled.Value && m_use_time_scale.Value)) {
					return true;
				}
				if (GameSave.Farming.GetNode("Farming6c")) {
					speedMultiplier += 0.05f + 0.05f * (float) GameSave.Farming.GetNodeAmount("Farming6c");
				}
				if (__instance.recipeType == RecipeType.Anvil || (__instance.recipeType == RecipeType.Furnace && GameSave.Mining.GetNode("Mining1b"))) {
					speedMultiplier += 0.1f * (float)GameSave.Mining.GetNodeAmount("Mining1b");
				}
				try {
					FoodData foodData = (FoodData) __instance.output.item;
					if ((bool) foodData && (foodData.isMeal || foodData.isPotion) && GameSave.Farming.GetNode("Farming6b")) {
						speedMultiplier += 0.1f * (float)GameSave.Farming.GetNodeAmount("Farming6b");
					}
				} catch {
				}
				__result = __instance.hoursToCraft * 60f / speedMultiplier / (m_time_speed.Value <= 0 ? 0.001f : m_time_speed.Value);
				return false;
			} catch (Exception e) {
				logger.LogError("** Recipe.GetHoursToCraft_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingPanel), "UpdateItemImages")]
	class HarmonyPatch_CraftingPanel_UpdateItemImages {
	
		private static void Postfix(CraftingPanel __instance, Recipe recipe, float ___speedMultiplier) {
			if (!(m_enabled.Value && m_use_time_scale.Value)) {
				return;
			}
			float craft_time = recipe.GetHoursToCraft(___speedMultiplier) / 60f;
			if (!m_realtime_craft_info.Value) {
				craft_time *= m_time_speed.Value;
				__instance.craftTimeTMP.text = ((craft_time < 1f) ? ($"{craft_time * 60f:0}" + " min") : ($"{craft_time:0.0}" + ((craft_time >= 1.1f) ? " hours" : " hour")));
				return;
			}
			int seconds = (int) ((craft_time - Math.Truncate(craft_time)) * 60f);
			craft_time = (float) Math.Truncate(craft_time);
			int hours = (int) (craft_time / 60f);
			int minutes = (int) (craft_time - (float) hours * 60f);
			__instance.craftTimeTMP.text = string.Format("{0,2:D2}h:{1,2:D2}m:{2,2:D2}s", hours, minutes, seconds);
		}
	}

	private static bool is_sleepy_hour(DateTime time) {
		return (time.Hour >= m_passout_hour.Value && time.Hour < SLEEPY_HOUR_STOP);
	}

	private static bool is_sleepy_time(DateTime time) {
		return is_sleepy_hour(time);
	}

	private static bool is_almost_sleepy_time(DateTime time) {
		return (time.Hour == m_passout_hour.Value - 1 && time.Minute >= ALMOST_SLEEPY_MINUTE_START);
	}

	private static bool is_late_night(DateTime time) {
		return (m_passout_hour.Value < 6 && time.Hour >= 0 && time.Hour < m_passout_hour.Value);
	}

	private static bool is_dark_time(DateTime time) {
		return (time.Hour >= DARK_HOUR_START || is_late_night(time));
	}

	private static bool is_invalid_time(DateTime time) {
		return (time.Hour >= m_passout_hour.Value + 1 && time.Hour < SLEEPY_HOUR_STOP);
	}

	[HarmonyPatch(typeof(DayCycle), "UpdateTimeText")]
	class HarmonyPatch_DayCycle {

		private const float CHECK_FREQUENCY = 1.0f;
		private static float m_elapsed = CHECK_FREQUENCY;
		private static DateTime m_last_system_time = DateTime.MinValue;
		private static DateTime m_last_game_time = DateTime.MinValue;
		private static string m_time_factor_string = "";

		private static bool Prefix(
			ref DayCycle __instance, 
			ref TextMeshProUGUI ____timeTMP, 
			ref Transform ____timeBar
		) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				if ((m_elapsed += Time.fixedDeltaTime) >= CHECK_FREQUENCY) {
					m_elapsed = 0f;
					if (m_last_system_time != DateTime.MinValue) {
						m_time_factor_string = "[" + Math.Round((__instance.Time - m_last_game_time).TotalMinutes / (DateTime.Now - m_last_system_time).TotalSeconds, 2).ToString() + " m/s]";					
					}
					m_last_system_time = DateTime.Now;
					m_last_game_time = __instance.Time;
				}
				____timeTMP.text =
					(__instance.Time.Hour <= m_passout_hour.Value || (m_passout_hour.Value == 0 && __instance.Time.Hour >= 22) ? "<color=red>" : "") +
					__instance.Time.ToString((m_twenty_four_hour_format.Value ? "HH:mm" : "hh:mm tt")) +
					(m_show_time_factor.Value ? m_time_factor_string : "");
				____timeBar.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, -180f, ((float) __instance.Time.Hour + (float) __instance.Time.Minute / 60f - 6f + 1f) / 20f));
				return false;
			} catch (Exception e) {
				logger.LogError("** DayCycle.UpdateTimeText_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "DisableUI")]
	class HarmonyPatch_GameManager_DisableUI {

		private static void Postfix() {
			m_is_ui_visible = false;
		}
	}

	[HarmonyPatch(typeof(GameManager), "EnableUI")]
	class HarmonyPatch_GameManager_EnableUI {

		private static void Postfix() {
			m_is_ui_visible = true;
		}
	}

	[HarmonyPatch(typeof(DayCycle))]
	[HarmonyPatch("OutsideLightIntensity", MethodType.Getter)]
	class HarmonyPatch_DayCycle_OutsideLightIntensity_Getter {

		private static bool Prefix(ref float __result) {
			__result = m_outside_light_intensity;
			return false;
		}
	}

	[HarmonyPatch(typeof(DayCycle), "Update")]
	class HarmonyPatch_DayCycle_Update {

		private static bool Prefix(
			DayCycle __instance,
			bool ____initialized,
			DateTime ___cachedTime,
			DateTime ___previousDay,
			DateTime ___nextDay,
			ref int ___previousHour,
			ref bool ___sent30MinuteEvent,
			TextMeshProUGUI ____dayTMP,
			TextMeshProUGUI ____yearTMP,
			Light ____globalLight,
			LightSettings[] ____lightSettings,
			LightSettings[] ____overrideLightSettings,
			UnityEngine.UI.Image ____weatherImage,
			Sprite ____dayImage,
			Sprite ____rainingDayImage,
			Sprite ____nightImage,
			Sprite ____rainingNightImage
		) {
			try {
				if (!m_enabled.Value || m_passout_hour.Value == 0) {
					return true;
				}
				if (!____initialized || !Player.Instance) {
					return false;
				}
				___cachedTime = SingletonBehaviour<GameSave>.Instance.CurrentWorld.time;
				if (__instance.TransitioningDays) {
					return false;
				}
				bool flag = true;
				if (GameManager.Multiplayer) {
					foreach (NetworkGamePlayer value in NetworkLobbyManager.Instance.GamePlayers.Values) {
						if (!value.ableToSleep) {
							flag = false;
							break;
						}
					}
				}
				if (!__instance.DayEnding) {
					float num = Settings.DaySpeedMultiplier;
					if (Settings.PauseDuringDialogue && (bool)DialogueController.Instance && DialogueController.Instance.DialogueOnGoing) {
						num = 0f;
					}
					if ((GameManager.Multiplayer || (!Cutscene.Active && !Cutscene.WithinMultipartCutscene)) && (flag || !is_sleepy_hour(__instance.Time)) && __instance.IncrementTime && (!GameManager.Multiplayer || GameManager.Host)) {
						__instance.Time = __instance.Time.AddSeconds(UnityEngine.Time.deltaTime * PlaySettingsManager.PlaySettings.daySpeed * num * (is_almost_sleepy_time(__instance.Time) ? 0.5f : 1f));
						if (__instance.Time.Month != 2 || __instance.Time.Day > 28) {
							__instance.Time = new DateTime(__instance.Time.Year + 1, 2, 1, __instance.Time.Hour, __instance.Time.Minute, __instance.Time.Second, DateTimeKind.Utc).ToUniversalTime();
						}
					}
					if (is_invalid_time(__instance.Time)) {
						// not sure about this one; assuming this is a catch for crazy timestamp
						if (GameManager.Multiplayer && NetworkLobbyManager.Instance.GamePlayers.Count >= 1) {
							if (!flag) {
								__instance.Time = new DateTime(___previousDay.Year, ___previousDay.Month, ___previousDay.Day, 23, 58, 0, DateTimeKind.Utc).ToUniversalTime();
							}
						}
						else if (!Player.Instance.ReadyToSleep) {
							__instance.Time = new DateTime(___previousDay.Year, ___previousDay.Month, ___previousDay.Day, 23, 58, 0, DateTimeKind.Utc).ToUniversalTime();
						}
					} else if (
						DayCycle.DayFromTime(__instance.Time) > DayCycle.DayFromTime(___nextDay) || 
						(DayCycle.DayFromTime(__instance.Time) == DayCycle.DayFromTime(___nextDay) && !is_late_night(__instance.Time)) ||
						is_sleepy_time(__instance.Time)
					) {
						DayCycle.OnDayEnd?.Invoke();
						__instance.DayEnding = true;
						__instance.Time = new DateTime(___nextDay.Year, ___nextDay.Month, ___nextDay.Day, m_passout_hour.Value, 0, 0, DateTimeKind.Utc).ToUniversalTime();
						if (PlaySettingsManager.PlaySettings.skipEndOfDayScreen) {
							Player.Instance.SkipSleep();
							__instance.DayEnding = false;
						} else {
							Player.Instance.PassOut();
						}
					}
				}
				if (__instance.Time.Hour != ___previousHour) {
					___previousHour = __instance.Time.Hour;
					___sent30MinuteEvent = false;
					DayCycle.OnHourChange?.Invoke(___previousHour, __instance.Time.Minute);
				}
				else if (__instance.Time.Minute == 30 && !___sent30MinuteEvent) {
					___sent30MinuteEvent = true;
					DayCycle.OnHourChange?.Invoke(___previousHour, __instance.Time.Minute);
				}
				____dayTMP.text = m_localized_weekdays[DayCycle.Weekday] + " Day " + DayCycle.MonthDay;
				____yearTMP.text = string.Concat(__instance.Season, " Year ", DayCycle.Year);
				__instance.GetType().GetTypeInfo().GetMethod("UpdateTimeText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				__instance.GetType().GetTypeInfo().GetMethod("UpdateMoneyText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				if (____globalLight != null)
				{
					if (__instance.OverrideLightSettings) {
						____globalLight.color = __instance.OverrideColor;
						____globalLight.intensity = __instance.OverrideIntensity;
					} else {
						__instance.GetType().GetTypeInfo().GetMethod("LerpLightSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {____lightSettings});
						m_outside_light_intensity = ____globalLight.intensity;
						__instance.GetType().GetTypeInfo().GetMethod("LerpLightSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {____overrideLightSettings ?? ____lightSettings});
					}
					if (!Player.Instance.Sleeping) {
						Shader.SetGlobalFloat("_GlobalLightIntensity", ____globalLight.intensity);
					}
				}
				if (is_dark_time(__instance.Time)) {
					____weatherImage.sprite = (__instance.WorldRaining ? ____rainingNightImage : ____nightImage);
				} else {
					____weatherImage.sprite = (__instance.WorldRaining ? ____rainingDayImage : ____dayImage);
				}
				if (__instance.Time.Hour >= 22 && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Rest")) {
					SingletonBehaviour<HelpTooltips>.Instance.SendNotification("Rest", "If you aren't in bed by <color=#39CCFF>12 am</color>, you will fall asleep where you stand and be charged a hospital fee. Make sure you don't miss your bedtime!", new List<(Transform, Vector3, Direction)>(), 22, delegate {
						SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Rest", value: true);
					});
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** DayCycle.Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	/*
	[HarmonyPatch(typeof(MainMusic), "CheckNightMusicImmediate")]
	class HarmonyPatch_MainMusic_CheckNightMusicImmediate {

		private static bool Prefix(
			int x, 
			ref bool ___cricketsPlaying,
			ref bool ___nightMusicPlaying,
			AudioSource ____mainMusic,
			AudioClip ___nightTimeMusic,
			AudioClip ___dayTimeMusic
		) {
			if (!m_enabled.Value || m_passout_hour.Value == 0) {
				return true;
			}
			if (___nightTimeMusic != null && (x >= 20 || (x >= 0 && x < SLEEPY_HOUR_STOP)) && !___nightMusicPlaying) {
				___nightMusicPlaying = true;
				___cricketsPlaying = false;
				____mainMusic.clip = ___nightTimeMusic;
				____mainMusic.Play();
			} else if ((x < 20 || x == 24 || x >= m_passout_hour.Value) && ___nightMusicPlaying) {
				___nightMusicPlaying = false;
				___cricketsPlaying = false;
				____mainMusic.clip = ___dayTimeMusic;
				____mainMusic.Play();
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(MainMusic), "CheckNightMusic")]
	class HarmonyPatch_MainMusic_CheckNightMusic {

		private static bool Prefix(
			int x, 
			int minute,
			ref bool ___cricketsPlaying,
			ref bool ___nightMusicPlaying,
			AudioSource ____mainMusic,
			AudioClip ___nightTimeMusic,
			AudioClip ___dayTimeMusic,
			float ___startingVolume,
			float ____fadeInDuration,
			float ___volume
		) {
			if (!m_enabled.Value || m_passout_hour.Value == 0) {
				return true;
			}
			if (x == SLEEPY_HOUR_STOP) {
				DOVirtual.Float(___startingVolume, 0f, ____fadeInDuration, delegate(float value) {
					___volume = value;
				}).OnComplete(delegate {
					if ((bool) ____mainMusic) {
						____mainMusic.clip = ___dayTimeMusic;
					}
				});
			} else if (___nightTimeMusic != null && (x >= 20 || (x >= 0 && x < SLEEPY_HOUR_STOP)) && (!___nightMusicPlaying || ___cricketsPlaying)) {
				___nightMusicPlaying = true;
				___cricketsPlaying = false;
				DOVirtual.Float(___startingVolume, 0f, ____fadeInDuration, delegate(float value) {
					___volume = value;
				}).OnComplete(delegate {
					if ((bool) ____mainMusic) {
						____mainMusic.clip = ___nightTimeMusic;
						____mainMusic.Play();
						DOVirtual.Float(___startingVolume / 5f, ___startingVolume, ____fadeInDuration, delegate(float value) {
							___volume = value;
						});
					}
				});
			} else {
				if (x >= 20 || (!___nightMusicPlaying && !___cricketsPlaying)) {
					return false;
				}
				___nightMusicPlaying = false;
				___cricketsPlaying = false;
				DOVirtual.Float(___startingVolume, 0f, ____fadeInDuration, delegate(float value) {
					___volume = value;
				}).OnComplete(delegate {
					if ((bool) ____mainMusic) {
						____mainMusic.clip = ___dayTimeMusic;
						____mainMusic.Play();
						DOVirtual.Float(___startingVolume / 5f, ___startingVolume, ____fadeInDuration, delegate(float value) {
							___volume = value;
						});
					}
				});
			}
			return false;
		}
	}
	*/
}