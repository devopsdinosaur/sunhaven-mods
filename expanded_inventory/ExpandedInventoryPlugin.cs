using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;


[BepInPlugin("devopsdinosaur.sunhaven.expanded_inventory", "Expanded Inventory", "0.0.1")]
public class ExpandedStoragePlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.expanded_inventory");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_num_inventory_slots;

	private const int CHEST_ORIGINAL_SLOT_COUNT = 30;
	private const int TEMPLATE_LEFT_ARROW_BUTTON = 0;
	private const int TEMPLATE_RIGHT_ARROW_BUTTON = 1;
	private const int TEMPLATE_SCROLL_VIEW = 2;
	private const int TEMPLATE_ITEM_ROW_PANEL = 3;

	private static Dictionary<int, GameObject> m_object_templates = new Dictionary<int, GameObject>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_num_inventory_slots = this.Config.Bind<int>("General", "Inventory Slot Count", 100, "Number of inventory slots");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.expanded_inventory v0.0.1 " + (m_enabled.Value ? "" : "[inactive; disabled in config]") + " loaded.");
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
			logger.LogInfo(indent_string + child.gameObject);
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

	public static GameObject templatize(GameObject original) {
		GameObject obj = GameObject.Instantiate(original, null);
		obj.SetActive(false);
		GameObject.DontDestroyOnLoad(obj);
		return obj;
	}

	[HarmonyPatch(typeof(Inventory), "Start")]
	class HarmonyPatch_Inventory_Start {

		private static void Postfix(
			PlayerInventory __instance, 
			Transform ____inventoryPanel
		) {
			try {
				if (!m_enabled.Value) {
					return;
				}
				// up button under Slot35 (right align)
				// down button under Slot36 (left align)
				list_descendants(____inventoryPanel, null, 0);
			} catch (Exception e) {
				logger.LogError("** Inventory_Start_Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerSettings), "SetupUI")]
	class HarmonyPatch_PlayerSettings_SetupUI {

		private static void Postfix(Slider ___daySpeedSlider) {

			bool find_buttons_callback(Transform transform) {
				if (transform.name == "SliderLeft") {
					m_object_templates[TEMPLATE_LEFT_ARROW_BUTTON] = templatize(transform.gameObject);
				} else if (transform.name == "SliderRight") {
					m_object_templates[TEMPLATE_RIGHT_ARROW_BUTTON] = templatize(transform.gameObject);
				}
				return !(m_object_templates.ContainsKey(TEMPLATE_LEFT_ARROW_BUTTON) && m_object_templates.ContainsKey(TEMPLATE_RIGHT_ARROW_BUTTON));
			}

			if (m_object_templates.ContainsKey(TEMPLATE_LEFT_ARROW_BUTTON) && m_object_templates.ContainsKey(TEMPLATE_RIGHT_ARROW_BUTTON)) {
				return;
			}
			list_descendants(___daySpeedSlider.transform, find_buttons_callback, 0);
		}
	}

}