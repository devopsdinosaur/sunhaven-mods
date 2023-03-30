using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.cash_for_trash", "Cash for Trash", "0.0.2")]
public class CashForTrashPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.cash_for_trash");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	public static ConfigEntry<float> m_cash_multiplier;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_cash_multiplier = this.Config.Bind<float>("General", "Sale Cash Multiplier", 1f, "Multiplier for sales multiplied times the value of the item trashed (float, 1f [default] == 100% value, 0.5f == 50% value, and 2.0f == 200% value, and so on)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.cash_for_trash v0.0.2 " + (m_enabled.Value ? "" : "[inactive; disabled in config]") + " loaded.");
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
				if (!m_enabled.Value || m_trash_button != null) {
					return;
				}
				enum_descendants(____actionBarPanel.parent, enum_descendants_callback);
				foreach (Component component in m_trash_button.GetComponents<Component>()) {
					if (component is Popup) {
						Popup popup = (Popup) component;
						if (popup.text != "") {
							popup.text = "Sell Item";
							popup.description = "Drop an item here to sell it for full price!";
						}
					} else if (component is UIButton) {
						UIButton button = (UIButton) component;
						foreach (int id in ItemDatabase.ids.Values) {
							if (ItemDatabase.items[id].name == "Small Money Bag") {
								button.defaultImage = button.hoverOverImage = button.pressedImage = ItemDatabase.items[id].icon;
								break;
							}
						}
					}
				}
			} catch (Exception e) {
				logger.LogError("** PlayerInventory.Awak_Postfix ERROR -" + e);
			}
		}
	}

	[HarmonyPatch(typeof(TrashSlot), "OnPointerDown")]
	class HarmonyPatch_TrashSlot_OnPointerDown {

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
				if (data.category == ItemCategory.Quest || !data.canTrash) {
					return false;
				}
				Item item = data.GetItem();
				if (data.canSell) {
					Player.Instance.AddMoneyAndRegisterSource((int) (item.SellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
					Player.Instance.AddOrbsAndRegisterSource((int) (item.OrbSellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
					Player.Instance.AddTicketsAndRegisterSource((int) (item.TicketSellPrice(icon.amount) * m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
				}
				icon.RemoveItemIcon();
				__instance.inventory.UpdateInventory();
				return false;
			} catch (Exception e) {
				logger.LogError("** TrashSlot.OnPointerDown_Prefix ERROR -" + e);
			}
			return true;
		}
	}

}