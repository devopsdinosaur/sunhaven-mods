﻿using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using QFSW.QC;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using ZeroFormatter;
using UnityEngine.SceneManagement;
using PSS;

[BepInPlugin("devopsdinosaur.sunhaven.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.testing");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void debug_log(object text) {
		logger.LogInfo(text);
	}

	public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent) {
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
			list_descendants(child, callback, indent + 1);
		}
		return true;
	}

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

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

    [HarmonyPatch(typeof(PlayerSettings), "SetCheatsEnabled")]
    class HarmonyPatch_PlayerSettings_SetCheatsEnabled {

        private static bool Prefix(ref bool enable) {
			logger.LogInfo("Enabling cheats.");
            enable = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "RequestSleep")]
	class HarmonyPatch_Player_RequestSleep {

		private static bool Prefix(Player __instance, Bed bed, ref bool ____paused, ref UnityAction ___OnUnpausePlayer) {
			try {
				DialogueController.Instance.SetDefaultBox();
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> { "Would you like to sleep?" },
					responses = new Dictionary<int, Response> {{
						0,
						new Response
						{
							responseText = () => "Yes",
							action = delegate {
								__instance.GetType().GetTypeInfo().GetMethod("StartSleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {bed});
							}
						}
					}, {
						1,
						new Response
						{
							responseText = () => "No",
							action = delegate {
								DialogueController.Instance.CancelDialogue(animate: true, null, showActionBar: true);
							}
						}
					}
				}
				});
				____paused = true;
				___OnUnpausePlayer = (UnityAction) Delegate.Combine(___OnUnpausePlayer, (UnityAction) delegate {
					DialogueController.Instance.CancelDialogue();
				});
				return false;
			} catch (Exception e) { 
				logger.LogError("** HarmonyPatch_Player_RequestSleep.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LiamWheat), "ReceiveDamage")]
	class HarmonyPatch_LiamWheat_ReceiveDamage {

		private static bool Prefix(ref LiamWheat __instance, ref DamageHit __result) {
			AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.cropHit, __instance.transform.position);
			UnityEngine.Object.Destroy(__instance.gameObject);
			__result = new DamageHit {
				hit = true,
				damageTaken = 1f
			};
			Pickup.Spawn(
				__instance.transform.position.x + 0.5f, 
				__instance.transform.position.y + 0.707106769f, 
				__instance.transform.position.z, 
				ItemID.Wheat
			);
			return false;
		}
	}

	// Player(Clone) => UI => Dialogue => DialoguePanel => BustOffset => Bust

	class SelfBustController : MonoBehaviour {

		private void Awake() {
			try {
				debug_log("self bust awake!");
				Transform bust_offset = this.gameObject.transform.Find("BustOffset");
				GameObject self_bust = GameObject.Instantiate<GameObject>(bust_offset.gameObject, this.gameObject.transform);
				self_bust.name = "SelfBustOffset";
				self_bust.transform.localPosition = self_bust.transform.position + Vector3.left * 20;
			} catch (Exception e) {
                logger.LogError("** SelfBustController.Awake ERROR - " + e);
            }
        }
    }

	class NpcSummoner : MonoBehaviour {
		
		private NPCAI m_npc = null;
		private bool m_is_summoned = false;
		private string m_prev_scene;
		private Vector3 m_prev_pos;
		private AIState m_prev_ai_state;
		private Direction m_prev_face_direction;

        private void Awake() {
			this.m_npc = this.gameObject.GetComponent<NPCAI>();
		}

        public static void summon_npc(string name, Player player) {
			foreach (NPCAI npc in Resources.FindObjectsOfTypeAll(typeof(NPCAI))) {
                if (npc.ActualNPCName != name) {
                    continue;
                }
				NpcSummoner summoner = npc.gameObject.GetComponent<NpcSummoner>();
				if (summoner == null) {
					summoner = npc.gameObject.AddComponent<NpcSummoner>();
				}
				summoner.summon(player);
                break;
            }
        }

        private void summon(Player player) {
            const float OFFSET = 2f;
			if (!m_is_summoned) {
				this.m_is_summoned = true;
				this.m_prev_ai_state = this.m_npc.AIState;
				this.m_prev_scene = this.m_npc.Scene;
				this.m_prev_pos = this.m_npc.transform.position;
				this.m_prev_face_direction = m_npc.FacingDirection;
			}
            this.m_npc.Scene = ScenePortalManager.ActiveSceneName;
            this.m_npc.SetAIState(AIState.Still);
            switch (player.facingDirection) {
            case Direction.North:
				this.m_npc.transform.position = player.transform.position + Vector3.up * OFFSET;
				this.m_npc.FacingDirection = Direction.South;
				break;
            case Direction.South:
				this.m_npc.transform.position = player.transform.position + Vector3.down * OFFSET;
				this.m_npc.FacingDirection = Direction.North;
				break;
            case Direction.East:
				this.m_npc.transform.position = player.transform.position + Vector3.right * OFFSET;
				this.m_npc.FacingDirection = Direction.West;
				break;
            case Direction.West:
				this.m_npc.transform.position = player.transform.position + Vector3.left * OFFSET;
				this.m_npc.FacingDirection = Direction.East;
				break;
            }
            this.m_npc.gameObject.SetActive(true);
        }
	}

    [HarmonyPatch(typeof(Player), "Update")]
    class HarmonyPatch_Player_Update {

        private static void Postfix(Player __instance) {
            try {
				if (!__instance.IsOwner || !Input.GetKeyDown(KeyCode.Backspace)) {
					return;
				}
				summon_npc("Anne", __instance);
            } catch (Exception e) {
                logger.LogError("** XXXXX.Prefix ERROR - " + e);
            }
        }
    }

	[HarmonyPatch(typeof(DialogueController), "Awake")]
	class HarmonyPatch_DialogueController_Awake {

		private static bool Prefix(DialogueController __instance, GameObject ____dialoguePanel) {
			try {
				____dialoguePanel.AddComponent<SelfBustController>();
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_DialogueController_Awake.Postfix ERROR - " + e);
			}
			return true;
		}
	}

    [HarmonyPatch(typeof(DialogueController), "Update")]
    class HarmonyPatch_DialogueController_Update {

        private static bool Prefix(DialogueController __instance, GameObject ____dialoguePanel) {
            try {
				
                return true;
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_DialogueController_Update.Prefix ERROR - " + e);
            }
            return true;
        }
    }

    /*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {

		private static bool Prefix() {
			try {

				return false;
			} catch (Exception e) {
				logger.LogError("** XXXXX.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {

		private static void Postfix() {
			try {

				
			} catch (Exception e) {
				logger.LogError("** XXXXX.Postfix ERROR - " + e);
			}
		}
	}
	*/
}