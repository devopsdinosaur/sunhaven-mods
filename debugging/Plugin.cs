
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using TMPro;
using System.IO;
using UnityEngine.Events;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	public static bool list_ancestors(Transform parent, Func<Transform, bool> callback, int indent) {
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
			list_ancestors(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_ancestors(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_ancestors(child, callback);
		}
		return true;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_MainMenuController_PlayGame {

		private static bool Prefix() {
			GameSave.Instance.SetProgressBoolCharacter("BabyDragon", value: true);
			GameSave.Instance.SetProgressBoolCharacter("BabyTiger", value: true);
			GameSave.Instance.SetProgressBoolCharacter("WithergateMask1", value: true);
			GameSave.Instance.SetProgressBoolCharacter("SunArmor", value: true);
			GameSave.Instance.SetProgressBoolCharacter("GoldRecord", value: true);
			return true;
		}
	}

	/*
	[HarmonyPatch(typeof(FarmSellingCrate), "Awake")]
	class HarmonyPatch_FarmSellingCrate_Awake {

		private static Transform m_income_tmp = null;

		public static bool enum_ancestors_callback(Transform transform) {
			TextMeshProUGUI tmp = transform.GetComponent<TextMeshProUGUI>();
			if (tmp != null && tmp.text.StartsWith("<sprite=\"gold_icon\"")) {
				return false;
			}
			m_income_tmp = transform;
			return true;
		}

		private static void Postfix(FarmSellingCrate __instance, GameObject ___ui,	Inventory ___sellingInventory) {
			Plugin.enum_ancestors(___ui.transform, enum_ancestors_callback);
			if (m_income_tmp == null) {
				return;
			}
			m_income_tmp.GetComponent<TextMeshProUGUI>().text = "Income [Click to Sell Now]";
			RectTransform rect_transform = m_income_tmp.GetComponent<RectTransform>();
			rect_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect_transform.rect.width * 4);
			rect_transform.position += Vector3.up * rect_transform.rect.height / 3;
			rect_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect_transform.rect.height * 2);
			m_income_tmp.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener((UnityAction) delegate {
				foreach (SlotItemData data in ___sellingInventory.Items) {
					logger.LogInfo(data.item.SellPrice(data.amount));
					logger.LogInfo(data.item.OrbSellPrice(data.amount));
					logger.LogInfo(data.item.TicketSellPrice(data.amount));
					Player.Instance.AddMoneyAndRegisterSource(data.item.SellPrice(data.amount), data.item.ID(), data.amount, MoneySource.ShippingPortal, playAudio: false);
					Player.Instance.AddOrbsAndRegisterSource(data.item.OrbSellPrice(data.amount), data.item.ID(), data.amount, MoneySource.ShippingPortal, playAudio: false);
					Player.Instance.AddTicketsAndRegisterSource(data.item.TicketSellPrice(data.amount), data.item.ID(), data.amount, MoneySource.ShippingPortal, playAudio: false);
				}
				___sellingInventory.ClearInventory();
				__instance.EndInteract(0);
			});
		}
	}
	*/

	[HarmonyPatch(typeof(PlayerInventory), "Awake")]
	class HarmonyPatch_FarmSellingCrate_Awake {

		private static Transform m_trash_button = null;

		public static bool enum_ancestors_callback(Transform transform) {
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
			Plugin.enum_ancestors(____actionBarPanel.parent, enum_ancestors_callback);
			//Plugin.list_component_types(m_trash_button);
			/*[Info: DEBUGGING] UnityEngine.RectTransform
			[Info: DEBUGGING] Wish.TrashSlot
			[Info: DEBUGGING] UnityEngine.CanvasRenderer
			[Info: DEBUGGING] UnityEngine.UI.Image
			[Info: DEBUGGING] Wish.NavigationElement
			[Info: DEBUGGING] Wish.UIButton
			[Info: DEBUGGING] Wish.Popup
			[Info: DEBUGGING] Wish.Popup*/
			foreach (Component component in m_trash_button.GetComponents<Component>()) {
				if (component is Popup) {
					Popup popup = (Popup) component;
					if (popup.text != "") {
						popup.text = "Sell Item";
						popup.description = "Drop an item here to sell it for full price!";
					}
				}
			}
		}
	}
}