using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using DG.Tweening;

[BepInPlugin("devopsdinosaur.sunhaven.free_rides", "Free Rides", "0.0.3")]
public class FreeRidesPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.free_rides");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.free_rides v0.0.3" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(SunHavenTaxi), "Interact")]
	class HarmonyPatch_SunHavenTaxi_Interact {

		private static bool Prefix(
			SunHavenTaxi __instance,
			ref bool ___interacting,
			ref Animator ___fixedAnim,
			ref Animator ___brokenAnim,
			ref CliveBrokenTaxi1Cutscene ___brokenCutscene1,
			ref CliveBrokenTaxi1Cutscene ___brokenCutscene2,
			ref CliveBrokenTaxi1Cutscene ___brokenCutscene3,
			ref string ___dialogueName,
			ref string ___inspectionText,
			ref bool ___notEnoughMoney
		) {
			if (___interacting) {
				return false;
			}
			___fixedAnim.Play("SHTaxi_IdleSouth");
			___brokenAnim.Play("SHTaxi_IdleSouth");
			if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("WildernessTaxi")) {
				if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("CliveBrokenTaxi1Cutscene")) {
					___brokenCutscene1.Begin();
					return false;
				}
				if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("CliveBrokenTaxi2Cutscene") && Player.Instance.QuestList.HasQuest("WheelinNDealinQuest")) {
					___brokenCutscene2.Begin();
					return false;
				}
				if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("CliveBrokenTaxi3Cutscene") && Player.Instance.QuestList.HasQuest("WheelRepairQuest") && Player.Instance.Inventory.HasEnough(1201, 1)) {
					___brokenCutscene3.Begin();
					return false;
				}
				DialogueController.Instance.SetDefaultBox(___dialogueName);
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> { "Cart still broke. Ain't going nowhere." },
					responses = new Dictionary<int, Response> {{
						1,
						new Response
						{
							responseText = () => "See ya!",
							action = delegate {
								__instance.EndInteract(0);
							}
						}
					}}
				});
				return false;
			}
			if (ScenePortalManager.ActiveSceneName.Equals("Town10")) {
				___inspectionText = "Hm? Going out to the deep wilderness?";
			} else {
				___inspectionText = "You headin' into Sun Haven?";
			}
			___interacting = true;
			Player.Instance.facingDirection = Direction.North;
			___notEnoughMoney = false;
			DialogueController.Instance.SetDefaultBox(___dialogueName);
			DialogueController.Instance.PushDialogue(new DialogueNode {
				dialogueText = new List<string> { ___inspectionText },
				responses = new Dictionary<int, Response> {{
					1,
					new Response {
						responseText = () => "Yes, please",
						action = delegate {
							__instance.InteractionAction();
						}
					}
				}, {
					2,
					new Response {
						responseText = () => "No thanks.",
						action = delegate {
							__instance.EndInteract(0);
						}
					}
				}}
			});
			return false;
		}
	}

	[HarmonyPatch(typeof(SunHavenTaxi), "InteractionAction")]
	class HarmonyPatch_SunHavenTaxi_InteractionAction {

		private static bool Prefix() {
			if (ScenePortalManager.ActiveSceneName.Equals("Town10")) {
				DOVirtual.DelayedCall(0.5f, delegate
				{
					SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(101.707f, 123.4622f), "WildernessTaxi");
				});
			} else {
				DOVirtual.DelayedCall(0.5f, delegate
				{
					SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(266.6114f, 219.1894f), "Town10");
				});
			}
			return false;
		}
	}

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
						}
						});
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
				}
				});
			} else {
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> { "<i>(The griffon doesn't seem to even notice you, maybe it's best to leave it alone for right now.)</i>" }
				});
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(Inventory), "HasEnough")]
	class HarmonyPatch_Inventory_HasEnough {

		private static bool Prefix(int id, ref bool __result) {
			if (id == ItemID.KingMinossEmblem) {
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_Player_Awake {

		private static bool Prefix() {
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("FishingRoom", value: false);
			return true;
		}
	}

	[HarmonyPatch(typeof(FishingRoom), "Initialize")]
	class HarmonyPatch_FishingRoom_Initialize {

		private static bool Prefix() {
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("FishingRoom", value: false);
			Player.Instance.PausePlayerWithDialogue("fishRoom", "We've arrived, traveler.  Take your time... I'm not going anywhere.", delegate {});
			return false;
		}
	}

	[HarmonyPatch(typeof(FishingRoom), "Update")]
	class HarmonyPatch_FishingRoom_Update {

		private static bool Prefix(ref TextMeshProUGUI ___timerTMP) {
			___timerTMP.text = "Time left: infinite";
			return false;
		}
	}

	[HarmonyPatch(typeof(FishingRoom), "Disable")]
	class HarmonyPatch_FishingRoom_Disable {

		private static bool Prefix() {
			return false;
		}
	}

}