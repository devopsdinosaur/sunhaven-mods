
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


[BepInPlugin("devopsdinosaur.sunhaven.time_management", "Time Management", "0.0.1")]
public class Plugin : BaseUnityPlugin {

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

	private const int HOTKEY_MODIFIER = 0;
	private const int HOTKEY_TIME_STOP_TOGGLE = 1;
	private const int HOTKEY_TIME_SPEED_UP = 2;
	private const int HOTKEY_TIME_SPEED_DOWN = 3;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

	public static float m_time_stop_multiplier = 1f;
	public static bool m_is_ui_visible = false;
	private static Plugin m_instance;
	public static Plugin Instance {
		get {
			return m_instance;
		}
	}

	public Plugin() {
	}

	private void Awake() {
		m_instance = this;
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.time_management v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_hotkey_modifier = this.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_stop_toggle = this.Config.Bind<string>("General", "Time Start/Stop Toggle Hotkey", "Alpha0,Keypad0", "Comma-separated list of Unity Keycodes, any of which will toggle the passage of time.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_speed_up = this.Config.Bind<string>("General", "Time Scale Increment Hotkey", "Equals,KeypadPlus", "Comma-separated list of Unity Keycodes, any of which will increase the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_speed_down = this.Config.Bind<string>("General", "Time Scale Decrement Hotkey", "Minus,KeypadMinus", "Comma-separated list of Unity Keycodes, any of which will decrease the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_time_speed = this.Config.Bind<float>("General", "Initial Time Scale", 0.25f, "Initial time scale (float, the default time scale equaling the number of game minutes that elapse per real-time second)");
		m_time_speed_delta = this.Config.Bind<float>("General", "Time Scale Delta", 0.05f, "Change in time scale with each up/down hotkey tick (float).");
		m_twenty_four_hour_format = this.Config.Bind<bool>("General", "24-hour Time Format", false, "If true then display time in 24-hour format, if false then display as game default AM/PM.");
		m_show_time_factor = this.Config.Bind<bool>("General", "Display Time Scale", true, "If true then the game time display will show a '[XX m/s]' time factor postfix representing the current game speed in gametime minutes per realtime seconds.  This value is calculated every realtime second based on simulation time vs real time, so it will show that, for example, the clock pauses when the UI is displayed.  Some people might want the option to hide this, so it's here.");
		m_use_time_scale = this.Config.Bind<bool>("General", "Use Time Scale", true, "Setting this option to false will disable the primary function of this mod, disabling the time scaling and using the usual simulation clock.  It is here for users desiring only to use the Pause in UI functionality and should always be true otherwise.  Note that the time scale will still be displayed on the clock and will represent the Day Speed setting in the game options.");
		m_pause_in_ui = this.Config.Bind<bool>("General", "Pause in UI", true, "This should always be true unless you want time to continue when opening chests and crafting tables.");
		m_hotkeys = new Dictionary<int, List<KeyCode>>();
		set_hotkey(m_hotkey_modifier.Value, HOTKEY_MODIFIER);
		set_hotkey(m_hotkey_time_stop_toggle.Value, HOTKEY_TIME_STOP_TOGGLE);
		set_hotkey(m_hotkey_time_speed_up.Value, HOTKEY_TIME_SPEED_UP);
		set_hotkey(m_hotkey_time_speed_down.Value, HOTKEY_TIME_SPEED_DOWN);
		m_is_ui_visible = false;
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
			if (!m_enabled.Value || !is_modifier_hotkey_down()) {
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
			return true;
		}
	}

	[HarmonyPatch(typeof(Settings))]
	[HarmonyPatch("DaySpeedMultiplier", MethodType.Getter)]
	class HarmonyPatch_Wish_Settings_DaySpeedMultiplier {

		private static bool Prefix(ref float __result) {
			if (!m_enabled.Value) {
				return true;
			}
			if (!m_use_time_scale.Value) {
				if (m_is_ui_visible) {
					__result = 0f;
					return false;
				}
				return true;
			}
			MethodBase calling_method = new StackFrame(2).GetMethod();
			ParameterInfo[] params_info = calling_method.GetParameters();
			if (calling_method.Name == "Craft" || (calling_method.Name == "Prefix" && params_info.Length > 1 && params_info[1].ParameterType == typeof(Recipe))) {
				__result = 1.0f - Plugin.m_time_speed.Value;
			} else {
				__result = (m_is_ui_visible ? 0f : Plugin.m_time_speed.Value * Plugin.m_time_stop_multiplier);
			}
			return false;
		}
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
				(__instance.Time.Hour >= 22 || __instance.Time.Hour <= 0 ? "<color=red>" : "") +
				__instance.Time.ToString((m_twenty_four_hour_format.Value ? "HH:mm" : "hh:mm tt")) +
				(m_show_time_factor.Value ? m_time_factor_string : "");
			____timeBar.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, -180f, ((float) __instance.Time.Hour + (float) __instance.Time.Minute / 60f - 6f + 1f) / 20f));
			return false;
		}
	}

	[HarmonyPatch(typeof(GameManager), "DisableUI")]
	class HarmonyPatch_GameManager_DisableUI {

		private static void Postfix() {
			Plugin.m_is_ui_visible = false;
		}
	}

	[HarmonyPatch(typeof(GameManager), "EnableUI")]
	class HarmonyPatch_GameManager_EnableUI {

		private static void Postfix() {
			m_is_ui_visible = m_pause_in_ui.Value;
		}
	}
}