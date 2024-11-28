using BepInEx;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using System.Collections.Generic;
using PSS;
using System.Reflection;

public static class PluginInfo {

	public const string TITLE = "Cash for Trash";
	public const string NAME = "cash_for_trash";
	public const string SHORT_DESCRIPTION = "Slow down, speed up, stop, and even reverse time using configurable hotkeys.";

	public const string VERSION = "0.0.5";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "Luma Island";
	public const string GAME = "luma-island";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "luma-island-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class CashForTrashPlugin : DDPlugin {
	const int TRASH_SLOT_TRASH = 0;
	const int TRACK_SLOT_RECYCLE = 1;

	private Harmony m_harmony = new Harmony(PluginInfo.GUID);
	public static Dictionary<int, int> m_trash_slots = new Dictionary<int, int>();

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
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
						if (component is Popup popup) {
							if (popup.text != "") {
								popup.text = text;
								popup.description = description;
							}
						} else if (component is UIButton wish_button) {
							Database.GetData(image_id, delegate(ItemData data) {
								try {
									wish_button.defaultImage = wish_button.hoverOverImage = wish_button.pressedImage = data.icon;
									wish_button.gameObject.GetComponent<UnityEngine.UI.Image>().sprite = data.icon;
								} catch (Exception e) {
									logger.LogError("** Database.GetData callback ERROR - " + e);
								}
							});
						} else if (component is TrashSlot) {
							m_trash_slots[component.GetHashCode()] = trash_slot_id;
						}
					}
				}

				if (!Settings.m_enabled.Value || m_trash_button != null) {
					return;
				}
				enum_descendants(____actionBarPanel.parent.parent, enum_descendants_callback);
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
				if (!Settings.m_enabled.Value) {
					return true;
				}
				ItemIcon icon = Inventory.CurrentItemIcon;
				if (icon == null) {
					return false;
				}
				ItemData data = null;
				Database.GetData(icon.item.ID(), delegate (ItemData _data) {
					data = _data;
				});
				if (data.category == ItemCategory.Quest || !data.canTrash || !m_trash_slots.ContainsKey(__instance.GetHashCode())) {
					return false;
				}
				Item item = data.GetItem();
				switch (m_trash_slots[__instance.GetHashCode()]) {
				case TRASH_SLOT_TRASH:
					if (data.canSell) {
						Player.Instance.AddMoneyAndRegisterSource((int) (item.SellPrice(icon.amount) * Settings.m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true, showNotification: true);
						Player.Instance.AddOrbsAndRegisterSource((int) (item.OrbSellPrice(icon.amount) * Settings.m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true, showNotification: true);
						Player.Instance.AddTicketsAndRegisterSource((int) (item.TicketSellPrice(icon.amount) * Settings.m_cash_multiplier.Value), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true, showNotification: true);
					}
					icon.RemoveItemIcon();
					__instance.inventory.UpdateInventory();
					break;
				case TRACK_SLOT_RECYCLE:
					foreach (Recipe recipe in Resources.FindObjectsOfTypeAll<Recipe>()) {
						if (recipe.output2?.id == item.ID()) {
							int item_count = 0;
							foreach (SerializedItemDataNamedAmount item2 in recipe.Input) {
								if (!BAD_ITEM_IDS.Contains(item2.id)) {
									Database.GetData(item2.id, delegate(ItemData item_data) {
										try {
											Player.Instance.Inventory.AddItem(
												item_data.GenerateItem(),
												recipe.ModifiedAmount(item2.amount, item_data.ID, recipe.output2.id, recipe.isFood) * icon.amount, 
												true
											);
											item_count++;
										} catch (Exception e) {
											logger.LogError("** TrashSlot.OnPointerDown_Prefix_Recycle ERROR - " + e);
										}
									});
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
				logger.LogError("** TrashSlot.OnPointerDown_Prefix ERROR - " + e);
			}
			return true;
		}
	}

}