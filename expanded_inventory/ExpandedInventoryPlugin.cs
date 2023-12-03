using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading;
using ZeroFormatter;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;
using ZeroFormatter.Segments;
using System.IO;


[BepInPlugin("devopsdinosaur.sunhaven.expanded_inventory", "Expanded Inventory", "0.0.1")]
public class ExpandedStoragePlugin : BaseUnityPlugin {

	protected Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.expanded_inventory");
	public static ManualLogSource logger;
	
	protected static ConfigEntry<bool> m_enabled;
	
	protected void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.expanded_inventory v0.0.1 " + (m_enabled.Value ? "" : "[inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public class ExpandedInventory {

		protected const int NUM_SLOTS = 40;
		protected const int NUM_BAGS = 8;
		protected const int TEMPLATE_LEFT_ARROW_BUTTON = 0;
		protected const int TEMPLATE_RIGHT_ARROW_BUTTON = 1;

		protected static ExpandedInventory m_instance = null;
		protected static Dictionary<int, GameObject> m_object_templates = new Dictionary<int, GameObject>();

		protected Mutex m_thread_lock = new Mutex();
		protected PlayerInventory m_player_inventory;
		protected Transform m_inventory_panel;
		protected Slot[] m_slots;
		protected Transform[] m_slot_transforms = new Transform[NUM_SLOTS];
		protected ChestData m_data = new ChestData();
		protected Transform[] m_bag_buttons = new Transform[NUM_BAGS];
		protected Dictionary<int, int> m_bag_trashslot_map = new Dictionary<int, int>();
		protected bool m_bag_can_be_clicked = true;
		protected int m_current_bag_index = 0;

		public static ExpandedInventory Instance {
			get {
				return (m_instance != null ? m_instance : m_instance = new ExpandedInventory());
			}
		}

		ExpandedInventory() {
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

		protected void get_thread_lock() {
			this.m_thread_lock.WaitOne();
		}

		protected void release_thread_lock() {
			this.m_thread_lock.ReleaseMutex();
		}

		public static GameObject templatize(GameObject original) {
			GameObject obj = GameObject.Instantiate(original, null);
			obj.SetActive(false);
			GameObject.DontDestroyOnLoad(obj);
			return obj;
		}

		public void attach_to_player_inventory(
			Inventory player_inventory, 
			Transform inventory_panel,
			Slot[] slots
		) {
			Transform trash_button = null;
			
			bool find_trash_button(Transform transform) {
				if (transform.name == "TrashButton") {
					trash_button = transform;
					return false;
				}
				return true;
			}

			void create_navigation_button(Transform parent, int index) {
				GameObject obj = GameObject.Instantiate<GameObject>(trash_button.gameObject, parent);
				obj.name = "ExpandedInventory_BagButton_" + index;
				obj.SetActive(true);
				foreach (Component component in obj.GetComponents<Component>()) {
					if (component is Popup) {
						Popup popup = (Popup) component;
						if (popup.text != "") {
							popup.text = $"Inventory Bag #{index + 1}";
							popup.description = "Click to open this bag.  Drop an item here to put it in the bag.";
						}
					} else if (component is UIButton) {
						UIButton button = (UIButton) component;
						button.defaultImage = button.hoverOverImage = button.pressedImage = ItemDatabase.items[(index == 2 ? ItemID.MoneyBagForageable : ItemID.SmallMoneyBag)].icon;
					} else if (component is TrashSlot) {
						this.m_bag_trashslot_map[component.GetHashCode()] = index;
					}
				}
				RectTransform other_rect = this.m_inventory_panel.GetComponent<RectTransform>();
				RectTransform obj_rect = obj.GetComponent<RectTransform>();
				obj_rect.localPosition = new Vector3(-295 + (index * 30), 255, 0);
				obj_rect.localScale = new Vector3(0.85f, 0.85f, 1f);
				obj.AddComponent<UnityEngine.UI.Button>().onClick.AddListener((UnityAction) delegate {
					ExpandedInventory.Instance.change_inventory_bag(index);
				});
			}

			try {
				this.m_player_inventory = (PlayerInventory) player_inventory;
				this.m_inventory_panel = inventory_panel;
				this.m_slots = slots;
				this.m_bag_can_be_clicked = true;
				this.m_current_bag_index = 0;
				enum_descendants(this.m_inventory_panel.parent.parent, find_trash_button);
				logger.LogInfo($"trash_button: {trash_button}, trash_button.parent: {trash_button.parent}");
				for (int index = 0; index < NUM_BAGS; index++) {
					create_navigation_button(trash_button.parent, index);
				}
			} catch (Exception e) {
				logger.LogError("** ExpandedInventory.attach_to_player_inventory ERROR - " + e);
			}
		}

		public bool add_item_to_bag(int bag_index, Item item) {
			try {

				return false;
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.add_item_to_bag ERROR - {e}");
			}
			return false;
		}

		protected bool drop_item_in_bag(TrashSlot trash_slot) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				ItemIcon icon = Inventory.CurrentItemIcon;
				if (icon == null) {
					return false;
				}
				ItemData data = ItemDatabase.GetItemData(icon.item);
				if (!m_bag_trashslot_map.ContainsKey(trash_slot.GetHashCode())) {
					return false;
				}
				this.m_bag_can_be_clicked = false;
				int bag_index = m_bag_trashslot_map[trash_slot.GetHashCode()];
				Item item = data.GetItem();
				logger.LogInfo($"ExpandedInventory.drop_item_in_bag({bag_index}, {item})");
				if (this.add_item_to_bag(bag_index, item)) {
					icon.RemoveItemIcon();
					this.m_player_inventory.UpdateInventory();
				}
				return false;
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.drop_item_in_bag ERROR - {e}");
			}
			return true;
		}

		[HarmonyPatch(typeof(TrashSlot), "OnPointerDown")]
		class HarmonyPatch_TrashSlot_OnPointerDown {
			
			private static bool Prefix(TrashSlot __instance) {
				return ExpandedInventory.Instance.drop_item_in_bag(__instance);
			}
		}

		protected void change_inventory_bag(int bag_index) {
			try {
				if (!m_enabled.Value) {
					return;
				}
				if (!this.m_bag_can_be_clicked) {
					this.m_bag_can_be_clicked = true;
					return;
				}
				logger.LogInfo($"change_inventory_page({bag_index})");
				
				
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.change_inventory_bag ERROR - {e}");
			}
		}

		public void save_to_file() {
			try {
				string out_file_path = $"{Application.persistentDataPath}/Saves/{GameSave.Instance.CurrentSave.fileName}.expanded_inventory";
				logger.LogInfo($"expanded_inventory.save_to_file('{out_file_path}')");
				File.WriteAllBytes(
					out_file_path,
					GameSave.CompressBytes(ZeroFormatterSerializer.Serialize(this.m_data))
				);
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.save_to_file ERROR - {e}");
			}
		}
	}

	[HarmonyPatch(typeof(Inventory), "Start")]
	class HarmonyPatch_Inventory_Start {

		protected static bool Prefix(
			Inventory __instance, 
			Transform ____inventoryPanel,
			Slot[]  ____slots
		) {
			if (m_enabled.Value && __instance is PlayerInventory) {
				ExpandedInventory.Instance.attach_to_player_inventory(
					__instance, 
					____inventoryPanel,
					____slots
				);
			}
			return true;
		}
	}
}