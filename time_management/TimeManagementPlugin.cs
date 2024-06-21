
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;
using System.Reflection;
using PSS;
using I2.Loc;

[BepInPlugin("devopsdinosaur.sunhaven.time_management", "Time Management", "0.0.11")]
public class TimeManagementPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.time_management");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<string> m_hotkey_modifier;
	private static ConfigEntry<string> m_hotkey_time_stop_toggle;
	private static ConfigEntry<string> m_hotkey_time_speed_up;
	private static ConfigEntry<string> m_hotkey_time_speed_down;
	private static ConfigEntry<string> m_hotkey_time_reverse_toggle;
	public static ConfigEntry<float> m_time_speed;
	private static ConfigEntry<float> m_time_speed_delta;
	private static ConfigEntry<bool> m_show_time_factor;
	private static ConfigEntry<bool> m_twenty_four_hour_format;
	private static ConfigEntry<bool> m_use_time_scale;
	private static ConfigEntry<bool> m_pause_in_ui;
	private static ConfigEntry<int> m_passout_hour;
	private static ConfigEntry<string> m_weekdays;

	private const int HOTKEY_MODIFIER = 0;
	private const int HOTKEY_TIME_STOP_TOGGLE = 1;
	private const int HOTKEY_TIME_SPEED_UP = 2;
	private const int HOTKEY_TIME_SPEED_DOWN = 3;
	private const int HOTKEY_TIME_REVERSE_TOGGLE= 4;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

	private const int SLEEPY_HOUR_STOP = 6;
	private const int ALMOST_SLEEPY_MINUTE_START = 50;
	private const int DARK_HOUR_START = 20;

	public static float m_time_stop_multiplier = 1f;
	public static float m_time_direction_multiplier = 1f;
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
			m_hotkey_time_reverse_toggle = this.Config.Bind<string>("General", "Time Reverse Toggle Hotkey", "Home", "Comma-separated list of Unity Keycodes, any of which will toggle reverse/forward time change.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_time_speed = this.Config.Bind<float>("General", "Initial Time Scale", 0.25f, "Initial time scale (float, the default time scale equaling the number of game minutes that elapse per real-time second)");
			m_time_speed_delta = this.Config.Bind<float>("General", "Time Scale Delta", 0.05f, "Change in time scale with each up/down hotkey tick (float).");
			m_twenty_four_hour_format = this.Config.Bind<bool>("General", "24-hour Time Format", false, "If true then display time in 24-hour format, if false then display as game default AM/PM.");
			m_show_time_factor = this.Config.Bind<bool>("General", "Display Time Scale", true, "If true then the game time display will show a '[XX m/s]' time factor postfix representing the current game speed in gametime minutes per realtime seconds.  This value is calculated every realtime second based on simulation time vs real time, so it will show that, for example, the clock pauses when the UI is displayed.  Some people might want the option to hide this, so it's here.");
			m_use_time_scale = this.Config.Bind<bool>("General", "Use Time Scale", true, "Setting this option to false will disable the primary function of this mod, disabling the time scaling and using the usual simulation clock.  It is here for users desiring only to use the Pause in Chests / Crafting functionality and should always be true otherwise.  Note that the time scale will still be displayed on the clock and will represent the Day Speed setting in the game options.");
			m_pause_in_ui = this.Config.Bind<bool>("General", "Pause in Chests / Crafting", true, "This should always be true unless you want time to continue when opening chests and crafting tables.");
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
			set_hotkey(m_hotkey_time_reverse_toggle.Value, HOTKEY_TIME_REVERSE_TOGGLE);
			m_is_ui_visible = true;
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.time_management v0.0.11" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
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

		private static bool Prefix(Player __instance) {
			try {
				if (!m_enabled.Value || !is_modifier_hotkey_down() || !__instance.IsOwner) {
					return true;
				}
				bool changed = false;
				if (is_hotkey_down(HOTKEY_TIME_STOP_TOGGLE)) {
					m_time_stop_multiplier = (m_time_stop_multiplier == 1f ? 0f : 1f);
					changed = true;
				}
				if (is_hotkey_down(HOTKEY_TIME_REVERSE_TOGGLE)) {
					m_time_direction_multiplier = (m_time_direction_multiplier == 1f ? -1f : 1f);
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
						notify("Time Factor: " + m_time_direction_multiplier * m_time_speed.Value + " (real sec / game min) [Paused: " + (m_time_stop_multiplier == 0f ? "True" : "False") + "]");
					} else {
						notify("Time Factor: <disabled in config> [Paused: " + (m_time_stop_multiplier == 0f ? "True" : "False") + "]");
					}
					typeof(DayCycle).GetMethod("UpdateTimeText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(DayCycle.Instance, new object[] {});
				}
			} catch (Exception e) {
				logger.LogError("** Player.Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "LateUpdate")]
	class HarmonyPatch_GameManager_LateUpdate {

		private static bool Prefix(float ___moveSpeed) {
			try {
				if (!PlayerInput.AllowInput || !Settings.EnableCheats) {
					return false;
				}
				if (Input.GetKeyDown(KeyCode.Minus)) {
					notify("cheatTimeScale is disabled to prevent glitches with time_management time scaling.");
				}
				if (Input.GetKeyDown(KeyCode.Equals)) {
					notify("cheatTimeScale is disabled to prevent glitches with time_management time scaling.");
				}
				if (Input.GetKey(KeyCode.LeftBracket)) {
					Time.timeScale = 0.4f * 1f;
					MainMusic.Pitch = 0.75f;
				} else if (Input.GetKey(KeyCode.RightBracket)) {
					Time.timeScale = 8f * 1f;
					MainMusic.Pitch = 2f;
				} else {
					if ((bool)Player.Instance && UIHandler.InventoryOpen && !GameManager.Multiplayer) {
						Time.timeScale = 0f;
					} else {
						Time.timeScale = 1f * 1f;
					}
					MainMusic.Pitch = 1f;
				}
				if (Input.GetKeyDown(KeyCode.F7)) {
					QuantumConsoleManager quantumConsoleManager = UnityEngine.Object.FindObjectOfType<QuantumConsoleManager>();
					quantumConsoleManager.noclip(!quantumConsoleManager.no_clip);
				}
				if (Input.GetKeyDown(KeyCode.F8)) {
					QuantumConsoleManager quantumConsoleManager2 = UnityEngine.Object.FindObjectOfType<QuantumConsoleManager>();
					quantumConsoleManager2.godmode(!quantumConsoleManager2.god_mode);
				}
				if (Input.GetKeyDown(KeyCode.Comma)) {
					___moveSpeed = Mathf.Clamp(___moveSpeed - 2f, 1f, 100f);
					UnityEngine.Object.FindObjectOfType<QuantumConsoleManager>().setstat("movespeed", ___moveSpeed);
				}
				if (Input.GetKeyDown(KeyCode.Period)) {
					___moveSpeed = Mathf.Clamp(___moveSpeed + 2f, 1f, 100f);
					UnityEngine.Object.FindObjectOfType<QuantumConsoleManager>().setstat("movespeed", ___moveSpeed);
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_GameManager_LateUpdate_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	private static bool get_day_speed_multiplier(bool ignore_ui, ref float result) {
		try {
			if (!m_enabled.Value) {
				return true;
			}
			if (!m_use_time_scale.Value) {
				if (!ignore_ui && m_pause_in_ui.Value && !m_is_ui_visible) {
					result = 0f;
					return false;
				}
				return true;
			}
			result = (!ignore_ui && m_pause_in_ui.Value && !m_is_ui_visible ? 0f : m_time_speed.Value * m_time_stop_multiplier);
			//logger.LogInfo(result);
			return false;
		} catch (Exception e) {
			logger.LogError("get_day_speed_multiplier ERROR - " + e);
		}
		return true;
	}

	[HarmonyPatch(typeof(Settings))]
	[HarmonyPatch("DaySpeedMultiplier", MethodType.Getter)]
	class HarmonyPatch_Wish_Settings_DaySpeedMultiplier {

		private static bool Prefix(ref float __result) {
			return get_day_speed_multiplier(true, ref __result);
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
				if (m_last_system_time != DateTime.MinValue) {
					float multiplier = 0;
					m_time_factor_string = (!get_day_speed_multiplier(false, ref multiplier) && multiplier > 0f ? " [" + Math.Round((__instance.Time - m_last_game_time).TotalMinutes / (DateTime.Now - m_last_system_time).TotalSeconds, 2).ToString() + " m/s]" : " [Paused]");					
				}
				m_last_system_time = DateTime.Now;
				m_last_game_time = __instance.Time;
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

	[HarmonyPatch(typeof(GameManager), "EnableUI")]
	class HarmonyPatch_GameManager_EnableUI {

		private static void Postfix() {
			m_is_ui_visible = true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "DisableUI")]
	class HarmonyPatch_GameManager_DisableUI {

		private static void Postfix() {
			m_is_ui_visible = false;
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

		private class PreviousValues {
			public int day = -1;
			public int monthDay = -1;
			public int year = -1;
			public int hour = -1;
			public int minute = -1;
			public Season season = Season.Winter;
			public float coins = -1f;
			public float tickets = -1f;
			public float orbs = -1f;
		}

		private static bool Prefix(
			DayCycle __instance,
			bool ____initialized,
			ref DateTime ___cachedTime,
			PreviousValues ___previousValues,
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
				if (!m_enabled.Value) {
					return true;
				}
				if (!____initialized || !Player.Instance) {
					return false;
				}
				___cachedTime = SingletonBehaviour<GameSave>.Instance.CurrentWorld.time;
				if (!__instance.TransitioningDays) {
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
						float num = 0;
						if (get_day_speed_multiplier(false, ref num)) {
							switch (Settings.DaySpeed) {
							case 1: num = 0.5f; break;
							case 2: num = 2f / 3f; break;
							case 3: num = 1f; break;
							case 4: num = 1.3333334f; break;
							case 5: num = 1.6666666f; break;
							default: num = 1f; break;
							}
						}
						if (Settings.PauseDuringDialogue && (bool) DialogueController.Instance && DialogueController.Instance.DialogueOnGoing) {
							num = 0f;
						}
						if ((GameManager.Multiplayer || (!Cutscene.Active && !Cutscene.WithinMultipartCutscene)) && (flag || !is_sleepy_hour(__instance.Time)) && __instance.IncrementTime && (!GameManager.Multiplayer || GameManager.Host)) {
							DateTime current_time = __instance.Time;
							__instance.Time = __instance.Time.AddSeconds(m_time_direction_multiplier * UnityEngine.Time.deltaTime * PlaySettingsManager.PlaySettings.daySpeed * num * (is_almost_sleepy_time(__instance.Time) ? 0.5f : 1f));
							if (m_time_direction_multiplier == -1f && is_sleepy_time(__instance.Time)) {
								__instance.Time = current_time;
							}
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
							} else if (!Player.Instance.ReadyToSleep) {
								__instance.Time = new DateTime(___previousDay.Year, ___previousDay.Month, ___previousDay.Day, 23, 58, 0, DateTimeKind.Utc).ToUniversalTime();
							}
						} else if (
							DayCycle.DayFromTime(__instance.Time) > DayCycle.DayFromTime(___nextDay) ||
							(DayCycle.DayFromTime(__instance.Time) == DayCycle.DayFromTime(___nextDay) && !is_late_night(__instance.Time)) ||
							is_sleepy_time(__instance.Time)
						) {
							__instance.GetType().GetMethod("DayEnd", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[]{});
						}
					}
				}
				if (__instance.Time.Hour != ___previousHour) {
					___previousHour = __instance.Time.Hour;
					___sent30MinuteEvent = false;
					DayCycle.OnHourChange?.Invoke(___previousHour, __instance.Time.Minute);
				} else if (__instance.Time.Minute == 30 && !___sent30MinuteEvent) {
					___sent30MinuteEvent = true;
					DayCycle.OnHourChange?.Invoke(___previousHour, __instance.Time.Minute);
				}
				if (___previousValues.monthDay != DayCycle.MonthDay || ___previousValues.season != __instance.Season || ___previousValues.year != DayCycle.Year) {
					____dayTMP.text = m_localized_weekdays[DayCycle.Weekday] + " Day " + StringHelper.StringFromInt[DayCycle.MonthDay];
					____yearTMP.text = string.Concat(__instance.Season, " Year ", (DayCycle.Year < 200 ? StringHelper.StringFromInt[DayCycle.Year] : DayCycle.Year.ToString()));
				}
				___previousValues.monthDay = DayCycle.MonthDay;
				___previousValues.year = DayCycle.Year;
				if (___previousValues.minute != __instance.Time.Minute || ___previousValues.hour != __instance.Time.Hour) {
					__instance.GetType().GetTypeInfo().GetMethod("UpdateTimeText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				}
				___previousValues.minute = __instance.Time.Minute;
				___previousValues.hour = __instance.Time.Hour;
				__instance.GetType().GetTypeInfo().GetMethod("UpdateMoneyText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				if (____globalLight != null) {
					if (__instance.OverrideLightSettings) {
						____globalLight.color = __instance.OverrideColor;
						____globalLight.intensity = __instance.OverrideIntensity;
					} else {
						__instance.GetType().GetTypeInfo().GetMethod("LerpLightSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { ____lightSettings });
						m_outside_light_intensity = ____globalLight.intensity;
						__instance.GetType().GetTypeInfo().GetMethod("LerpLightSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { ____overrideLightSettings ?? ____lightSettings });
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
					SingletonBehaviour<HelpTooltips>.Instance.SendNotification("Rest", $"If you aren't in bed by <color=#39CCFF>{(m_passout_hour.Value == 0 ? 12 : m_passout_hour.Value)}</color>, you will fall asleep where you stand and be charged a hospital fee. Make sure you don't miss your bedtime!", new List<(Transform, Vector3, Direction)>(), 22, delegate {
						SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Rest", value: true);
					});
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_DayCycle_Update.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "SetupCraftingQueue")]
	class HarmonyPatch_CraftingTable_SetupCraftingQueue {

		private static bool Prefix(ref bool ___needsRefresh) {
			___needsRefresh = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "CancelCraft")]
	class HarmonyPatch_CraftingTable_CancelCraft {

		private static bool Prefix(ref bool ___needsRefresh) {
			___needsRefresh = true;
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "CancelCrafting")]
	class HarmonyPatch_CraftingTable_CancelCrafting {

		private static bool Prefix(ref bool ___needsRefresh) {
			___needsRefresh = true;
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "LateUpdate")]
	class HarmonyPatch_CraftingTable_LateUpdate {

		const float CHECK_FREQUENCY = 0.5f;
		static float m_elapsed = CHECK_FREQUENCY;
		static Dictionary<int, RecipeInfo> m_recipe_times = new Dictionary<int, RecipeInfo>();
		
		class RecipeInfo {
			public int item_hash = 0;
			public float m_total_time = -1;
			public DateTime m_start_time;
		}

		private static bool Prefix(
			CraftingTable __instance,
			ref bool ___needsRefresh,
			CraftingTableData ___craftingData,
			CraftingUI ___craftingUI,
			Animator ___animator
		) {
			try {
				if ((m_elapsed += Time.deltaTime) < CHECK_FREQUENCY && !___needsRefresh) {
					return false;
				}
				m_elapsed = 0f;
				___needsRefresh = false;
				if (___craftingData.items.Count == 0) {
					___craftingUI.worldProgressSlider.gameObject.SetActive(value: false);
					__instance.GetType().GetMethod("SetCraftingQueueImages", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
					return false;
				}
				ItemCraftInfo item = ___craftingData.items[0];
				if (!m_recipe_times.ContainsKey(__instance.GetHashCode())) {
					m_recipe_times[__instance.GetHashCode()] = new RecipeInfo();
				}
				RecipeInfo recipe_info = m_recipe_times[__instance.GetHashCode()];
				if (recipe_info.item_hash != item.GetHashCode() || recipe_info.m_total_time <= 0) {
					recipe_info.item_hash = item.GetHashCode();
					recipe_info.m_total_time = item.craftTime;
					recipe_info.m_start_time = DayCycle.Instance.Time;
				}
				item.craftTime = (float) (DayCycle.Instance.Time - recipe_info.m_start_time).TotalSeconds / 60f;
				logger.LogInfo($"instance: {__instance.name}, craftTime: {item.craftTime}, recipe_time: {recipe_info.m_total_time}, craftingUI.value: {___craftingUI.value}");
				if (item.craftTime >= recipe_info.m_total_time) {
					___craftingData.items.RemoveAt(0);
					recipe_info.m_total_time = -1;
					__instance.GetType().GetMethod("FinishItem", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {item});
				} else {
					___craftingUI.value = Math.Abs(1 - ((recipe_info.m_total_time - item.craftTime) / recipe_info.m_total_time));
					___craftingUI.worldProgressSlider.value = ___craftingUI.value;
					___craftingUI.screenProgressSlider.value = ___craftingUI.value;
					___craftingUI.worldProgressSlider.gameObject.SetActive(value: true);
					if (___craftingUI.value - ___craftingUI.worldProgressSlider.value > 0.025f) {
						___craftingUI.worldProgressSlider.value = ___craftingUI.value;
						___craftingUI.screenProgressSlider.value = ___craftingUI.value;
					}
					if (___animator != null) {
						___animator.SetBool("Crafting", value: true);
					}
				}
				__instance.GetType().GetMethod("SetCraftingQueueImages", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_LateUpdate.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}