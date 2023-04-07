using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroFormatter;
using DG.Tweening;


[BepInPlugin("devopsdinosaur.sunhaven.arborist", "Arborist", "0.0.2")]
public class EasyAnimalsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.arborist");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_overnight_growth_chance;
	private static ConfigEntry<float> m_tree_respawn_rate;
	private static ConfigEntry<float> m_overnight_mushroom_chance;
	private static ConfigEntry<float> m_overnight_cobweb_chance;
	private static ConfigEntry<int> m_tree_drop_amount_min;
	private static ConfigEntry<int> m_tree_drop_amount_max;
	private static ConfigEntry<float> m_seed_drop_chance;
	private static ConfigEntry<float> m_museum_item_drop_chance;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_overnight_growth_chance = this.Config.Bind<float>("General", "Overnight Growth Chance", 0.45f, "Float value between 0 (no chance) and 1 (100% chance) of single tree stage increment each night (default is game default of 0.45f).");
			m_tree_respawn_rate = this.Config.Bind<float>("General", "Tree Respawn Multiplier", 0.1f, "Float value between 0 (no chance) and infinity (higher == higher chance) of tree respawn in random tiles (this value is used as a multiplier with a perlin noise map for random tree placement, default is game default of 0.1f).");
			m_overnight_mushroom_chance = this.Config.Bind<float>("General", "Overnight Mushroom Gain Chance", 0.035f, "Float value between 0 (no chance) and 1 (100% chance) of tree gaining a mushroom each night (default is game default of 0.035f).");
			m_overnight_cobweb_chance = this.Config.Bind<float>("General", "Overnight Cobweb Gain Chance", 0.018f, "Float value between 0 (no chance) and 1 (100% chance) of tree gaining a cobweb each night (default is game default of 0.018f).");
			m_tree_drop_amount_min = this.Config.Bind<int>("General", "Tree Drop Minimum Amount", 4, "Minimum amount of items (i.e. wood) dropped when tree is cut down (default is game default of 4 [halved for stumps]).");
			m_tree_drop_amount_max = this.Config.Bind<int>("General", "Tree Drop Maximum Amount", 6, "Maxmum amount of items (i.e. wood) dropped when tree is cut down (default is game default of 6 [halved for stumps], note this is increased by exploration perks).");
			m_seed_drop_chance = this.Config.Bind<float>("General", "Seed Drop Chance", 0.15f, "Float value between 0 (no chance) and 1 (100% chance) of seed dropping when tree if cut down (default is game default of 0.15f [halved for stumps]).");			
			m_museum_item_drop_chance = this.Config.Bind<float>("General", "Museum Item Drop Chance", 0.035f, "Float value between 0 (no chance) and 1 (100% chance) of museum item dropping when tree if cut down (default is game default of 0.035f [halved for stumps]).");			
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.arborist v0.0.2 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Tree), "UpdateMetaOvernight")]
	class HarmonyPatch_Tree_UpdateMetaOvernight {

		private static bool Prefix(
			Tree __instance, 
			ref DecorationPositionData decorationData,
			List<Sprite> ____treeStages,
			List<Sprite> ____springStages,
			List<Sprite> ____summerStages,
			List<Sprite> ____fallStages,
			List<Sprite> ____winterStages
		) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				List<Sprite> TreeStages;
				switch (DayCycle.Instance.Season) {
				case Season.Summer: TreeStages = (____summerStages == null || ____summerStages.Count <= 0 ? ____treeStages : ____summerStages); break;
				case Season.Fall: TreeStages = (____fallStages == null || ____fallStages.Count <= 0 ? ____treeStages : ____fallStages); break;
				case Season.Winter: TreeStages = (____winterStages == null || ____winterStages.Count <= 0 ? ____treeStages : ____winterStages); break;
				default: TreeStages = (____springStages == null || ____springStages.Count <= 0 ? ____treeStages : ____springStages); break;
				}
				if (!Decoration.DeserializeMeta(decorationData.meta, ref __instance.data)) {
					__instance.data.fallen = false;
					__instance.data.mushroom = __instance.data.mushroom || Utilities.Chance(m_overnight_mushroom_chance.Value);
					__instance.data.cobweb = __instance.data.cobweb || Utilities.Chance(m_overnight_cobweb_chance.Value);
					__instance.data.stage = 1;
				} else if (__instance.data.stage <= 2 || Utilities.Chance(m_overnight_growth_chance.Value)) {
					__instance.data.stage = Mathf.Min(__instance.data.stage + 1, TreeStages.Count + 1);
				}
				try {
					__instance.data.fallen = __instance.data.fallen;
					__instance.data.mushroom = __instance.data.mushroom;
					__instance.data.cobweb = __instance.data.cobweb;
					__instance.data.stage = __instance.data.stage;
				} catch {
					__instance.data = new TreeSaveData {
						fallen = false,
						stage = TreeStages.Count + 1
					};
				}
				SceneSettings value;
				bool flag = SceneSettingsManager.Instance.sceneDictionary.TryGetValue(decorationData.sceneID, out value) && value.mapType != MapType.Farm;
				if (__instance.data.fallen && flag) {
					__instance.data.fallen = Utilities.Chance(0.5f);
				}
				if (!__instance.data.fallen && __instance.data.stage == TreeStages.Count + 1)
				{
					__instance.data.mushroom = __instance.data.mushroom || Utilities.Chance(m_overnight_mushroom_chance.Value);
					__instance.data.cobweb = __instance.data.cobweb || Utilities.Chance(m_overnight_mushroom_chance.Value);
				}
				decorationData.meta = ZeroFormatterSerializer.Serialize(__instance.data);
				return false;
			} catch (Exception e) {
				logger.LogError("** Tree.UpdateMetaOvernight_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FoliageManager), "UpdateDataTileOvernightTrees")]
	class HarmonyPatch_FoliageManager_UpdateDataTileOvernightTrees {

		private static bool Prefix(ref SceneSettings sceneSettings) {
			try {
				if (m_enabled.Value) {
					sceneSettings.treeRespawnRate = m_tree_respawn_rate.Value;
				}
				return true;
			} catch (Exception e) {
				logger.LogError("FoliageManager.UpdateDataTileOvernightTrees ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Tree), "SpawnDrops")]
	class HarmonyPatch_Tree_SpawnDrops {

		private static bool Prefix(
			Tree __instance,
			Vector3 fallPosition, 
			bool stumpDrop,
			Transform ___graphics,
			List<Sprite> ____treeStages,
			List<Sprite> ____springStages,
			List<Sprite> ____summerStages,
			List<Sprite> ____fallStages,
			List<Sprite> ____winterStages,
			ItemData ____itemData,
			TreeType ___treeType,
			ItemData ___seeds,
			RandomArray ___bonusDrops
		) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				List<Sprite> TreeStages;
				switch (DayCycle.Instance.Season) {
				case Season.Summer: TreeStages = (____summerStages == null || ____summerStages.Count <= 0 ? ____treeStages : ____summerStages); break;
				case Season.Fall: TreeStages = (____fallStages == null || ____fallStages.Count <= 0 ? ____treeStages : ____fallStages); break;
				case Season.Winter: TreeStages = (____winterStages == null || ____winterStages.Count <= 0 ? ____treeStages : ____winterStages); break;
				default: TreeStages = (____springStages == null || ____springStages.Count <= 0 ? ____treeStages : ____springStages); break;
				}
				if (!stumpDrop) {
					___graphics.gameObject.SetActive(value: false);
				}
				if (__instance.data.stage == TreeStages.Count + 1) {
					float num = 0f;
					if (!stumpDrop && GameSave.Exploration.GetNode("Exploration2b")) {
						num += (float)GameSave.Exploration.GetNodeAmount("Exploration2b");
					}
					float num2 = (float) UnityEngine.Random.Range(m_tree_drop_amount_min.Value, m_tree_drop_amount_max.Value) + num;
					if (stumpDrop) {
						num2 /= 2f;
					}
					for (int i = 0; (float) i < num2; i++) {
						float num3 = (stumpDrop ? 0.5f : 2f);
						float num4 = (stumpDrop ? 0.25f : 0.35f);
						Vector2 vector = new Vector2(UnityEngine.Random.Range(0f - num3, num3), UnityEngine.Random.Range(0f - num4, num4));
						Pickup.Spawn(fallPosition.x + vector.x, (fallPosition.y + vector.y - 0.5f) * 1.41421354f, 0f, ____itemData.id);
					}
					if (___treeType == TreeType.NelvariTree) {
						for (int j = 0; j < (stumpDrop ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(1, 4)); j++) {
							float num5 = (stumpDrop ? 0.5f : 2f);
							float num6 = (stumpDrop ? 0.25f : 0.35f);
							Vector2 vector2 = new Vector2(UnityEngine.Random.Range(0f - num5, num5), UnityEngine.Random.Range(0f - num6, num6));
							Pickup.Spawn(fallPosition.x + vector2.x, (fallPosition.y + vector2.y - 0.5f) * 1.41421354f, 0f, ItemID.ManaOrbs);
						}
					} else if (___treeType == TreeType.Oak && Time.time - PlayerFarmQuestManager.startTime > 30f && Utilities.Chance(0.3f)) {
						Pickup.Spawn(fallPosition.x, fallPosition.y * 1.41421354f, 0f, ItemID.ManaOrbs);
					}
					if (Utilities.Chance(0.3f)) {
						Pickup.Spawn(fallPosition.x, fallPosition.y * 1.41421354f, 0f, ItemID.ManaOrbs);
					}
					if (!stumpDrop) {
						int token = ItemID.SpringToken;
						switch (DayCycle.Instance.Season) {
						case Season.Summer: token = ItemID.SummerToken; break;
						case Season.Fall: token = ItemID.FallToken; break;
						case Season.Winter: token = ItemID.WinterToken; break;
						}
						for (int k = 0; (float) k < UnityEngine.Random.Range(0f, 2.25f); k++) {
							Pickup.Spawn(fallPosition.x, fallPosition.y * 1.41421354f, 0f, token);
						}
					}
					if ((bool) ___seeds && Utilities.Chance(stumpDrop ? m_seed_drop_chance.Value / 2 : m_seed_drop_chance.Value))
					{
						float num8 = (stumpDrop ? 0.5f : 2f);
						float num9 = (stumpDrop ? 0.25f : 0.35f);
						Vector2 vector3 = new Vector2(UnityEngine.Random.Range(0f - num8, num8), UnityEngine.Random.Range(0f - num9, num9));
						Pickup.Spawn(fallPosition.x + vector3.x, (fallPosition.y + vector3.y - 0.5f) * 1.41421354f, 0f, ___seeds.id);
					}
					if (!stumpDrop && GameSave.Exploration.GetNode("Exploration10b") && Utilities.Chance(0.05f * (float) GameSave.Exploration.GetNodeAmount("Exploration10b"))) {
						int amount;
						int randomDrop = ___bonusDrops.RandomItem(out amount).id;
						for (int l = 0; l < amount; l++) {
							int num10 = l;
							DOVirtual.DelayedCall(0.2f + 0.12f * (float)num10, delegate {
								Pickup.Spawn(fallPosition.x, (fallPosition.y - 0.5f) * 1.41421354f, 0f, randomDrop);
							});
						}
					}
					if (Utilities.Chance(stumpDrop ? m_museum_item_drop_chance.Value / 2 : m_museum_item_drop_chance.Value)) {
						int num11 = Tree.explorationMuseumItems.RandomItem();
						Pickup.Spawn(fallPosition.x, (fallPosition.y - 0.5f) * 1.41421354f, 0f, num11);
					}
				} else if (__instance.data.stage >= 3) {
					for (int m = 0; m < UnityEngine.Random.Range(__instance.data.stage - 4, __instance.data.stage - 1); m++) {
						Vector2 vector4 = new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(-0.3f, 0.3f));
						Pickup.Spawn(fallPosition.x + vector4.x, (fallPosition.y + vector4.y - 0.5f) * 1.41421354f, 0f, ____itemData.id);
					}
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** Tree.SpawnDrops_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}