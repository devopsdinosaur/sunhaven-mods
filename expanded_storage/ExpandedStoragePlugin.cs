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


[BepInPlugin("devopsdinosaur.sunhaven.expanded_storage", "Expanded Storage", "0.0.1")]
public class ExpandedStoragePlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.expanded_storage");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_num_chest_slots;

	private const int CHEST_ORIGINAL_SLOT_COUNT = 30;
	private const int TEMPLATE_LEFT_ARROW_BUTTON = 0;
	private const int TEMPLATE_RIGHT_ARROW_BUTTON = 1;
	private const int TEMPLATE_SCROLL_VIEW = 2;

	private static Dictionary<int, GameObject> m_object_templates = new Dictionary<int, GameObject>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_num_chest_slots = this.Config.Bind<int>("General", "Chest Slot Count", 100, "Number of chest inventory slots");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.expanded_storage v0.0.1 " + (m_enabled.Value ? "" : "[inactive; disabled in config]") + " loaded.");
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

	public static GameObject templatize(GameObject original) {
		GameObject obj = GameObject.Instantiate(original, null);
		obj.SetActive(false);
		GameObject.DontDestroyOnLoad(obj);
		return obj;
	}

	[HarmonyPatch(typeof(Inventory), "Start")]
	class HarmonyPatch_Inventory_Start {

		private static void Postfix(Inventory __instance, Transform ____inventoryPanel) {
			/*
			try {
				GameObject sort_button = null;
				
				bool __enum_descendants_callback_find_sort_button__(Transform transform) {
					if (sort_button == null && transform.name == "SortChestButton") {
						sort_button = transform.gameObject; 
						return false;
					}
					return true;
				}

				GameObject create_navigation_button(GameObject left_obj, int template_id, string name, string text, Vector3 direction) {
					GameObject obj = GameObject.Instantiate<GameObject>(m_object_templates[template_id], left_obj.transform.parent);
					obj.name = name;
					obj.SetActive(true);
					RectTransform other_rect = left_obj.GetComponent<RectTransform>();
					RectTransform obj_rect = obj.GetComponent<RectTransform>();
					//obj.transform.Rotate(0, 0, 90f);
					obj_rect.localPosition = other_rect.localPosition + Vector3.right * (other_rect.rect.width * 2);
					obj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(new UnityAction(delegate {
						logger.LogInfo("hello there!");
					}));
					return obj;
				}

				if (!m_enabled.Value) {
					return;
				}
				Slot[] original_slots = ____inventoryPanel.GetComponentsInChildren<Slot>(includeInactive: true);
				if (original_slots.Length != CHEST_ORIGINAL_SLOT_COUNT) {
					return;
				}
				Slot slot0 = original_slots[0];
				__instance.maxSlots = m_num_chest_slots.Value;
				for (int index = original_slots.Length; index < __instance.maxSlots; index++) {
					GameObject.Instantiate(slot0.gameObject, slot0.transform.parent);
				}
				enum_descendants(____inventoryPanel.parent.parent.parent, __enum_descendants_callback_find_sort_button__);
				create_navigation_button(
					sort_button,
					TEMPLATE_LEFT_ARROW_BUTTON,
					"chest_page_button_up", 
					"up", 
					Vector3.up
				);
				return;
			} catch (Exception e) {
				logger.LogError("** Inventory_Start_Postfix ERROR - " + e);
			}
			*/
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

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		private static bool done = false;

		private static void Postfix() {
			if (Player.Instance == null || !Player.Instance.IsOwner) {
				return;
			}
			if (done) {
				return;
			}
			//done = false;
			Placeable table_placeable = null;
			foreach (Placeable placeable in Resources.FindObjectsOfTypeAll<Placeable>()) {
				if (placeable._itemData.id == ItemID.JamMaker) {
					table_placeable = placeable;
					break;
				}
			}
			Decoration temp_table = UnityEngine.Object.Instantiate(table_placeable.Decoration, new Vector3(0, 0, 0), Quaternion.identity, GameManager.DecorationContainer);
			temp_table.gameObject.SetActive(false);
			//foreach (CraftingTable table in Resources.FindObjectsOfTypeAll<CraftingTable>()) {
			//	logger.LogInfo(table);
			//}
			GameObject.Destroy(temp_table.gameObject);
			done = true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "Start")]
	class HarmonyPatch_CraftingTable_Start {

		private static void Postfix(CraftingUI ___craftingUI) {
			try {
				if (m_object_templates.ContainsKey(TEMPLATE_SCROLL_VIEW)) {
					return;
				}
				GameObject template = templatize(___craftingUI.craftingPane.parent.parent.parent.gameObject);
				//list_descendants(template.transform, null, 0);
				Transform content = template.transform.GetChild(0).GetChild(0);
				for (int index = 0; index < content.childCount; index++) {
					GameObject.Destroy(content.GetChild(index).gameObject);
				}
				m_object_templates[TEMPLATE_SCROLL_VIEW] = template;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_Start_Prefix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(Chest), "Interact")]
	class HarmonyPatch_Chest_Interact {

		private static bool Prefix(bool ___interacting, ChestData ___data, ref GameObject ___ui) {
			try {
				if (___interacting || ___data.inUse) {
					return true;
				}
				logger.LogInfo("\n\n\n***************************************************\n\n\n");
				Transform external_inventory = ___ui.transform.GetChild(1).transform;
				GameObject original_panel = external_inventory.GetChild(0).gameObject;
				RectTransform panel_rect = original_panel.GetComponent<RectTransform>();
				//GameObject.Destroy(external_inventory.GetChild(0).gameObject);
				GameObject scroll_view = GameObject.Instantiate(m_object_templates[TEMPLATE_SCROLL_VIEW], external_inventory);
				RectTransform scroll_rect = scroll_view.GetComponent<RectTransform>();
				scroll_rect.localPosition = panel_rect.localPosition;
				scroll_rect.sizeDelta = panel_rect.sizeDelta;
				scroll_view.SetActive(true);
				original_panel.SetActive(false);
				list_descendants(___ui.transform, null, 0);
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_Start_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	/*
	[HarmonyPatch(typeof(ItemIcon), "SetupTooltip")]
	class HarmonyPatch_ItemIcon_SetupTooltip {

		private static void Postfix(ItemIcon __instance) {
			logger.LogInfo(__instance);
		}
	}
	*/

	/*
	[HarmonyPatch(typeof(Inventory), "Start")]
	class HarmonyPatch_Inventory_Start {

		private static bool Prefix(
			Inventory __instance, 
			Transform ____inventoryPanel
		) {
			if (!m_enabled.Value) {
				return true;
			}
			int index;
			Slot[] original_slots = ____inventoryPanel.GetComponentsInChildren<Slot>(includeInactive: true);
			Slot slot0 = original_slots[0];
			__instance.maxSlots = m_num_slots.Value;
			for (index = original_slots.Length; index < __instance.maxSlots; index++) {
				GameObject.Instantiate(slot0.gameObject, slot0.transform.parent);
			}
			return true;
		}
	}

	private static Dictionary<int, Vector3> m_slot_positions = new Dictionary<int, Vector3>();

	[HarmonyPatch(typeof(ItemIcon), "Initialize")]
	class HarmonyPatch_ItemIcon_Initialize {

		private static void Postfix(ItemIcon __instance, Item item, int amount, Slot slot, int slotIndex) {
			logger.LogInfo(slotIndex);
			//if (!m_slot_positions.ContainsKey(__instance.GetHashCode())) {
			//	return;
			//}
			//__instance.transform.localPosition = m_slot_positions[__instance.GetHashCode()];
		}
	}

	[HarmonyPatch(typeof(PlayerInventory), "Initialize")]
	class HarmonyPatch_PlayerInventory_Initialize {

		private static void Postfix(
			PlayerInventory __instance,
			Transform ____inventoryPanel
		) {
			if (!m_enabled.Value || m_temporary_disable) {
				return;
			}
			RectTransform inventory_rect = ____inventoryPanel.GetComponent<RectTransform>();
			float scale = STARTING_SCALE;
			int slot_index = 0;
			int panel_width = (int) inventory_rect.rect.width;
			int panel_height = (int) inventory_rect.rect.height;
			int slot_width = -1;
			int slot_height = -1;
			int x = -1;
			int y = -1;
			
			bool find_slots(Transform transform) {
				Slot slot = transform.GetComponent<Slot>();
				if (slot == null) {
					return true;
				}
				if (slot_index >= ORIGINAL_SLOT_TOTAL_COUNT) {
					return false;
				}
				// RectTransform, UI.Image, NavigationElement, CanvasRenderer
				//list_component_types(transform);
				RectTransform rect = transform.GetComponent<RectTransform>();
				NavigationElement nav = transform.GetComponent<NavigationElement>();
				if (slot_index == 0) {
					scale = STARTING_SCALE;
					for (;;) {
						bool does_fit = true;
						slot_width = (int) (rect.rect.width * scale);
						slot_height = (int) (rect.rect.height * scale);
						if (slot_width < MIN_SLOT_WIDTH) {
							logger.LogError("** ERROR - unable to fit requested number of inventory slots due to panel space constraints (slot width would be less than minimum width of " + MIN_SLOT_WIDTH + ").");
							m_temporary_disable = true;
							return false;
						}
						logger.LogInfo("panel_width: " + panel_width + ", panel_height: " + panel_height);
						logger.LogInfo("orig_slot_width: " + rect.rect.width + ", orig_slot_height: " + rect.rect.height);
						logger.LogInfo("scale: " + scale);
						logger.LogInfo("slot_width: " + slot_width + ", slot_height: " + slot_height);
						x = 0;
						y = 0;
						for (int check_index = 0; check_index < m_num_slots.Value; check_index++) {
							//logger.LogInfo("slot" + check_index + " - x: " + x + ", y: " + y);
							if ((x += slot_width + SPACE_X) >= panel_width) {
								x = 0;
								if ((y += slot_height + SPACE_Y) + slot_height + SPACE_Y >= panel_height) {
									does_fit = false;
									break;
								}
							}
						}
						logger.LogInfo("does_fit: " + does_fit);
						if (does_fit) {
							break;
						}
						scale -= SCALE_DEC;
					}
					x = 0;
					y = 0;
				}
				logger.LogInfo("slot" + slot_index + " - x: " + x + ", y: " + y);
				rect.localScale = new Vector3(scale, scale);
				//m_slot_positions[= new Vector3(inventory_rect.rect.x + x, inventory_rect.rect.y + y);
				if ((x += slot_width + SPACE_X) >= panel_width) {
					x = 0;
					y += slot_height + SPACE_Y;
				}
				
				slot_index++;
				return true;
			}
			
			list_descendants(____inventoryPanel, find_slots, 0);
		}
	}
	*/

}