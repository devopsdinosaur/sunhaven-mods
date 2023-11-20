using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using TMPro;
using System;
using UnityEngine.Events;
using System.Threading;
using UnityEngine.UI;


[BepInPlugin("devopsdinosaur.sunhaven.craft_from_storage", "Craft From Storage", "0.0.17")]
public class CraftFromStoragePlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.craft_from_storage");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_use_inventory_first;
	private static ConfigEntry<bool> m_transfer_from_action_bar;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_use_inventory_first = this.Config.Bind<bool>("General", "Use Inventory First", true, "If true then crafting stations will pull from inventory before storage chests.");
			m_transfer_from_action_bar = this.Config.Bind<bool>("General", "Transfer From Action Bar", false, "If true then the transfer similar/same buttons will also pull from the action bar.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.craft_from_storage v0.0.17" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public class OmniChest {

		private static OmniChest m_instance = null;
		private List<int> m_added_hashes = null;
		private Dictionary<int, List<SlotItemData>> m_items = null;
		private List<Inventory> m_inventories = null;
		private List<Chest> m_chests = null;
		private Chest m_current_chest = null;
		private const float CHECK_FREQUENCY = 1.0f;
		private float m_elapsed = CHECK_FREQUENCY;
		private GameObject m_transfer_similar_button = null;
		private GameObject[] m_chest_navigate_buttons = new GameObject[2];
		private Mutex m_thread_lock = new Mutex();
		private const int TEMPLATE_LEFT_ARROW_BUTTON = 0;
		private const int TEMPLATE_RIGHT_ARROW_BUTTON = 1;
		private static Dictionary<int, GameObject> m_object_templates = new Dictionary<int, GameObject>();

		public static OmniChest Instance {
			get {
				return (m_instance != null ? m_instance : m_instance = new OmniChest());
			}
		}

		private OmniChest() {
		}

		private void get_thread_lock() {
			this.m_thread_lock.WaitOne();
		}

		private void release_thread_lock() {
			this.m_thread_lock.ReleaseMutex();
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

		[HarmonyPatch(typeof(PlayerSettings), "SetupUI")]
		class HarmonyPatch_PlayerSettings_SetupUI {

			private static void Postfix(Slider ___daySpeedSlider) {

				GameObject templatize(GameObject original) {
					GameObject obj = GameObject.Instantiate(original, null);
					obj.SetActive(false);
					GameObject.DontDestroyOnLoad(obj);
					return obj;
				}

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
				enum_descendants(___daySpeedSlider.transform, find_buttons_callback);
			}
		}

		[HarmonyPatch(typeof(Chest), "Interact")]
		class HarmonyPatch_Chest_Interact {

			private static void Postfix(Chest __instance) {
				OmniChest.Instance.get_thread_lock();
				OmniChest.Instance.m_current_chest = __instance;
				OmniChest.Instance.release_thread_lock();
			}
		}

		private void create_send_similar_button(Transform chest_transform, Inventory player_inventory) {
			GameObject chest_transfer_similar_button = null;
			GameObject sort_button = null;
			
			bool __enum_descendants_callback_find_same_button__(Transform transform) {
				if (transform.name == "TransferSimilarToChestButton") {
					chest_transfer_similar_button = transform.gameObject;
					return false;
				} 
				return true;
			}

			bool __enum_descendants_callback_find_sort_button__(Transform transform) {
				if (sort_button == null && transform.name == "SortButton") {
					sort_button = transform.gameObject; 
					return false;
				}
				return true;
			}

			enum_descendants(chest_transform, __enum_descendants_callback_find_same_button__);
			enum_descendants(player_inventory.transform, __enum_descendants_callback_find_sort_button__);
			this.m_transfer_similar_button = GameObject.Instantiate<GameObject>(chest_transfer_similar_button, sort_button.transform.parent);
			this.m_transfer_similar_button.GetComponent<RectTransform>().position =
				sort_button.GetComponent<RectTransform>().position +
				Vector3.right * sort_button.GetComponent<RectTransform>().rect.width * 3;
			this.m_transfer_similar_button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate {
				this.transfer_similar_items();
			});
		}

		public void transfer_similar_items() {
			this.refresh();
			this.get_thread_lock();
			try {
				Inventory player_inventory = Player.Instance.Inventory;
				Inventory inventory;
				for (int index = (m_use_inventory_first.Value ? 1 : 0); index < (!m_use_inventory_first.Value ? this.m_inventories.Count - 1 : this.m_inventories.Count); index++) {
					try {
						inventory = this.m_inventories[index];
						player_inventory.TransferPlayerSimilarToOtherInventory(inventory);
						inventory.UpdateInventory();
						player_inventory.UpdateInventory();
					} catch {
					}
				}
			} catch (Exception e) {
				logger.LogError("** transfer_similar_items ERROR - " + e);
			}
			this.release_thread_lock();
			this.refresh();
		}

		public void refresh() {
			this.get_thread_lock();
			try {
				if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
					return;
				}
				Inventory player_inventory = Player.Instance.Inventory;
				m_elapsed = 0f;
				int hash = 0;
				this.m_added_hashes = new List<int>();
				this.m_items = new Dictionary<int, List<SlotItemData>>();
				this.m_inventories = new List<Inventory>();
				this.m_chests = new List<Chest>();
				if (m_use_inventory_first.Value) {
					this.add_inventory(player_inventory);
				}
				foreach (KeyValuePair<Vector3Int, Decoration> kvp in GameManager.Instance.objects) {
					if (!(kvp.Value is Chest) || this.m_added_hashes.Contains(hash = kvp.Value.GetHashCode())) {
						continue;
					}
					string type_name = kvp.Value.GetType().Name;
					if (!(type_name == "Chest" || type_name == "AutoCollector" || type_name == "Hopper")) {
						continue;
					}
					if (this.m_transfer_similar_button == null) {
						this.create_send_similar_button(kvp.Value.transform, player_inventory);
					}
					this.m_added_hashes.Add(hash);
					this.add_inventory(((Chest) kvp.Value).sellingInventory);
					this.add_chest((Chest) kvp.Value);
				}
				if (!m_use_inventory_first.Value) {
					this.add_inventory(player_inventory);
				}
				Popup popup = this.m_transfer_similar_button.GetComponent<Popup>();
				popup.text = "Zone Send Similar";
				popup.description = "Send similar items to nearby chests within the current zone (note: house and outside are different zones).\nNearby chests: " + Math.Max(this.m_inventories.Count - 1, 0);
			} catch {
			}
			this.release_thread_lock();
		}

		private void add_chest(Chest chest) {
			GameObject chest_title = null;

			bool __enum_descendants_callback_find_chest_title__(Transform transform) {
				if (transform.name == "ExternalInventoryTitle") {
					chest_title = transform.GetChild(0).gameObject;
					return false;
				}
				return true;
			}

			void create_navigation_button(int template_id, string name, Vector3 direction) {
				for (int index = 0; index < chest_title.transform.parent.childCount; index++) {
					if (chest_title.transform.parent.GetChild(index).name == name) {
						return;
					}
				}
				GameObject obj = GameObject.Instantiate<GameObject>(m_object_templates[template_id], chest_title.transform.parent);
				obj.name = name;
				obj.SetActive(true);
				RectTransform chest_title_rect = chest_title.GetComponent<RectTransform>();
				RectTransform obj_rect = obj.GetComponent<RectTransform>();
				obj_rect.localScale = new Vector3(1.5f, 1.5f, 1f);
				obj_rect.localPosition = chest_title_rect.localPosition + direction * ((chest_title_rect.rect.width / 2) + (obj_rect.rect.width / 2) + 5f);
				obj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener((UnityAction) delegate {
					OmniChest.Instance.navigate_from_chest(direction);
				});
			}

			this.m_chests.Add(chest);
			FieldInfo field_info = chest.GetType().GetField("ui", BindingFlags.Instance | BindingFlags.NonPublic);
			GameObject ui = null;
			if (field_info == null || (ui = (GameObject) field_info.GetValue(chest)) == null) {
				logger.LogWarning("** add_chest WARN - chest '" + chest.name + "' has no 'ui' field; unable to create navigation buttons.");
				return;
			}
			enum_descendants(ui.transform, __enum_descendants_callback_find_chest_title__);
			if (chest_title == null) {
				logger.LogWarning("** add_chest WARN - unable to locate chest title object for '" + chest.name + "'; cannot create navigation buttons.");
				return;
			}
			create_navigation_button(TEMPLATE_LEFT_ARROW_BUTTON, "CraftFromStorage_NavigateButtonLeft", Vector3.left);
			create_navigation_button(TEMPLATE_RIGHT_ARROW_BUTTON, "CraftFromStorage_NavigateButtonRight", Vector3.right);
		}

		private void navigate_from_chest(Vector3 direction) {
			this.get_thread_lock();
			try {
				for (int index = 0; index < this.m_chests.Count; index++) {
					if (this.m_chests[index].GetHashCode() != this.m_current_chest.GetHashCode()) {
						continue;
					}
					this.m_current_chest.EndInteract((int) InteractionType.Both);
					this.m_chests[
						(direction == Vector3.left ?
							(index == 0 ?
								this.m_chests.Count - 1 : 
								index - 1
							) :
							(index == this.m_chests.Count - 1 ?
								0 :
								index + 1
							)
						)
					].Interact((int) InteractionType.Both);
					break;
				}
			} catch (Exception e) {
				logger.LogError("** navigate_from_chest ERROR - " + e);
			}
			this.release_thread_lock();
		}

		private void add_inventory(Inventory inventory) {
			try {
				this.m_inventories.Add(inventory);
				foreach (SlotItemData item in inventory.Items) {
					if (!this.m_items.ContainsKey(item.id)) {
						this.m_items[item.id] = new List<SlotItemData>();
					}
					this.m_items[item.id].Add(item);
				}
			} catch (Exception) {
			}
		}

		public int get_fish_rarity_amount(ItemRarity rarity) {
            int amount = 0;
			foreach (int id in this.m_items.Keys) {
				foreach (SlotItemData item in this.m_items[id]) {
					if (item.item is FishItem fish_item && ItemDatabase.GetItemData(fish_item.ID()).rarity.Equals(rarity)) {
						amount += item.amount;
					}
				}
			}
			return amount;
        }

		public int get_item_amount(int id) {
			int amount = 0;
			switch (id) {
			case 60000:	return GameSave.Coins;
			case 60001:	return GameSave.Orbs;
			case 60002:	return GameSave.Tickets;
			case 60200:	return this.get_fish_rarity_amount(ItemRarity.Common);
			case 60201:	return this.get_fish_rarity_amount(ItemRarity.Uncommon);
			case 60202:	return this.get_fish_rarity_amount(ItemRarity.Rare);
			case 60203:	return this.get_fish_rarity_amount(ItemRarity.Epic);
			case 60204:	return this.get_fish_rarity_amount(ItemRarity.Legendary);
			}
			if (this.m_items.ContainsKey(id)) {
				foreach (SlotItemData item in this.m_items[id]) {
					amount += item.amount;
				}
			}	
			return amount;
		}

		public List<ItemAmount> remove_item(int id, int amount = 1, int slot = 0) {
			List<ItemAmount> items = new List<ItemAmount>();

			// note: this function assumes can_craft() was first called, so it
			// does not do any checks for item existence.
			foreach (SlotItemData data in this.m_items[id]) {
				foreach (ItemAmount item_amount in data.slot.inventory.RemoveItem(id, amount)) {
					amount -= item_amount.amount;
					items.Add(item_amount);
				}
				if (amount <= 0) {
					break;
				}
			}
			return items;
		}
	}

	[HarmonyPatch(typeof(Player), "FixedUpdate")]
	class HarmonyPatch_Player_FixedUpdate {

		private static bool Prefix(Player __instance) {
			if (__instance.IsOwner) {
				OmniChest.Instance.refresh();
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ScenePortalSpot), "OnTriggerEnter2D")]
	class HarmonyPatch_ScenePortalSpot_OnTriggerEnter2D {

		private static void Postfix() {
			OmniChest.Instance.refresh();
		}
	}

	[HarmonyPatch(typeof(Inventory), "TransferPlayerSimilarToOtherInventory")]
	class HarmonyPatch_Inventory_TransferPlayerSimilarToOtherInventory {

		private static bool Prefix(ref Inventory __instance, Inventory otherInventory) {
			if (m_enabled.Value && m_transfer_from_action_bar.Value) {
				__instance.TransferSimilarToOtherInventory(otherInventory);
				return false;
			}
			return true;
		}
	}
}