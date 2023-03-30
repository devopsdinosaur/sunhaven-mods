using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using UnityEngine.Events;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.expanded_storage", "Expanded Storage", "0.0.1")]
public class ExpandedStoragePlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.expanded_storage");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_num_slots;

	private const int ORIGINAL_SLOT_COL_COUNT = 8;
	private const int ORIGINAL_SLOT_ROW_COUNT = 5;
	private const int ORIGINAL_SLOT_TOTAL_COUNT = ORIGINAL_SLOT_COL_COUNT * ORIGINAL_SLOT_ROW_COUNT;
	private const int SPACE_X = 4;
	private const int SPACE_Y = 4;
	private const float STARTING_SCALE = 1f;
	private const float SCALE_DEC = 0.05f;
	private const int MIN_SLOT_WIDTH = 4;

	// this will be true if the scaling algorithm is unable to fit requested slots
	private static bool m_temporary_disable = false;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_num_slots = this.Config.Bind<int>("General", "Slot Count", 100, "Number of inventory slots");
			m_temporary_disable = false;
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

}