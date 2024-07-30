using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using System;
using System.Reflection;

[BepInPlugin("devopsdinosaur.sunhaven.green_man", "Green Man", "0.0.6")]
public class GreenManPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.green_man");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_grow_crops;
	private static ConfigEntry<bool> m_grow_trees;
	private static ConfigEntry<int> m_influence_radius;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_grow_crops = this.Config.Bind<bool>("General", "Insta-grow Crops", true, "Set to false to disable crop insta-growth.");
			m_grow_trees = this.Config.Bind<bool>("General", "Insta-grow Trees", true, "Set to false to disable tree insta-growth.");
			m_influence_radius = this.Config.Bind<int>("General", "Green Influence Radius", 2, "Radius of tiles around the player in which 'green' influence spreads (int, note that larger values could significantly increase computation time)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.green_man v0.0.6" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	class InfluenceCollider : MonoBehaviour {

		private CircleCollider2D m_collider = null;

		private void Awake() {
			try {
				this.m_collider = this.gameObject.AddComponent<CircleCollider2D>();
				this.m_collider.isTrigger = true;
				this.m_collider.radius = m_influence_radius.Value;
			} catch (Exception e) {
				logger.LogError("** InfluenceCollider.Awake ERROR - " + e);
			}
		}
		
		private void OnTriggerEnter2D(Collider2D collider) {
		
			void grow_forage_tree(ForageTree tree, List<Sprite> sprites) {
				tree.data = new ForageTreeSaveData {
					spot1 = false,
					spot2 = false,
					spot3 = false,
					golden = true,
					stage = sprites.Count + 1
				};
				for (int index = 0; index < 3; index++) {
					tree.spots[index].sprite = null;
				}
				MeshGenerator treeMesh = (MeshGenerator) tree.GetType().GetField("treeMesh", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
				treeMesh.sprite = sprites[Mathf.Clamp(tree.data.stage - 2, 0, sprites.Count - 1)];
				((GameObject) tree.GetType().GetField("_decals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree)).SetActive(tree.data.stage >= 4);
				treeMesh.SetDefault();
				tree.GetType().GetMethod("SetDecalsEnabledBySeason", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tree, new object[] {});
				tree.SaveMeta();
			}

			try {
				if (!base.isActiveAndEnabled || !m_enabled.Value) {
					return;
				}
				if (m_grow_trees.Value) {
					Wish.Tree tree = collider.GetComponent<Wish.Tree>();
					if (tree != null) {
						List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float current_health = (float) tree.GetType().GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float max_health = (float) tree.GetType().GetProperty("MaxHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						if (sprites != null && current_health >= max_health) {
							tree.SetTreeStage(sprites.Count + 1);
						}
						return;
					}
					ForageTree forage_tree = collider.GetComponent<ForageTree>();
					if (forage_tree != null) {
						List<Sprite> sprites = (List<Sprite>) forage_tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(forage_tree);
						if (sprites != null) {
							grow_forage_tree(forage_tree, sprites);
						}
						return;
					}
				}
				if (m_grow_crops.Value) {
					Crop crop = collider.GetComponent<Crop>();
					if (crop != null && !crop.CheckGrowth) {
						crop.GrowToMax();
						crop.data.stage = crop.SeedData.cropStages.Length - 1;
						return;
					}
				}
			} catch (Exception e) {
				logger.LogError("** InfluenceCollider.OnTriggerEnter2D ERROR - " + e);
			}
		}
	}
	
	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_Player_Awake {

		private static void Postfix(Player __instance) {
			try {
				GameObject obj = GameObject.Instantiate<GameObject>(__instance.transform.Find("InteractionTrigger").gameObject, __instance.transform);
				obj.name = "Green_Man_Plugin_Influence_Collider";
				GameObject.Destroy(obj.GetComponent<BoxCollider2D>());
				GameObject.Destroy(obj.GetComponent<PlayerInteractions>());
				obj.AddComponent<InfluenceCollider>();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Awake.Postfix ERROR - " + e);
			}
		}
	}
}