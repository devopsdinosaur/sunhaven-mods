
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using TMPro;
using System.IO;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	/*
	[HarmonyPatch(typeof(NPCManager), "FixedUpdate")]
	class HarmonyPatch_NPCManager_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = 0f;
		static bool m_done = false;

		private static bool Prefix(ref NPCManager __instance, bool ___initialized, Dictionary<string, NPCAI> ____npcs) {
			if ((m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return true;
			}
			m_elapsed = 0f;
			if (SceneManager.sceneCount < 2 || !___initialized || GameManager.ApplicationQuitting) {
				return true;
			}
			if (m_done) {
				return true;
			}
			foreach (NPCAI npc in ____npcs.Values) {
				logger.LogInfo(npc.ActualNPCName);
			}
			m_done = true;
			return true;
		}
	}
	*/

	public static bool list_ancestors(Transform parent, Func<Transform, bool> callback, int indent) {
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
			list_ancestors(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_ancestors(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_ancestors(child, callback);
		}
		return true;
	}

	public static Transform find_child_by_name(Transform parent, string name) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			if ((child = parent.GetChild(index)).gameObject.name == name) {
				return child;
			}
		}
		return null;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

	[HarmonyPatch(typeof(PlayerInventory), "Update")]
	class HarmonyPatch_PlayerInventory_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = 0f;
		static bool m_done = false;

		public static bool list_ancestors_callback(Transform obj) {
			if (obj.gameObject.name == "ItemImage") {
				
			}
			return true;
		}

		private static void Postfix(ref PlayerInventory __instance, Transform ____inventoryPanel) {
			if ((m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return;
			}
			m_elapsed = 0f;
			if (m_done) {
				return;
			}
			m_done = true;
			//Plugin.list_ancestors(____inventoryPanel.parent, list_ancestors_callback, 0);
			GameObject drop_button =
				Plugin.find_child_by_name(
					Plugin.find_child_by_name(
						____inventoryPanel.parent,
						"UtilityButtons"
					),
					"DropButton"
				).gameObject;
			GameObject sell_button = GameObject.Instantiate<GameObject>(drop_button, drop_button.transform.parent);
			sell_button.transform.position = drop_button.transform.position + (Vector3.left * sell_button.GetComponent<RectTransform>().rect.width * 3);
			//Plugin.list_component_types(sell_button.transform);

			//logger.LogInfo(ItemDatabase.GetItemData(ItemDatabase.GetID("smallmoneybag")).icon.texture.height);

			//sell_button.GetComponent<UnityEngine.UI.Image>().sprite = ItemDatabase.GetItemData(ItemDatabase.GetID("smallmoneybag")).icon;
			//foreach (KeyValuePair<string, int> id in ItemDatabase.ids) {
			//	logger.LogInfo(id.Key + ": " + id.Value);
			//}

		}
	}

	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_MainMenuController_PlayGame {

		private static bool Prefix() {
			GameSave.Instance.SetProgressBoolCharacter("BabyDragon", value: true);
			GameSave.Instance.SetProgressBoolCharacter("BabyTiger", value: true);
			GameSave.Instance.SetProgressBoolCharacter("WithergateMask1", value: true);
			GameSave.Instance.SetProgressBoolCharacter("SunArmor", value: true);
			GameSave.Instance.SetProgressBoolCharacter("GoldRecord", value: true);
			return true;
		}
	}

	[HarmonyPatch(typeof(MainMenuController), "HomeMenu")]
	class HarmonyPatch_MainMenuController_HomeMenu {

		private static Transform m_play_button = null;
		private static int m_latest_save_index = -1;

		public static bool enum_ancestors_callback(Transform transform) {
			if (transform.name.StartsWith("PlayButton")) {
				if (transform.GetChild(0).GetComponent<TextMeshProUGUI>().text == "Play") {
					m_play_button = transform;
					return false;
				}
			}
			return true;
		}

		private static void Postfix(ref MainMenuController __instance, ref GameObject ___homeMenu, ref GameObject ___loadCharacterMenu) {
			string saves_dir = Path.Combine(Application.persistentDataPath, "Saves");
			DateTime latest_timestamp = DateTime.Now;
			string full_path;
			GameObject continue_button = null;

			if (m_play_button != null || !Directory.Exists(saves_dir)) {
				return;
			}
			for (int index = 0; index < GameSave.Instance.Saves.Count; index++) {
				full_path = Path.Combine(saves_dir, GameSave.Instance.Saves[index].fileName);
				if (m_latest_save_index == -1 || File.GetLastWriteTime(full_path) > latest_timestamp) {
					m_latest_save_index = index;
					latest_timestamp = File.GetLastWriteTime(full_path);
				}
			}
			Plugin.enum_ancestors(___homeMenu.transform, enum_ancestors_callback);
			if (m_latest_save_index == -1 || m_play_button == null) {
				return;
			}
			list_component_types(m_play_button);
			continue_button = GameObject.Instantiate<GameObject>(m_play_button.gameObject, m_play_button.parent);
			continue_button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Continue";
			
		}
	}
}