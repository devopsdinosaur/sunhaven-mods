
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.time_machine", "Time Machine", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.time_machine");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<string> m_hotkey_modifier;
	private static ConfigEntry<string> m_hotkey_time_stop_toggle;
	private static ConfigEntry<string> m_hotkey_time_speed_up;
	private static ConfigEntry<string> m_hotkey_time_speed_down;
	public static ConfigEntry<float> m_time_speed;
	private static ConfigEntry<float> m_time_speed_delta;
	
	private const int HOTKEY_MODIFIER = 0;
	private const int HOTKEY_TIME_STOP_TOGGLE = 1;
	private const int HOTKEY_TIME_SPEED_UP = 2;
	private const int HOTKEY_TIME_SPEED_DOWN = 3;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

	public static float m_time_stop_multiplier = 1f;
	public static bool m_is_ui_hidden = false;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.time_machine v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_hotkey_modifier = this.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_stop_toggle = this.Config.Bind<string>("General", "Time Start/Stop Toggle Hotkey", "Alpha0,Keypad0", "Comma-separated list of Unity Keycodes, any of which will toggle the passage of time.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_speed_up = this.Config.Bind<string>("General", "Time Speed Up Hotkey", "Equals,KeypadPlus", "Comma-separated list of Unity Keycodes, any of which will increase the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_hotkey_time_speed_down = this.Config.Bind<string>("General", "Time Speed Down Hotkey", "Minus,KeypadMinus", "Comma-separated list of Unity Keycodes, any of which will decrease the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
		m_time_speed = this.Config.Bind<float>("General", "Initial Time Speed", 0.25f, "Initial time speed (float, 0.5f == 40min, 1.66f == 15 min).");
		m_time_speed_delta = this.Config.Bind<float>("General", "Time Speed Delta", 0.05f, "Change in time speed with each up/down hotkey tick (float).");
		m_hotkeys = new Dictionary<int, List<KeyCode>>();
		set_hotkey(m_hotkey_modifier.Value, HOTKEY_MODIFIER);
		set_hotkey(m_hotkey_time_stop_toggle.Value, HOTKEY_TIME_STOP_TOGGLE);
		set_hotkey(m_hotkey_time_speed_up.Value, HOTKEY_TIME_SPEED_UP);
		set_hotkey(m_hotkey_time_speed_down.Value, HOTKEY_TIME_SPEED_DOWN);
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

		private static void Postfix(ref Player __instance) {
			if (!m_enabled.Value) {
				return;
			}
			if (!is_modifier_hotkey_down()) {
				return;
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
				notify("time_speed (" + 
					m_time_speed.Value + 
					") * on_off_multiplier (" + 
					m_time_stop_multiplier + 
					") == " + Settings.DaySpeedMultiplier + " (real sec / game min)"
				);
			}
		}
	}

	[HarmonyPatch(typeof(Settings))]
	[HarmonyPatch("DaySpeedMultiplier", MethodType.Getter)]
	class HarmonyPatch_Wish_Settings_DaySpeedMultiplier {

		private static bool Prefix(ref float __result) {
			__result = (m_is_ui_hidden ? 0f : Plugin.m_time_speed.Value * Plugin.m_time_stop_multiplier);
			return false;
		}
	}

	[HarmonyPatch(typeof(DayCycle), "UpdateTimeText")]
	class HarmonyPatch_DayCycle {

		private const float CHECK_FREQUENCY = 1.0f;
		private static float m_elapsed = 0f;
		private static DateTime m_last_system_time = DateTime.MinValue;
		private static DateTime m_last_game_time = DateTime.MinValue;
		private static string m_time_factor_string = "";

		private static bool Prefix(
			ref DayCycle __instance, 
			ref TextMeshProUGUI ____timeTMP, 
			ref Transform ____timeBar
		) {
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
				__instance.Time.ToString("hh:mm tt") +
				m_time_factor_string;
			____timeBar.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, -180f, ((float) __instance.Time.Hour + (float) __instance.Time.Minute / 60f - 6f + 1f) / 20f));
			return false;
		}
	}

	[HarmonyPatch(typeof(GameManager), "DisableUI")]
	class HarmonyPatch_GameManager_DisableUI {

		private static void Postfix() {
			Plugin.m_is_ui_hidden = true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "EnableUI")]
	class HarmonyPatch_GameManager_EnableUI {

		private static void Postfix() {
			Plugin.m_is_ui_hidden = false;
		}
	}
}