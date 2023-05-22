using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.cash_for_trash", "Cash for Trash", "0.0.3")]
public class CashForTrashPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.cash_for_trash");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	public static ConfigEntry<float> m_cash_multiplier;
	const int TRASH_SLOT_TRASH = 0;
	const int TRACK_SLOT_RECYCLE = 1;
	public static Dictionary<int, int> m_trash_slots = new Dictionary<int, int>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_cash_multiplier = this.Config.Bind<float>("General", "Sale Cash Multiplier", 1f, "Multiplier for sales multiplied times the value of the item trashed (float, 1f [default] == 100% value, 0.5f == 50% value, and 2.0f == 200% value, and so on)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.cash_for_trash v0.0.3 " + (m_enabled.Value ? "" : "[inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}		
	}

	[HarmonyPatch(typeof(PlayerInventory), "Awake")]
	class HarmonyPatch_PlayerInventory_Awake {
		
		private static Transform m_trash_button = null;

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

		public static bool enum_descendants_callback(Transform transform) {
			if (transform.name == "TrashButton") {
				m_trash_button = transform;
				return false;
			}
			return true;
		}

		private static void Postfix(ref PlayerInventory __instance, Transform ____actionBarPanel) {
			try {

				void update_button(Transform transform, string text, string description, int image_id, int trash_slot_id) {
					foreach (Component component in transform.GetComponents<Component>()) {
						if (component is Popup) {
							Popup popup = (Popup) component;
							if (popup.text != "") {
								popup.text = text;
								popup.description = description;
							}
						} else if (component is UIButton) {
							UIButton button = (UIButton) component;
							button.defaultImage = button.hoverOverImage = button.pressedImage = ItemDatabase.items[image_id].icon;
						} else if (component is TrashSlot) {
							m_trash_slots[component.GetHashCode()] = trash_slot_id;
						}
					}
				}

				if (!m_enabled.Value || m_trash_button != null) {
					return;
				}
				enum_descendants(____actionBarPanel.parent, enum_descendants_callback);
				update_button(m_trash_button, "Sell Item", "Drop an item here to sell it for full price!", ItemID.SmallMoneyBag, TRASH_SLOT_TRASH);
				Transform recycle_button = GameObject.Instantiate(m_trash_button, m_trash_button.parent);
				update_button(recycle_button, "Recycle Item", "Drop a crafted item here to recyle it into its original ingredients.  Note that if there are multiple recipes for the item then only the first in memory will be used (for example: Fire Crystal yields Soot and not coal).", ItemID.SpringToken, TRACK_SLOT_RECYCLE);
				recycle_button.localPosition = m_trash_button.localPosition + (Vector3.left * recycle_button.GetComponent<RectTransform>().rect.width * 3);
			} catch (Exception e) {
				logger.LogError("** PlayerInventory.Awake_Postfix ERROR -" + e);
			}
		}
	}

	[HarmonyPatch(typeof(TrashSlot), "OnPointerDown")]
	class HarmonyPatch_TrashSlot_OnPointerDown {

		static List<int> BAD_ITEM_IDS = new List<int>(new int[] {
			ItemID.Health,
			ItemID.Mana,
			ItemID.Coins,
			ItemID.ManaOrbs,
			ItemID.Tickets,
			ItemID.CommonFish,
			ItemID.UncommonFish,
			ItemID.RareFish,
			ItemID.EpicFish,
			ItemID.LegendaryFish,
		});

		private static bool Prefix(ref TrashSlot __instance) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				ItemIcon icon = Inventory.CurrentItemIcon;
				if (icon == null) {
					return false;
				}
				ItemData data = ItemDatabase.GetItemData(icon.item);
				if (data.category == ItemCategory.Quest || !data.canTrash || !m_trash_slots.ContainsKey(__instance.GetHashCode())) {
					return false;
				}
				Item item = data.GetItem();
				switch (m_trash_slots[__instance.GetHashCode()]) {
				case TRASH_SLOT_TRASH:
					if (data.canSell) {
						Player.Instance.AddMoneyAndRegisterSource((int) (item.SellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
						Player.Instance.AddOrbsAndRegisterSource((int) (item.OrbSellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
						Player.Instance.AddTicketsAndRegisterSource((int) (item.TicketSellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
					}
					icon.RemoveItemIcon();
					__instance.inventory.UpdateInventory();
					break;
				case TRACK_SLOT_RECYCLE:
					foreach (Recipe recipe in Resources.FindObjectsOfTypeAll<Recipe>()) {
						if (recipe.output != null && recipe.output.item != null && recipe.output.item.id == item.ID()) {
							int item_count = 0;
							foreach (ItemInfo item_info in recipe.input) {
								if (!BAD_ITEM_IDS.Contains(item_info.item.id)) {
									Player.Instance.Inventory.AddItem(
										item_info.item.GenerateItem(), 
										recipe.ModifiedAmount(item_info.amount, item_info.item, recipe.output.item) * icon.amount, 
										true
									);
									item_count++;
								}
							}
							if (item_count > 0) {
								icon.RemoveItemIcon();
								__instance.inventory.UpdateInventory();
								break;
							}
						}
					}
					break;
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** TrashSlot.OnPointerDown_Prefix ERROR -" + e);
			}
			return true;
		}
	}

}