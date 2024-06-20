using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Reflection;
using System.Collections.Generic;

[BepInPlugin("devopsdinosaur.sunhaven.always_open", "Always Open", "0.0.3")]
public class AlwaysOpenPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.always_open");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_shops_enabled;
	private static ConfigEntry<bool> m_houses_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_shops_enabled = this.Config.Bind<bool>("General", "Shops Enabled", true, "If true then shops will always be open, otherwise default open/close time");
			m_houses_enabled = this.Config.Bind<bool>("General", "Houses Enabled", true, "If true then NPC houses will always be open, otherwise default open/close time based on relationship");
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.always_open v0.0.3 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(DoorKnockController), "KnockOnDoor")]
	class HarmonyPatch_DoorKnockController_KnockOnDoor {

		private static bool Prefix(
			DoorKnockController __instance,
			ScenePortalSpot ___portal,
			Cutscene ___hangoutCutscene,
			bool ___hasHangOutCutscene,
			string ___hangOutMsg
		) {
			try {
				if (!m_enabled.Value || !m_houses_enabled.Value) {
					return true;
				}
				Dictionary<int, Response> dictionary = new Dictionary<int, Response>();
				dictionary.Add(0, new Response {
					responseText = () => "Enter anyways.",
					action = delegate {
						___portal.EnterHouse(delegate {
							Player.Instance.facingDirection = Direction.North;
						});
					}
				});
				dictionary.Add(1, new Response {
					responseText = () => "Knock.",
					action = delegate {
						__instance.GetType().GetMethod("SummonNPC", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
					}
				});
				if (___hangoutCutscene != null && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter(___hangoutCutscene.Progress.progressID) && ___hasHangOutCutscene) {
					dictionary.Add(2, new Response {
						responseText = () => ___hangOutMsg,
						action = delegate {
							___hangoutCutscene.Begin();
						}
					});
				}
				dictionary.Add(3, new Response {
					responseText = () => "Seeya!",
					action = delegate {
					}
				});
				DialogueController.Instance.CancelDialogue(animate: false);
				DialogueController.Instance.SetDefaultBox();
				DialogueController.Instance.PushDialogue(new DialogueNode {
					dialogueText = new List<string> {"I wonder if they're accepting visitors right now? Hmm..."},
					responses = dictionary
				});
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_DoorKnockController_KnockOnDoor.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ScenePortalSpot), "Awake")]
	class HarmonyPatch_ScenePortalSpot_Awake {

		private static void Postfix(
			ref bool ___hasOpenAndCloseTime, 
			ref bool ___hasRelationshipRequirement,
			bool ____hasKnock
		) {
			try {
				if (!m_enabled.Value || (____hasKnock && !m_houses_enabled.Value) || (!____hasKnock && !m_shops_enabled.Value)) {
					return;
				}
				___hasOpenAndCloseTime = false;
				___hasRelationshipRequirement = false;
			} catch (Exception e) {
				logger.LogError("** ScenePortalSpot.Awake_Postfix ERROR - " + e);
			}
		}
	}
}