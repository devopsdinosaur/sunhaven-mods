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
	private static ConfigEntry<bool> m_use_inventory_first;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.craft_from_storage v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_use_inventory_first = this.Config.Bind<bool>("General", "Use Inventory First", true, "If true then crafting stations will pull from inventory before storage chests.");
	}

	public class OmniChest {

		private static OmniChest m_instance = null;
		private List<int> m_added_hashes = null;
		private Dictionary<int, List<SlotItemData>> m_items = null;
		private const float CHECK_FREQUENCY = 1.0f;
		private float m_elapsed = 0f;
		private string m_chest_interact_text;

		public static OmniChest Instance {
			get {
				return (m_instance != null ? m_instance : m_instance = new OmniChest());
			}
		}

		private OmniChest() {
			// Need to check Chest.interactText to make sure it's not one of the other random
			// world "chests" (i.e. snaccoons).  It's hacky, but there seems to be no other
			// distinction.  This is a string and not a const because I assume the game will
			// eventually have localization and I'll just localize it here.
			this.m_chest_interact_text = "Chest";
		}

		public void refresh(Inventory player_inventory) {
			if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return;
			}
			m_elapsed = 0f;
			int hash = 0;
			this.m_added_hashes = new List<int>();
			this.m_items = new Dictionary<int, List<SlotItemData>>();
			if (m_use_inventory_first.Value) {
				this.add_inventory(player_inventory);
			}
			foreach (KeyValuePair<Vector3Int, Decoration> kvp in GameManager.Instance.objects) {
				if (kvp.Value is Chest) {
					if (!this.m_added_hashes.Contains(hash = kvp.Value.GetHashCode()) &&
						(string) kvp.Value.GetType().GetTypeInfo().GetField("interactText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(kvp.Value) == this.m_chest_interact_text
					) {
						this.m_added_hashes.Add(hash);
						this.add_inventory((Inventory) kvp.Value.GetType().GetTypeInfo().GetField("sellingInventory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(kvp.Value));
					}
				}
			}
			if (!m_use_inventory_first.Value) {
				this.add_inventory(player_inventory);
			}
		}

		private void add_inventory(Inventory inventory) {
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

		}

		public List<ItemAmount> remove_item(int id, int amount = 1, int slot = 0) {

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
			ref Recipe recipe,
			ref int amount,
			ref CraftingTableData ___craftingData,
			ref float ___queueTime
		) {
			if (!m_enabled.Value) {
				return true;
			}
			if (OmniChest.Instance.can_craft(recipe, amount)) {
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
				float craftTime = recipe.GetHoursToCraft(CraftSpeedMultiplier) * Settings.DaySpeedMultiplier;
				ItemCraftInfo itemCraftInfo = new ItemCraftInfo {
					item = item,
					craftTime = craftTime,
					amount = recipe.output.amount,
					input = list
				};
				craftingData.items.Add(itemCraftInfo);
				queueTime += itemCraftInfo.craftTime;
			}
			recipe.Craft();
			SetupCraftingQueue();
			if (recipe is SkillTomeRecipe) {
				Initialize();
			} else {
				Refresh();
			}
			SaveMeta();
			SendNewMeta(base.meta);
			return false;
		}
	}
}