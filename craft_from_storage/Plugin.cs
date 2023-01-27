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


[BepInPlugin("devopsdinosaur.sunhaven.craft_from_storage", "Craft From Storage", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.craft_from_storage");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<string> m_chest_interact_strings;
	private static ConfigEntry<bool> m_use_inventory_first;
	private static ConfigEntry<bool> m_transfer_from_action_bar;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.craft_from_storage v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_chest_interact_strings = this.Config.Bind<string>("General", "Chest Interact Strings", "Chest,Fridge,Wardrobe", "[Advanced] Comma-separated list of strings matching the *exact* text displayed when hovering over the storage container.  For a container to be included in the global access its interact text must be in this list.  Messing up this value *will* break the mod =)  If you have to add a string please PM me on nexus, and I will add it to the mod defaults.");
		m_use_inventory_first = this.Config.Bind<bool>("General", "Use Inventory First", true, "If true then crafting stations will pull from inventory before storage chests.");
		m_transfer_from_action_bar = this.Config.Bind<bool>("General", "Transfer From Action Bar", false, "If true then the transfer similar/same buttons will also pull from the action bar.");
	}

	public class OmniChest {

		private static OmniChest m_instance = null;
		private List<int> m_added_hashes = null;
		private Dictionary<int, List<SlotItemData>> m_items = null;
		private List<Inventory> m_inventories = null;
		private const float CHECK_FREQUENCY = 1.0f;
		private float m_elapsed = 0f;
		private List<string> m_chest_interact_strings = null;
		private GameObject m_transfer_similar_button = null;

		public static OmniChest Instance {
			get {
				return (m_instance != null ? m_instance : m_instance = new OmniChest());
			}
		}

		private OmniChest() {
			this.m_chest_interact_strings = new List<string>();
			foreach (string value in Plugin.m_chest_interact_strings.Value.Split(',')) {
				this.m_chest_interact_strings.Add(value);
			}
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

		private void create_transfer_button(Transform chest_transform, Inventory player_inventory) {
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
				if (transform.name == "SortButton") {
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
				this.transfer_similar_items(player_inventory);
			});
		}

		public void transfer_similar_items(Inventory player_inventory) {
			foreach (Inventory inventory in this.m_inventories) {
				if (inventory == player_inventory) {
					continue;
				}
				player_inventory.TransferPlayerSimilarToOtherInventory(inventory);
			}
		}

		public void refresh(Inventory player_inventory) {
			if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return;
			}
			m_elapsed = 0f;
			int hash = 0;
			this.m_added_hashes = new List<int>();
			this.m_items = new Dictionary<int, List<SlotItemData>>();
			this.m_inventories = new List<Inventory>();
			if (m_use_inventory_first.Value) {
				this.add_inventory(player_inventory);
			}
			foreach (KeyValuePair<Vector3Int, Decoration> kvp in GameManager.Instance.objects) {
				if (kvp.Value is Chest) {
					if (!this.m_added_hashes.Contains(hash = kvp.Value.GetHashCode()) &&
						this.m_chest_interact_strings.Contains((string) kvp.Value.GetType().GetTypeInfo().GetField("interactText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(kvp.Value))
					) {
						if (this.m_transfer_similar_button == null) {
							this.create_transfer_button(kvp.Value.transform, player_inventory);
						}
						this.m_added_hashes.Add(hash);
						this.add_inventory((Inventory) kvp.Value.GetType().GetTypeInfo().GetField("sellingInventory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(kvp.Value));
					}
				}
			}
			if (!m_use_inventory_first.Value) {
				this.add_inventory(player_inventory);
			}
			Popup popup = this.m_transfer_similar_button.GetComponent<Popup>();
			popup.text = "Zone Send Similar";
			popup.description = "Send similar items to nearby chests within the current zone (note: house and outside are different zones).\nNearby chests: " + Math.Max(this.m_inventories.Count - 1, 0);
		}

		private void add_inventory(Inventory inventory) {
			this.m_inventories.Add(inventory);
			foreach (SlotItemData item in inventory.Items) {
				if (!this.m_items.ContainsKey(item.id)) {
					this.m_items[item.id] = new List<SlotItemData>();
				}
				this.m_items[item.id].Add(item);
			}
		}

		public int get_item_amount(int id) {
			int amount = 0;
			if (this.m_items.ContainsKey(id)) {
				foreach (SlotItemData item in this.m_items[id]) {
					amount += item.amount;
				}
			}	
			return amount;
		}

		public bool can_craft(Recipe recipe, int amount) {
			foreach (ItemInfo item in recipe.Input) {
				if (item.item.name == "Mana") {
					if (Player.Instance.Mana <= (float) recipe.ModifiedAmount(item.amount, item.item, recipe.output.item) * amount) {
						return false;
					}
				} else if (!this.m_items.ContainsKey(item.item.id)) {
					return false;
				} else {
					int counter = 0;
					foreach (SlotItemData data in this.m_items[item.item.id]) {
						if ((counter += data.amount) >= amount) {
							break;
						}
					}
					if (counter < amount) {
						return false;
					}
				}
			}
			return true;
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

		private static bool Prefix(ref PlayerInventory ____inventory) {
			OmniChest.Instance.refresh(____inventory);
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "Interact")]
	class HarmonyPatch_CraftingTable_Interact {

		private static void Postfix() {
			
		}
	}

	[HarmonyPatch(typeof(CraftingPanel), "InitializeInput")]
	class HarmonyPatch_CraftingPanel_InitializeInput {

		private static bool Prefix(Recipe recipe, ItemImage itemImage, int index) {
			if (!m_enabled.Value) {
				return true;
			}
			itemImage.Initialize(ItemDatabase.GetItemData(recipe.Input[index].item.id).GetItem());
			int num = 0;
			num = ((!(recipe.Input[index].item.name == "Mana")) ? 
				OmniChest.Instance.get_item_amount(recipe.Input[index].item.id) : 
				((int) Player.Instance.Mana)
			);
			int num2 = recipe.ModifiedAmount(recipe.Input[index].amount, recipe.Input[index].item, recipe.output.item);
			itemImage.SetDisabled(num < num2);
			itemImage.SetAmount(num.FormatWithCommas() + "/" + num2.FormatWithCommas(), num2);
			return false;
		}
	}

	[HarmonyPatch(typeof(CraftingPanel), "UpdateCraftingButtons")]
	class HarmonyPatch_CraftingPanel_UpdateCraftingButtons {

		private static bool Prefix(
			ref Recipe ___recipe,
			ref UnityEngine.UI.Button ___buyButton,
			ref TextMeshProUGUI ___buyButtonTMP,
			ref UnityEngine.UI.Button ___buy5Button,
			ref TextMeshProUGUI ___buy5ButtonTMP,
			ref UnityEngine.UI.Button ___buy20Button,
			ref TextMeshProUGUI ___buy20ButtonTMP
		) {
			if (!m_enabled.Value) {
				return true;
			}
			int num = 999;
			foreach (ItemInfo item in ___recipe.Input) {
				int num2 = ((!(item.item.name == "Mana")) ? 
					(OmniChest.Instance.get_item_amount(item.item.id) / ___recipe.ModifiedAmount(item.amount, item.item, ___recipe.output.item)) : 
					((int) Player.Instance.Mana / ___recipe.ModifiedAmount(item.amount, item.item, ___recipe.output.item))
				);
				if (num2 < num) {
					num = num2;
				}
			}
			Color color = new Color(1f, 1f, 1f, 28f / 51f);
			if (num < 1) {
				___buyButton.interactable = false;
				___buyButtonTMP.color = color;
				___buyButtonTMP.text = "x 1";
			} else {
				___buyButton.interactable = true;
				___buyButtonTMP.color = Color.white;
				___buyButtonTMP.text = "x <color=#FEE463>1";
			}
			if (num < 5) {
				___buy5Button.interactable = false;
				___buy5ButtonTMP.color = color;
				___buy5ButtonTMP.text = "x 5";
			} else {
				___buy5Button.interactable = true;
				___buy5ButtonTMP.color = Color.white;
				___buy5ButtonTMP.text = "x <color=#FEE463>5";
			}
			if (num < 20) {
				___buy20Button.interactable = false;
				___buy20ButtonTMP.color = color;
				___buy20ButtonTMP.text = "x 20";
			} else {
				___buy20Button.interactable = true;
				___buy20ButtonTMP.color = Color.white;
				___buy20ButtonTMP.text = "x <color=#FEE463>20";
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "Craft")]
	class HarmonyPatch_CraftingTable_Craft {

		protected static float TimeFromDate(DateTime date) {
			return (float) (date.Minute + date.Hour * 60 + date.Day * 24 * 60) + (float) date.Second / 60f + (float) (date.Year * 28 * 24 * 60);
		}

		private static bool Prefix(
			ref CraftingTable __instance,
			Recipe recipe,
			int amount,
			CraftingTableData ___craftingData,
			ref float ___queueTime,
			float ___craftSpeedMultiplier
		) {
			if (!m_enabled.Value) {
				return true;
			}
			if (!OmniChest.Instance.can_craft(recipe, amount)) {
				return false;
			}
			if (___craftingData.items.Count <= 0) {
				___queueTime = 0f;
			}
			if (___queueTime <= 0f) {
				___craftingData.timeStart = TimeFromDate(SingletonBehaviour<DayCycle>.Instance.Time);
			}
			for (int i = 0; i < amount; i++) {
				List<ItemAmount> list = new List<ItemAmount>();
				foreach (ItemInfo item2 in recipe.Input) {
					if (item2.item.name == "Mana") {
						int num = recipe.ModifiedAmount(item2.amount, item2.item, recipe.output.item);
						Player.Instance.UseMana(num);
					} else {
						List<ItemAmount> collection = OmniChest.Instance.remove_item(item2.item.id, recipe.ModifiedAmount(item2.amount, item2.item, recipe.output.item));
						list.AddRange(collection);
					}
				}
				Item item = recipe.output.item.GenerateItem(list);
				float multiplier = ___craftSpeedMultiplier * ((GameSave.CurrentCharacter.race == (int) Race.Human) ? 1.2f : 1f);
				float craftTime = recipe.GetHoursToCraft(multiplier) * Settings.DaySpeedMultiplier;
				ItemCraftInfo itemCraftInfo = new ItemCraftInfo {
					item = item,
					craftTime = craftTime,
					amount = recipe.output.amount,
					input = list
				};
				___craftingData.items.Add(itemCraftInfo);
				___queueTime += itemCraftInfo.craftTime;
			}
			recipe.Craft();
			__instance.GetType().GetTypeInfo().GetMethod("SetupCraftingQueue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
			if (recipe is SkillTomeRecipe) {
				__instance.Initialize();
			} else {
				__instance.Refresh();
			}
			__instance.SaveMeta();
			__instance.SendNewMeta(__instance.meta);
			return false;
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