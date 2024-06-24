using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;
using System.Reflection;
using PSS;
using UnityEngine;

[BepInPlugin("devopsdinosaur.sunhaven.inventory_first", "Inventory First", "0.0.1")]
public class InventoryFirstPlugin : BaseUnityPlugin {

	private const int ACTION_BAR_INDEX_START = 0;
	private const int ACTION_BAR_INDEX_STOP = 9;

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.inventory_first");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.inventory_first v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Inventory), "AddItem", new Type[] { typeof(Item), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) })]
	class HarmonyPatch_Inventory_AddItem {

		private static bool Prefix(
			Inventory __instance,
			Item item,
			int amount,
			int slot,
			bool sendNotification,
			bool specialItem,
			bool superSecretCheck,
			Dictionary<int, int> ___currentAmounts
		) {
			try {
				if (!m_enabled.Value || __instance.GetHashCode() != Player.Instance.Inventory.GetHashCode() || !(__instance is PlayerInventory inventory) || slot != 0) {
					return true;
				}
				ItemData itemData = null;
				Database.GetData(item.ID(), delegate (ItemData _itemData) {
					itemData = _itemData;
				});
				if (itemData == null || (specialItem && (bool) typeof(Inventory).GetMethod("AddSpecialItem", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { amount, sendNotification, itemData })) || (superSecretCheck && !(bool) typeof(Inventory).GetMethod("SuperSecretMethodIfYouRemoveThisWeWillSue", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { itemData.id }))) {
					return false;
				}
				SingletonBehaviour<GameSave>.Instance.SaveEncylopediaItem(item.ID(), DayCycle.Day);

				bool slot_has_space(int index, int required_id) {
					return (inventory.Items[index].item?.ID() == required_id && inventory.Items[index].amount < (inventory.Items[index].slot.onlyAcceptSpecificItem ? inventory.Items[index].slot.numberOfItemToAccept : itemData.stackSize) && inventory.Items[index].slot.ValidateItem(item.ID()));
				}

				int find_slot() {
					int check_index;
					int max_index = Mathf.Min(inventory.maxSlots, inventory.Items.Count);
					for (check_index = 0; check_index < max_index; check_index++) {
						if (slot_has_space(check_index, item.ID())) {
							return check_index;
						}
					}
					check_index = ACTION_BAR_INDEX_STOP + 1;
					for (; ; ) {
						if (slot_has_space(check_index, 0)) {
							return check_index;
						}
						if (++check_index >= max_index) {
							check_index = ACTION_BAR_INDEX_START;
						} else if (check_index == ACTION_BAR_INDEX_STOP + 1) {
							break;
						}
					}
					return -1;
				}

				void add_item(ref int item_count) {
					if (item_count <= 0) {
						return;
					}
					int slot_index;
					if ((slot_index = find_slot()) == -1) {
						Pickup.Spawn(Player.Instance.transform.position, item, item_count, homeIn: false, 0.4f, Pickup.BounceAnimation.Normal, 2f, 100f);
						item_count = 0;
						return;
					}

					SlotItemData slot_data = inventory.Items[slot_index];
					int can_accept = Mathf.Min((slot_data.slot.onlyAcceptSpecificItem ? slot_data.slot.numberOfItemToAccept : itemData.stackSize) - slot_data.amount, item_count);
					if (slot_data.item.ID() == 0) {
						slot_data.item = item.DeepClone();
					}
					slot_data.id = item.ID();
					slot_data.amount += can_accept;
					if (sendNotification) {
						SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, can_accept);
					}
					ItemIcon icon = slot_data.slot.GetComponentInChildren<ItemIcon>();
					if (icon == null) {
						icon = UnityEngine.Object.Instantiate(SingletonBehaviour<Prefabs>.Instance.ItemIcon, slot_data.slot.transform);
						slot_data.slot.ModifyItemQuality(slot_data.item);
						icon.Initialize(inventory.Items[slot_index]);
					} else {
						icon.UpdateAmount(slot_data.amount);
					}
					___currentAmounts.Remove(itemData.id);
					__instance.UpdateInventory();
					__instance.OnAddedItem?.Invoke(slot_data.id);
					if ((item_count -= can_accept) > 0) {
						add_item(ref item_count);
					}
				}

				add_item(ref amount);
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Inventory_AddItem.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}