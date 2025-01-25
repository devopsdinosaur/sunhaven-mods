using BepInEx;
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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using UnityEngine.Experimental.Rendering;

public static class PluginInfo {

    public const string TITLE = "Testing";
    public const string NAME = "testing";
    public const string SHORT_DESCRIPTION = "For testing purposes only";

    public const string VERSION = "0.0.0";

    public const string AUTHOR = "devopsdinosaur";
    public const string GAME_TITLE = "Sun Haven";
    public const string GAME = "sunhaven";
    public const string GUID = AUTHOR + "." + GAME + "." + NAME;
    public const string REPO = "sunhaven-mods";

    public static Dictionary<string, string> to_dict() {
        Dictionary<string, string> info = new Dictionary<string, string>();
        foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
            info[field.Name.ToLower()] = (string) field.GetValue(null);
        }
        return info;
    }
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class TestingPlugin:DDPlugin {
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

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

    class NpcSummoner : MonoBehaviour {
		
		private NPCAI m_npc = null;
		private bool m_is_summoned = false;
		private string m_prev_scene;
		private Vector3 m_prev_pos;
		private AIState m_prev_ai_state;
		private Direction m_prev_face_direction;
		private float m_update_elapsed = 0;

        private void Awake() {
			this.m_npc = this.gameObject.GetComponent<NPCAI>();
		}

        public static void summon_npc(string name, Player player) {
			try {
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
			} catch (Exception e) {
				logger.LogError("** NpcSummoner.summon_npc ERROR - " + e);
			}
		}

        private void summon(Player player) {
			try {
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
			} catch (Exception e) {
                logger.LogError("** NpcSummoner.summon ERROR - " + e);
            }
}

		private void Update() {
			try {
				if (!this.m_is_summoned || (this.m_update_elapsed += Time.deltaTime) < 1) {
					return;
				}
				this.m_update_elapsed = 0;
				if (ScenePortalManager.ActiveSceneName == this.m_npc.Scene) {
					return;
				}
				//debug_log($"NpcSummoner.Update - player left scene (active scene: {ScenePortalManager.ActiveSceneName}, npc scene: {this.m_npc.Scene}; returning {this.m_npc.ActualNPCName} to previous scene: {this.m_prev_scene}, pos: {this.m_prev_pos}, state: {this.m_prev_ai_state}");
				this.m_npc.Scene = this.m_prev_scene;
				this.m_npc.transform.position = this.m_prev_pos;
				this.m_npc.FacingDirection = this.m_prev_face_direction;
				this.m_npc.SetAIState(this.m_prev_ai_state);
				this.m_is_summoned = false;
			} catch (Exception e) {
				logger.LogError("** NpcSummoner.Update ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(Relationships), "OnEnable")]
	class HarmonyPatch_Relationships_OnEnable {

		private static void Postfix(Relationships __instance, Dictionary<string, RelationshipPanel> ___relationshipPanels) {
			try {
				foreach (KeyValuePair<string, RelationshipPanel> item in ___relationshipPanels) {
					item.Value.npcImage.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(new UnityAction(delegate() {
						NpcSummoner.summon_npc(item.Key, Player.Instance);
						UIHandler.Instance.CloseInventory();
					}));
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Relationships_OnEnable.Postfix ERROR - " + e);
			}
		}
	}
	
    [HarmonyPatch(typeof(Player), "Update")]
    class HarmonyPatch_Player_Update {

        private static void Postfix(Player __instance) {
            try {
				if (!__instance.IsOwner || !Input.GetKeyDown(KeyCode.Backspace)) {
					return;
				}
				NpcSummoner.summon_npc("Anne", __instance);
            } catch (Exception e) {
                logger.LogError("** XXXXX.Prefix ERROR - " + e);
            }
        }
    }

	[HarmonyPatch(typeof(Shop), "GenerateRandomItems")]
	class HarmonyPatch_Shop_GenerateRandomItems {

		private static void Postfix(Shop __instance) {
			try {
				_debug_log(__instance.name);
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Shop_GenerateRandomItems.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(RandomShopTable2), "GenerateShopItemList")]
	class HarmonyPatch_RandomShopTable2_GenerateShopItemList {

		private static bool Prefix(RandomShopTable2 __instance) {
			try {
				//_debug_log("!!!!!!!!!!!!!!!!!!!!");
				__instance.randomShopItemAmount = 9999;
				if (__instance.shopItems.Count == 0) {
					_debug_log("Zero items!?");
					return true;
				}
				//Call Shop.SetupBuyableItem and add items in ShopUI.OpenUI.Prefix
				ShopLoot2 template = __instance.shopItems[0];
				__instance.shopItems.Add(new ShopLoot2() {
					id = ItemID.DeadCrop,
					price = 1,
					orbs = 0,
					tickets = 0,
					chance = 1f,
					amount = template.amount,
					characterProgressIDs = template.characterProgressIDs,
					worldProgressIDs = template.worldProgressIDs,
					saleItem = true,
					itemToUseAsCurrency = template.itemToUseAsCurrency
				});
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_SaleManager_GenerateMerchantShops.Prefix ERROR - " + e);
			}
			return true;
		}

		private static void Postfix(RandomShopTable2 __instance, List<ShopLoot2> ___viableShopItems) {
			try {
				//foreach (ShopLoot2 item in ___viableShopItems) {
				//	_debug_log(item.id);
				//}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_SaleManager_GenerateMerchantShops.Postfix ERROR - " + e);
			}
		}
	}

	/*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			try {

				return false;
			} catch (Exception e) {
				_error_log("** XXXXX.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			try {
				
			} catch (Exception e) {
				_error_log("** XXXXX.Postfix ERROR - " + e);
			}
		}
	}
	*/
}
