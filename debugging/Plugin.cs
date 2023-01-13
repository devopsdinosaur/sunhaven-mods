
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
using DG.Tweening;


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

	public static bool e__result_ancestors(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			e__result_ancestors(child, callback);
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

		public static bool e__result_ancestors_callback(Transform transform) {
			TextMeshProUGUI tmp = transform.GetComponent<TextMeshProUGUI>();
			if (tmp != null && tmp.text.StartsWith("<sprite=\"gold_icon\"")) {
				return false;
			}
			m_income_tmp = transform;
			return true;
		}

		private static void Postfix(FarmSellingCrate __instance, GameObject ___ui,	Inventory ___sellingInventory) {
			Plugin.e__result_ancestors(___ui.transform, e__result_ancestors_callback);
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

		public static bool e__result_ancestors_callback(Transform transform) {
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
			Plugin.e__result_ancestors(____actionBarPanel.parent, e__result_ancestors_callback);
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
			Player.Instance.AddMoneyAndRegisterSource(item.SellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
			Player.Instance.AddOrbsAndRegisterSource(item.OrbSellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
			Player.Instance.AddTicketsAndRegisterSource(item.TicketSellPrice(icon.amount), item.ID(), icon.amount, MoneySource.ShippingPortal, playAudio: true);
			icon.RemoveItemIcon();
			__instance.inventory.UpdateInventory();
			return false;
		}
	}

	[HarmonyPatch(typeof(ItemDatabase), "ConstructDatabase", new[] {typeof(IList<ItemData>)})]
	class HarmonyPatch_ItemDatabase_ConstructDatabase {

		private static void Postfix() {
			foreach (int id in ItemDatabase.ids.Values) {
				ItemDatabase.items[id].stackSize = 9999;
			}
		}
	}

	[HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result) {
			if (stat != StatType.Movespeed) {
				return true;
			}
			__result = 0.5f;
			if (GameSave.Exploration.GetNode("Exploration2a")) {
				__result += 0.02f + 0.02f * (float) GameSave.Exploration.GetNodeAmount("Exploration2a");
			}
			if (Player.Instance.Mounted && GameSave.Exploration.GetNode("Exploration8a")) {
				__result += 0.04f * (float) GameSave.Exploration.GetNodeAmount("Exploration8a");
			}
			if (GameSave.Exploration.GetNode("Exploration5a") && SingletonBehaviour<TileManager>.Instance.GetTileInfo(Player.Instance.Position) != 0) {
				__result += 0.05f + 0.05f * (float) GameSave.Exploration.GetNodeAmount("Exploration5a");
			}
			if (GameSave.Exploration.GetNode("Exploration6a") && Time.time < Player.Instance.lastPickupTime + 3.5f) {
				__result += 0.1f * (float) GameSave.Exploration.GetNodeAmount("Exploration6a");
			}
			if (Time.time < Player.Instance.lastPickaxeTime + 2.5f) {
				__result += Player.Instance.MiningStats.GetStat(StatType.MovementSpeedAfterRock);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(HelpTooltips), "SendNotification")]
	class HarmonyPatch_HelpTooltips_SendNotification {

		private static bool Prefix(string title) {
			return title != "Exiting Mines";
		}
	}

	/*
	[HarmonyPatch(typeof(FishingRod), "UseDown1")]
	class HarmonyPatch_FishingRod_UseDown1 {

		private static bool Prefix(
			FishingRod __instance, 
			Player ___player, 
			bool ____canUseFishingRod,
			bool ____fishing,
			ref bool ___wonMiniGame,
			ref float ___hitSweetSpot,
			bool ____startCastingLine
		) {
			string[] METHOD_NAMES = new string[] {"CancelFishingAnimation", "AttemptToUseTool", "GetOffset", "StartCast", "Attack"};
			if (!___player.IsOwner || !____canUseFishingRod || __instance.Reeling) {
				return false;
			}
			Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
			foreach (string name in METHOD_NAMES) {
				methods[name] = __instance.GetType().GetTypeInfo().GetDeclaredMethod(name);
			}
			if (____fishing) {
				if (!__instance.ReadyForFish) {
					return false;
				}
				if ((bool) __instance.CurrentFish) {
					___wonMiniGame = true;
					___hitSweetSpot = 1f;
				} else {
					___wonMiniGame = false;
					___hitSweetSpot = 0f;
					methods["CancelFishingAnimation"].Invoke(__instance, new object[] {});
				}
			} else if ((bool) methods["AttemptToUseTool"].Invoke(__instance, new object[] {})) {
				Vector2 vector2 = (Vector2) methods["GetOffset"].Invoke(__instance, new object[] {});
				if (!____startCastingLine && (bool) methods["Attack"].Invoke(__instance, new object[] {vector2})) {
					methods["StartCast"].Invoke(__instance, new object[] {});
				}
			}
			return false;
		}
	}
	*/

	/*
	class ReallyHungryFish : Fish {

		public void TargetBobber(Collider2D collider, Bobber bobber) {
			if (GameManager.Multiplayer) {
				onTargetBobber?.Invoke();
			}
			float min = 1.25f;
			float max = 4.75f;
			float num = 1f;
			_pathMoveSpeed = 1.25f;
			if (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Fishing].GetNode("Fishing1b")) {
				float t = (8f - (collider.transform.position - bobber.transform.position).magnitude) / 8f;
				num = Mathf.Lerp(1f, 0.4f, t);
				_pathMoveSpeed = Mathf.Lerp(1.25f, 1.6f, t);
			}
			_targetBobber = bobber;
			bobber.FishingRod.TargetFish = this;
			_goToBobberTween = DOVirtual.DelayedCall(UnityEngine.Random.Range(min, max) / (float) bobber.FishingRod.fishAttractionRate * num, delegate
			{
				SetTarget(collider.transform.position, 1f);
			});
			onDestinationReached = (UnityAction) Delegate.Combine(onDestinationReached, (UnityAction) delegate
			{
				biteRoutine = StartCoroutine(BiteRoutine());
			});
		}

	}
	*/

	/*
	[HarmonyPatch(typeof(Fish), "BiteRoutine")]
	class HarmonyPatch_Fish_BiteRoutine {

		private static bool Prefix(
			Fish __instance, 
			Bobber ____targetBobber, 
			ref Tween ___fishRunAwayTween, 
			bool ___canBite,

		) {
			if (!base.isActiveAndEnabled) {
				yield break;
			}
			float seconds = UnityEngine.Random.Range(0.1f, 0.2f);
			yield return new WaitForSeconds(seconds);
			____targetBobber.Bite();
			____targetBobber.FishingRod.HasFish(__instance);
			___fishRunAwayTween = DOVirtual.DelayedCall(0.66f, delegate {
				if (!(____targetBobber != null) || !____targetBobber.FishingRod.Reeling) {
					__instance.Flee();
					___canBite = false;
					DOVirtual.DelayedCall(4f, delegate
					{
						___canBite = true;
					});
					if (____targetBobber != null) {
						if (____targetBobber.FishingRod.CurrentFish == __instance) {
							____targetBobber.FishingRod.CurrentFish = null;
						}
						if (____targetBobber.FishingRod.TargetFish == __instance) {
							____targetBobber.FishingRod.TargetFish = null;
						}
					}
					__instance.SetTarget(__instance.transform.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * 2.25f, 0f);
					____targetBobber?.FailMiniGame();
					____targetBobber = null;
				}
			});
			return false;
		}
	}
	*/

	[HarmonyPatch(typeof(Griffon), "Interact")]
	class HarmonyPatch_Griffon_Interact {

		private static bool Prefix(ref bool ___interacting, ref bool ___active, bool ___sunHaven, Animator ___animator) {
			if (DialogueController.Instance.DialogueOnGoing) {
				return false;
			}
			___interacting = true;
			DialogueController.Instance.CancelDialogue(animate: false);
			DialogueController.Instance.SetDefaultBox("Griffon");
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Griffon")) {
				if (___active) {
					if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("TalkedToMinesWilt")) {
						if (ScenePortalManager.ActiveSceneName.Equals("NelvariMinesEntrance")) {
							DialogueController.Instance.PushDialogue(new DialogueNode {
								dialogueText = new List<string> { " <i>(The griffon acknowledges you with a friendly gaze.)</i>" },
								responses = new Dictionary<int, Response> {{
									1,
									new Response {
										responseText = () => "Fly to Sun Haven Farm",
										action = delegate {
											DOVirtual.DelayedCall(1.25f, delegate
											{
												SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(295.5f, 143.7195f), "2playerfarm");
											});
										},
										enabled = true
									}
								}, {
								2,
								new Response {
									responseText = () => "Fly to Nel'Vari Farm",
									action = delegate {
										DOVirtual.DelayedCall(1.25f, delegate
										{
											SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(130.9583f, 102.4126f), "NelvariFarm");
										});
									},
									enabled = true
								}
							}, {
								3,
								new Response {
									responseText = () => "Never mind",
									action = null
								}
							}
							}});
							return false;
						}
						DialogueController.Instance.PushDialogue(new DialogueNode {
							dialogueText = new List<string> { " <i>(The griffon acknowledges you with a friendly gaze.)</i>" },
							responses = new Dictionary<int, Response>
							{{
								1,
								new Response {
									responseText = () => ___sunHaven ? "Fly to Nel'Vari Farm" : "Fly to Sun Haven Farm",
									action = delegate {
										DOVirtual.DelayedCall(1.25f, delegate {
											if (!___sunHaven) {
												SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(295.5f, 143.7195f), "2playerfarm");
											} else {
												SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(130.9583f, 102.4126f), "NelvariFarm");
											}
										});
									},
									enabled = true
								}
							}, {
								3,
								new Response {
									responseText = () => "Never mind",
									action = null
								}
							}}
						});
						return false;
					}
					if (ScenePortalManager.ActiveSceneName.Equals("NelvariMinesEntrance")) {
						DialogueController.Instance.PushDialogue(new DialogueNode {
							dialogueText = new List<string> { " <i>(The griffon acknowledges you with a friendly gaze.)</i>" },
							responses = new Dictionary<int, Response> {{
								1,
								new Response {
									responseText = () => "Fly to Sun Haven Farm",
									action = delegate {
										DOVirtual.DelayedCall(1.25f, delegate {
											SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(295.5f, 143.7195f), "2playerfarm");
										});
									},
									enabled = true
								}
							}, {
								2,
								new Response {
									responseText = () => "Fly to Nel'Vari Farm",
									action = delegate {
										DOVirtual.DelayedCall(1.25f, delegate {
											SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(130.9583f, 102.4126f), "NelvariFarm");
										});
									},
									enabled = true
								}
							}, {
								3,
								new Response {
									responseText = () => "Never mind",
									action = null
								}
							}}
						});
						return false;
					}
					DialogueController.Instance.PushDialogue(new DialogueNode {
						dialogueText = new List<string> { " <i>(The griffon acknowledges you with a friendly gaze.)</i>" },
						responses = new Dictionary<int, Response> {{
							1,
							new Response {
								responseText = () => ___sunHaven ? "Fly to Nel'Vari Farm" : "Fly to Sun Haven Farm",
								action = delegate {
									DOVirtual.DelayedCall(1.25f, delegate {
										if (!___sunHaven) {
											SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(295.5f, 143.7195f), "2playerfarm");
										} else {
											SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(130.9583f, 102.4126f), "NelvariFarm");
										}
									});
								},
								enabled = true
							}
						}, {
							2,
							new Response {
								responseText = () => "Fly to Nel'Vari Mines",
								action = delegate {
									DOVirtual.DelayedCall(1.25f, delegate {
										SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(154.1667f, 157.2463f), "NelvariMinesEntrance");
									});
								},
								enabled = true
							}
						}, {
							3,
							new Response {
								responseText = () => "Never mind",
								action = null
							}
						}
					}});
				} else {
					DialogueController.Instance.PushDialogue(new DialogueNode {
						dialogueText = new List<string> { "<i>(The griffon doesn't seem to even notice you, maybe it's best to leave it alone for right now.)</i>" },
						responses = new Dictionary<int, Response> {{
							2,
							new Response {
								responseText = () => "Pet the Griffon",
								action = delegate {
									___animator.SetTrigger("Pet");
								}
							}
						}, {
							3,
							new Response {
								responseText = () => "Never mind",
								action = null
							}
						}
					}});
				}
			} else {
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> { "<i>(The griffon doesn't seem to even notice you, maybe it's best to leave it alone for right now.)</i>" }
				});
			}
			return false;
		}
	}
}