using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;


[BepInPlugin("devopsdinosaur.sunhaven.free_rides", "Free Rides", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.free_rides");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.free_rides v0.0.1 loaded.");
		this.m_harmony.PatchAll();
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
}