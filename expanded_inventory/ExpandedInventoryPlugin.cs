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
using System.Reflection;


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

		protected const int NUM_ACTION_BAR_SLOTS = 10;
		protected const int NUM_SLOTS = 40;
		protected const int NUM_BAGS = 10;
		protected const int BAG_ICON_ID_SELECTED = ItemID.MoneyBagForageable;
		protected const int BAG_ICON_ID_UNSELECTED = ItemID.SmallMoneyBag;

		protected static ExpandedInventory m_instance = null;
		protected static Dictionary<int, string> m_item_id_strings = null;

		protected Mutex m_thread_lock = new Mutex();
		protected PlayerInventory m_player_inventory = null;
		protected Transform m_inventory_panel;
		protected Slot[] m_slots;
		protected Transform[] m_slot_transforms = new Transform[NUM_SLOTS];
		protected ChestData m_data = new ChestData();
		protected UIButton[] m_bag_buttons = new UIButton[NUM_BAGS];
		protected Dictionary<int, int> m_bag_trashslot_map = new Dictionary<int, int>();
		protected bool m_did_save_file_exist = false;
		protected bool m_bag_can_be_clicked = true;
		protected int m_current_bag_index = 0;

		public static ExpandedInventory Instance {
			get {
				return (m_instance != null ? m_instance : m_instance = new ExpandedInventory());
			}
		}

		ExpandedInventory() {
			if (m_item_id_strings != null) {
				return;
			}
			m_item_id_strings = new Dictionary<int, string>();
			foreach (FieldInfo field_info in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				if (field_info.IsLiteral && !field_info.IsInitOnly) {
					m_item_id_strings[(int) field_info.GetRawConstantValue()] = field_info.Name;
				}
			}
		}

		public static string item_name_from_id(int id) {
			return (m_item_id_strings != null && m_item_id_strings.ContainsKey(id) ? m_item_id_strings[id] : $"[item_id: {id}");
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

		public void attach_to_player_inventory(Inventory player_inventory, Transform inventory_panel,Slot[] slots) {
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
					} else if (component is UIButton button) {
						this.m_bag_buttons[index] = button;
						button.defaultImage = button.hoverOverImage = button.pressedImage = ItemDatabase.items[(index == 0 ? BAG_ICON_ID_SELECTED : BAG_ICON_ID_UNSELECTED)].icon;
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
				this.m_did_save_file_exist = false;
				this.m_bag_can_be_clicked = true;
				this.m_current_bag_index = 0;
				this.m_player_inventory.OnInventoryUpdated = (UnityAction) Delegate.Combine(this.m_player_inventory.OnInventoryUpdated, new UnityAction(this.on_update_inventory));
				enum_descendants(this.m_inventory_panel.parent.parent, find_trash_button);
				for (int index = 0; index < NUM_BAGS; index++) {
					create_navigation_button(trash_button.parent, index);
				}
				this.load_from_file();
			} catch (Exception e) {
				logger.LogError("** ExpandedInventory.attach_to_player_inventory ERROR - " + e);
			}
		}

		protected void on_update_inventory() {
			//logger.LogInfo($"expanded_inventory.on_update_inventory()");
		}

		public void on_load_inventory(PlayerInventory player_inventory) {
			if (!player_inventory == this.m_player_inventory) {
				return;
			}
			logger.LogInfo($"expanded_inventory.on_load_inventory(m_did_save_file_exist: {this.m_did_save_file_exist})");
			short slot_index;
			SlotItemData slot_item;
			if (this.m_did_save_file_exist) {
				for (slot_index = 0; slot_index < NUM_SLOTS; slot_index++) {
					slot_item = player_inventory.Items[slot_index + NUM_ACTION_BAR_SLOTS];
					slot_item.item = this.m_data.items[slot_index].Item;
					slot_item.amount = this.m_data.items[slot_index].Amount;
					logger.LogInfo($"--> slot {slot_index} = {item_name_from_id(slot_item.item.ID())} (amount: {slot_item.amount})");
				}
				return;
			}
			for (slot_index = 0; slot_index < NUM_SLOTS; slot_index++) {
				slot_item = player_inventory.Items[slot_index + NUM_ACTION_BAR_SLOTS];
				this.m_data.items[slot_index] = new InventoryItemData();
				this.m_data.items[slot_index].Item = slot_item.item;
				this.m_data.items[slot_index].Amount = slot_item.amount;
				logger.LogInfo($"--> slot {slot_index} = {item_name_from_id(slot_item.item.ID())} (amount: {slot_item.amount})");
			}
		}

		public bool add_item_to_bag(int bag_index, InventoryItemData item, ref short key) {
			try {
				logger.LogInfo($"add_item_to_bag(bag_index: {bag_index}, item: {item_name_from_id(item.Item.ID())})");
				InventoryItemData check_item;
				for (int slot_index = 0; slot_index < NUM_SLOTS; slot_index++) {
					key = (short) ((bag_index << 8) + slot_index);
					check_item = this.m_data.items[key];
					logger.LogInfo($"--> add_item_to_bag - slot {slot_index} == {(check_item != null ? item_name_from_id(check_item.Item.ID()) : "null")}.");
					if (check_item == null) {
						logger.LogInfo($"--> add_item_to_bag - new stack for item '{item.Item}' in slot {slot_index}.");
						this.m_data.items[key] = item;
						return true;
					}
					if (check_item.Item.ID() == item.Item.ID() && check_item.Amount < ItemDatabase.items[check_item.Item.ID()].stackSize) {
						logger.LogInfo($"--> add_item_to_bag - added item to existing stack for item '{item_name_from_id(item.Item.ID())}' in slot {slot_index}.");
						this.m_data.items[key].Amount += item.Amount;
						return true;
					}
				}
				logger.LogInfo($"--> add_item_to_bag - no space in bag.");
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
				InventoryItemData item = new InventoryItemData();
				item.Item = icon.item;
				item.Amount = icon.amount;
				if (!m_bag_trashslot_map.ContainsKey(trash_slot.GetHashCode())) {
					return false;
				}
				this.m_bag_can_be_clicked = false;
				int bag_index = m_bag_trashslot_map[trash_slot.GetHashCode()];
				if (bag_index == this.m_current_bag_index) {
					return false;
				}
				logger.LogInfo($"ExpandedInventory.drop_item_in_bag({bag_index}, {item_name_from_id(item.Item.ID())})");
				short key = -1;
				if (this.add_item_to_bag(bag_index, item, ref key)) {
					icon.RemoveItemIcon();
					this.m_player_inventory.UpdateInventory();
				}
				return false;
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.drop_item_in_bag ERROR - {e}");
			}
			return true;
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
				if (bag_index == this.m_current_bag_index) {
					return;
				}
				logger.LogInfo($"expanded_inventory.change_inventory_page({bag_index})");
				int index;
				this.m_current_bag_index = bag_index;
				this.m_player_inventory.SavePlayerInventory();
				for (index = 0; index < NUM_BAGS; index++) {
					this.m_bag_buttons[index].defaultImage = 
						this.m_bag_buttons[index].hoverOverImage = 
						this.m_bag_buttons[index].pressedImage = 
						ItemDatabase.items[(index == bag_index ? BAG_ICON_ID_SELECTED : BAG_ICON_ID_UNSELECTED)].icon;
					this.m_bag_buttons[index].image.sprite = this.m_bag_buttons[index].defaultImage;
				}
				//SlotItemData slot;
				InventoryItemData item;
				int slot_index = NUM_ACTION_BAR_SLOTS;
				for (index = 0; index < NUM_SLOTS; index++) {
					item = this.m_data.items[(short) ((bag_index << 8) + index)];
					logger.LogInfo($"this.m_data.items[(short) ((bag_index << 8) + index)] = {this.m_data.items[(short) ((bag_index << 8) + index)]}");
					//slot = this.m_player_inventory.Items[index + NUM_ACTION_BAR_SLOTS];
					//slot.item = item.Item;
					//slot.amount = item.Amount;
					this.m_player_inventory.RemoveItemAt(slot_index);
					this.m_player_inventory.AddItem(item.Item.ID(), item.Amount, slot_index++, true);
					logger.LogInfo($"--> slot {index} = {item_name_from_id(item.Item.ID())} (amount: {item.Amount})");
				}
				this.m_player_inventory.UpdateInventory();
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.change_inventory_bag ERROR - {e}");
			}
		}

		public void load_from_file() {
			try {
				string path = $"{Application.persistentDataPath}/Saves/{GameSave.Instance.CurrentSave.fileName}.expanded_inventory";
				this.m_did_save_file_exist = File.Exists(path);
				logger.LogInfo($"expanded_inventory.load_from_file(path: '{path}', m_did_save_file_exist: {this.m_did_save_file_exist})");
				if (this.m_did_save_file_exist) {
					this.m_data = ZeroFormatterSerializer.Deserialize<ChestData>(GameSave.DecompressBytes(File.ReadAllBytes(path)));
				}
				else {
					this.m_data = new ChestData();
				}
				short key;
				for (int bag_index = 0; bag_index < NUM_BAGS; bag_index++) {
					for (int slot_index = 0; slot_index < NUM_SLOTS; slot_index++) {
						key = (short) ((bag_index << 8) + slot_index);
						if (!this.m_data.items.ContainsKey(key)) {
							this.m_data.items[key] = new InventoryItemData();
							this.m_data.items[key].Item = new NormalItem(0);
						}
					}
				}
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.save_to_file ERROR - {e}");
			}
		}

		public void save_to_file() {
			try {
				string path = $"{Application.persistentDataPath}/Saves/{GameSave.Instance.CurrentSave.fileName}.expanded_inventory";
				logger.LogInfo($"expanded_inventory.save_to_file('{path}')");
				File.WriteAllBytes(path, GameSave.CompressBytes(ZeroFormatterSerializer.Serialize(this.m_data)));
			} catch (Exception e) {
				logger.LogError($"** ExpandedInventory.save_to_file ERROR - {e}");
			}
		}

		[HarmonyPatch(typeof(Inventory), "Start")]
		class HarmonyPatch_Inventory_Start {

			protected static bool Prefix(Inventory __instance, Transform ____inventoryPanel, Slot[]  ____slots) {
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

		[HarmonyPatch(typeof(Inventory), "LoadInventory")]
		class HarmonyPatch_Inventory_LoadInventory {

			protected static void Postfix(Inventory __instance) {
				if (m_enabled.Value && __instance is PlayerInventory player_inventory) {
					ExpandedInventory.Instance.on_load_inventory(player_inventory);
				}
			}
		}
		
		[HarmonyPatch(typeof(PlayerInventory), "SavePlayerInventory")]
		class HarmonyPatch_PlayerInventory_SavePlayerInventory {

			protected static bool Prefix() {
				if (m_enabled.Value) {
					ExpandedInventory.Instance.save_to_file();
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(TrashSlot), "OnPointerDown")]
		class HarmonyPatch_TrashSlot_OnPointerDown {
			
			private static bool Prefix(TrashSlot __instance) {
				return ExpandedInventory.Instance.drop_item_in_bag(__instance);
			}
		}
	}
}