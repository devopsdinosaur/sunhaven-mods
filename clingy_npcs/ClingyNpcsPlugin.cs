using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[BepInPlugin("devopsdinosaur.sunhaven.clingy_npcs", "Clingy NPCs", "0.0.3")]
public class ClingyNpcsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.clingy_npcs");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_grant_pet_buffs;
	private static ConfigEntry<float> m_pet_buff_multiplier;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_grant_pet_buffs = this.Config.Bind<bool>("General", "Grant Pet Buffs", false, "Set to true to make one or more following NPCs grant the built-in pet buffs, as specified in talents (will also cause pets to be dismissed when NPC is following).");
			m_pet_buff_multiplier = this.Config.Bind<float>("General", "Pet Buff Multiplier", 1f, "Multiplier applied to NPC 'pet' buffs (if enabled by 'Grant Pet Buffs') (float, default 1, 1 == no change).");
			this.m_harmony.PatchAll();
			logger.LogInfo("devopsdinosaur.sunhaven.clingy_npcs v0.0.3 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void debug_log(object text) {
		logger.LogInfo(text);
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
					item.Value.npcImage.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(new UnityAction(delegate () {
						NpcSummoner.summon_npc(item.Key, Player.Instance);
						UIHandler.Instance.CloseInventory();
					}));
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Relationships_OnEnable.Postfix ERROR - " + e);
			}
		}
	}

	class ClingyNpc : MonoBehaviour {

		private const int RESPONSE_KEY_FOLLOW_ME = 99998;
		private const int RESPONSE_KEY_GO_AWAY = 99999;
		private const float DISTANCE_MULTIPLIER = 2f;

		private NPCAI m_npc = null;
		private bool m_is_following = false;

		private static List<int> m_followers = new List<int>();
		private static HourlyBuff m_pet_buff = new HourlyBuff {
			entity = Player.Instance,
			duration = 6000000f,
			buffType = BuffType.PetParade,
			buffName = "NPC Promenade",
			buffDescription = "Gives exp over time"
		};

		[HarmonyPatch(typeof(NPCAI), "Awake")]
		class ClingyNpc_HarmonyPatch_NPCAI_Awake {
			private static void Postfix(NPCAI __instance, ref Transform ____followTarget) {
				try {
					__instance.gameObject.AddComponent<ClingyNpc>();
				} catch (Exception e) {
					logger.LogError("** ClingyNpc_HarmonyPatch_NPCAI_Awake.Postfix ERROR - " + e);
				}
			}
		}

		private void Awake() {
			this.m_npc = this.GetComponent<NPCAI>();
		}

		[HarmonyPatch(typeof(NPCAI), "Update")]
		class HarmonyPatch_NPCAI_Update {
			private static void Postfix(NPCAI __instance, Transform ____followTarget, AIState ____aiState) {
				__instance.gameObject.GetComponent<ClingyNpc>()?.npcai_update(__instance, ____followTarget, ____aiState);
			}
		}

		private void npcai_update(NPCAI __instance, Transform ____followTarget, AIState ____aiState) {
			try {
				if (!this.m_is_following) {
					return;
				}
				if (m_grant_pet_buffs.Value && Player.Instance.PlayerPet != null) {
					PetManager.Instance.DespawnPet(Player.Instance);
				}
				int distance = 1;
				foreach (int hash in m_followers) {
					if (hash == this.GetHashCode()) {
						break;
					}
					distance++;
				}
				if (Vector3.Distance(__instance.transform.position, Player.Instance.transform.position) > distance * DISTANCE_MULTIPLIER) {
					__instance.SetAIState(AIState.Follow);
					____followTarget = Player.Instance.transform;
				} else {
					__instance.SetAIState(AIState.Still);
				}
			} catch (Exception e) {
				logger.LogError("** ClingyNpc.npcai_update ERROR - " + e);
			}
		}

		private void reset_position() {
			this.m_npc.Scene = ScenePortalManager.ActiveSceneName;
			this.m_npc.transform.position = Player.Instance.transform.position;
		}

		[HarmonyPatch(typeof(LocalizedDialogueTree), "Talk")]
		class HarmonyPatch_LocalizedDialogueTree_Talk {
			private static bool Prefix(LocalizedDialogueTree __instance, string key, bool cancelDialogue, UnityAction onComplete, Dictionary<string, DialogueNode> ___tree) {
				ClingyNpc npc = __instance.npc.gameObject.GetComponent<ClingyNpc>();
				return (npc != null ? npc.talk(__instance, key, cancelDialogue, onComplete, ___tree) : true);
			}
		}

		private bool talk(LocalizedDialogueTree __instance, string key, bool cancelDialogue, UnityAction onComplete, Dictionary<string, DialogueNode> ___tree) {
			try {
				if (!__instance.questToTurnIn.IsNullOrWhiteSpace() && Player.Instance.QuestList.HasQuest(__instance.questToTurnIn) && (__instance.QuestIcons.QuestIsComplete || Player.Instance.QuestList.GetQuest(__instance.questToTurnIn).CheckForCompletion(null)) && key == "00" && __instance.readyToTurnInQuest) {
					QuestAsset quest = SingletonBehaviour<QuestManager>.Instance.GetQuest(__instance.questToTurnIn);
					if (!quest.endTex.IsNullOrWhiteSpace()) {
						Dictionary<int, Response> dictionary = new Dictionary<int, Response>();
						for (int i = 0; i < quest.farewellMessages.Count; i++) {
							string s = LocalizeText.TranslateText(quest.farewellMessages[i].keyFarewellText, quest.farewellMessages[i].farewellText);
							dictionary.Add(i + 1, new Response {
								responseText = () => s,
								action = delegate {
									__instance.CompleteQuest();
									__instance.OnEnd?.Invoke();
								}
							});
						}
						string item = LocalizeText.TranslateText(quest.keyEndTex, quest.endTex);
						DialogueController.Instance.PushDialogue(new DialogueNode {
							dialogueText = new List<string> { item },
							responses = dictionary
						}, onComplete);
					} else {
						__instance.CompleteQuest();
					}
				} else if (___tree.ContainsKey(key)) {
					if (cancelDialogue) {
						DialogueController.Instance.CancelDialogue(animate: false);
					}
					DialogueNode node = ___tree[key];
					if (node.responses.ContainsKey(RESPONSE_KEY_FOLLOW_ME)) {
						node.responses.Remove(RESPONSE_KEY_FOLLOW_ME);
					}
					if (node.responses.ContainsKey(RESPONSE_KEY_GO_AWAY)) {
						node.responses.Remove(RESPONSE_KEY_GO_AWAY);
					}
					if (!this.m_is_following) {
						node.responses[RESPONSE_KEY_FOLLOW_ME] = new Wish.Response() {
							responseText = this.localize_follow_me,
							action = this.on_response_follow_me
						};
					} else {
						node.responses[RESPONSE_KEY_GO_AWAY] = new Wish.Response() {
							responseText = this.localize_go_away,
							action = this.on_response_go_away
						};
					}
					DialogueController.Instance.PushDialogue(node, onComplete);
				} else {
					__instance.OnEnd?.Invoke();
					__instance.npc.EndInteract(0);
					DialogueController.Instance.CancelDialogue();
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** ClingyNpc.talk ERROR - " + e);
			}
			return true;
		}

		private string localize_follow_me() {
			return "Follow me!";
		}

		private string localize_go_away() {
			return "Go away!";
		}

		private void on_response_follow_me() {
			DialogueController.Instance.CancelDialogue();
			this.start_following();
		}

		private void on_response_go_away() {
			DialogueController.Instance.CancelDialogue();
			this.stop_following();
		}

		private float get_movement_speed_bonus() {
			switch (GameSave.Farming.GetNodeAmount("Farming7d")) {
			case 1: return m_pet_buff_multiplier.Value;
			case 2: return 2 * m_pet_buff_multiplier.Value;
			}
			return 5 * m_pet_buff_multiplier.Value;
		}

		[HarmonyPatch(typeof(SkillStats), "GetStat")]
		class HarmonyPatch_SkillStats_GetStat {
			private static void Postfix(StatType stat, ref float __result) {
				if (stat == StatType.Movespeed && m_grant_pet_buffs.Value && m_followers.Count > 0) {
					__result *= m_pet_buff_multiplier.Value;
				}
			}
		}

		[HarmonyPatch(typeof(PetManager), "SpawnPet", new Type[] {typeof(string), typeof(Player), typeof(PetItem), typeof(Action<Pet>)})]
		class HarmonyPatch_PetManager_SpawnPet {
			private static bool Prefix() {
				return !(m_grant_pet_buffs.Value && m_followers.Count > 0);
			}
		}

		private void start_following() {
			if (m_grant_pet_buffs.Value && m_followers.Count == 0) {
				float exp_amount = 2f * (float) GameSave.Farming.GetNodeAmount("Farming7d") * m_pet_buff_multiplier.Value;
				m_pet_buff.onHourChange = delegate(int hour, int minute) {
					if (minute == 0) {
						Player.Instance.AddEXP(ProfessionType.Exploration, exp_amount);
					}
				};
				m_pet_buff.buffDescription = $"Receive <color=#B7FFA3>{exp_amount} {ProfessionType.Exploration} experience</color> every hour and grants a <color=#B7FFA3>bonus movement speed of {this.get_movement_speed_bonus()}</color>.";
				Player.Instance.ReceiveBuff(BuffType.PetParade, m_pet_buff);
			}
			m_followers.Add(this.GetHashCode());
			this.m_is_following = true;
			this.m_npc.SetAIState(AIState.Follow);
			ReflectionUtils.get_field(this.m_npc, "_followTarget").SetValue(this.m_npc, Player.Instance.transform);
		}

		private void stop_following() {
			if (m_followers.Count == 1 && (m_grant_pet_buffs.Value || (Player.Instance.PlayerPet == null))) {
				Player.Instance.FinishBuff(BuffType.PetParade);
			}
			m_followers.Remove(this.GetHashCode());
			this.m_is_following = false;
			this.m_npc.SetAIState(AIState.Still);
			ReflectionUtils.get_field(this.m_npc, "_followTarget").SetValue(this.m_npc, null);
		}

		[HarmonyPatch(typeof(AI), "Follow")]
		class HarmonyPatch_AI_Follow {
			private static bool Prefix(AI __instance) {
				ClingyNpc npc = __instance.gameObject.GetComponent<ClingyNpc>();
				return (npc != null ? npc.follow() : true);
			}
		}

		private void face_player() {
			const float X_BUFFER = 0.25f;
			const float Y_BUFFER = 0.25f;
			float x_diff = this.m_npc.transform.position.x - Player.Instance.transform.position.x;
			float abs_x_diff = Mathf.Abs(x_diff);
			float y_diff = this.m_npc.transform.position.y - Player.Instance.transform.position.y;
			float abs_y_diff = Mathf.Abs(y_diff);
			if (abs_x_diff < X_BUFFER && abs_y_diff < Y_BUFFER) {
				this.m_npc.FacingDirection = Direction.South;
			} else if (abs_x_diff > abs_y_diff) {
				this.m_npc.FacingDirection = (x_diff > 0 ? Direction.West : Direction.East);
			} else {
				this.m_npc.FacingDirection = (y_diff > 0 ? Direction.South : Direction.North);
			}
		}

		private bool follow() {
			try {
				if (!this.m_is_following) {
					return true;
				}
				if (this.m_npc.Scene != ScenePortalManager.ActiveSceneName) {
					reset_position();
				}
				this.face_player();
				this.m_npc.animation = 0;
				Vector2 vector = Player.Instance.transform.position - this.m_npc.transform.position;
				float magnitude = vector.magnitude;
				if (magnitude > 14f) {
					reset_position();
					return false;
				}
				if (magnitude > 2.25f) {
					this.m_npc.targetVelocity = vector.normalized * Mathf.Lerp(0.75f, Mathf.Max(1, 20), (magnitude - 2.25f) / 4f);
					return false;
				}
				this.m_npc.SetAIState(AIState.Still);
				return false;
			} catch (Exception e) {
				logger.LogError("** ClingyNpc.follow ERROR - " + e);
			}
			return true;
		}
	}

}