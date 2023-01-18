using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.cash_for_trash", "Cash for Trash", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.cash_for_trash");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.cash_for_trash v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(PlayerInventory), "Awake")]
	class HarmonyPatch_FarmSellingCrate_Awake {

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
			if (m_trash_button != null) {
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
		}
	}

	[HarmonyPatch(typeof(TrashSlot), "OnPointerDown")]
	class HarmonyPatch_TrashSlot_OnPointerDown {

		private static bool Prefix(ref TrashSlot __instance) {
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
				Player.Instance.AddMoneyAndRegisterSource(item.SellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
				Player.Instance.AddOrbsAndRegisterSource(item.OrbSellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
				Player.Instance.AddTicketsAndRegisterSource(item.TicketSellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
			}
			icon.RemoveItemIcon();
			__instance.inventory.UpdateInventory();
			return false;
		}
	}

}