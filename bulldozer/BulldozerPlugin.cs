
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using System;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.bulldozer", "Bulldozer", "0.0.3")]
public class BulldozerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.bulldozer");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_harvest_breakables;
	private static ConfigEntry<bool> m_harvest_crops;
	private static ConfigEntry<string> m_excluded_crops;
	private static ConfigEntry<bool> m_harvest_trees;
	private static ConfigEntry<bool> m_harvest_fruit;
	private static ConfigEntry<bool> m_harvest_rocks;
	private static ConfigEntry<bool> m_harvest_weeds;
	private static ConfigEntry<bool> m_water;
	private static ConfigEntry<bool> m_fertilize_earth2;
	private static ConfigEntry<bool> m_fertilize_fire2;
	private static ConfigEntry<int> m_influence_radius;

	private static List<int> m_excluded_crop_ids = new List<int>();
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_influence_radius = this.Config.Bind<int>("General", "Bulldoze Radius", 2, "Radius of tiles around the player to bulldoze (int, note that larger values could significantly increase computation time)");
			m_harvest_breakables = this.Config.Bind<bool>("General", "Harvest Breakables", true, "Set to false to disable bulldozing jars and pots.");
			m_harvest_crops = this.Config.Bind<bool>("General", "Harvest Crops", true, "Set to false to disable crop harvest.");
			m_excluded_crops = this.Config.Bind<string>("General", "Excluded Crops", "HoneyFlowerSeeds,LavenderSeeds,HibiscusSeeds,LilySeeds,OrchidSeeds,SunflowerSeeds,RedRoseSeeds,BlueRoseSeeds,TulipSeeds,LotusSeeds,DaisySeeds", "[Advanced] Comma-separated list of crop seed IDs to exclude from bulldozing.  By default this is a list of flower seeds in order to protect honey production.  NOTE: This value is parsed when the mod is loaded; changing the value with ConfigurationManager will have no effect.");
			m_harvest_fruit = this.Config.Bind<bool>("General", "Harvest Fruit", true, "Set to false to disable tree-fruit harvest.");
			m_harvest_rocks = this.Config.Bind<bool>("General", "Harvest Rocks", true, "Set to false to disable bulldozing rocks and ores.");
			m_harvest_trees = this.Config.Bind<bool>("General", "Harvest Trees", true, "Set to false to disable bulldozing fully-grown trees.");
			m_harvest_weeds = this.Config.Bind<bool>("General", "Harvest Weeds", true, "Set to false to disable bulldozing weeds.");
			m_water = this.Config.Bind<bool>("General", "Water Tilled Tiles", true, "Set to false to disable auto-watering of tilled tiles.");
			m_fertilize_earth2 = this.Config.Bind<bool>("General", "Fertilize Earth2", true, "If true then all crops in radius will be automatically fertilized with Earth Fertilizer 2 (can be combined with Fertilize Fire2 [combined fertilizer will produce a white floating particle])");
			m_fertilize_fire2 = this.Config.Bind<bool>("General", "Fertilize Fire2", true, "If true then all crops in radius will be automatically fertilized with Fire Fertilizer 2 (can be combined with Fertilize Earth2 [combined fertilizer will produce a white floating particle])");
			List<string> excluded = new List<string>();
			string[] vals = m_excluded_crops.Value.Split(',');
			for (int index = 0; index < vals.Length; index++) {
				string val = vals[index].Trim();
				if (val.Length > 0) {
					excluded.Add(val);
				}
			}
			foreach (FieldInfo field_info in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				if (field_info.IsLiteral && !field_info.IsInitOnly && excluded.Contains(field_info.Name)) {
					m_excluded_crop_ids.Add((int) field_info.GetRawConstantValue());
				}
			}
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.bulldozer v0.0.3" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void be_the_bulldozer() {
		try {

			void bulldoze_crop(Vector2Int pos, ref bool done) {
				if (done || !(m_harvest_crops.Value || m_fertilize_earth2.Value || m_fertilize_fire2.Value || m_water.Value) || !GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Crop crop)) {
					return;
				}
				done = true;
				if (m_water.Value  && TileManager.Instance.IsWaterable(pos) && !TileManager.Instance.IsWatered(pos)) {
					TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
					Player.Instance.AddEXP(ProfessionType.Farming, 1f);
				}
				if (m_harvest_crops.Value && crop.CheckGrowth && !m_excluded_crop_ids.Contains(crop.id)) {
					crop.ReceiveDamage(new DamageInfo {hitType = HitType.Scythe});
					return;
				}
				if (m_fertilize_earth2.Value && crop.data.fertilizerType == FertilizerType.None) {
					crop.Fertilize(FertilizerType.Earth2);
				}
				if (m_fertilize_fire2.Value && crop.data.fertilizerType == FertilizerType.None) {
					crop.Fertilize(FertilizerType.Fire2);
				}
				if (crop.data.fertilizerType == FertilizerType.None) {
					return;
				}
				GameObject _fertilized = (GameObject) crop.GetType().GetTypeInfo().GetField("_fertilized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crop);
				ParticleSystem.MainModule main = _fertilized.GetComponent<ParticleSystem>().main;
				if (m_fertilize_earth2.Value && m_fertilize_fire2.Value) {
					main.startColor = new Color(1f, 1f, 1f);
				}
			}

			void bulldoze_forageable(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_breakables.Value || !GameManager.Instance.TryGetObjectSubTile<Forageable>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Forageable item)) {
					return;
				}
				done = true;
				if ((ForageCollectType) item.GetType().GetField("collectionType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item) != ForageCollectType.Breakable) {
					return;
				}
				item.Break();
			}

			void bulldoze_tree(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_trees.Value || !GameManager.Instance.TryGetObjectSubTile<Tree>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Tree tree)) {
					return;
				}
				done = true;
				List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
				if (sprites == null || tree.data.stage < sprites.Count + 1 || (bool) tree.GetType().GetField("_dying", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree)) {
					return;
				}
				tree.Die(0.1f, Player.Instance.ExactPosition);
			}

			void bulldoze_fruit(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_fruit.Value || !GameManager.Instance.TryGetObjectSubTile<ForageTree>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out ForageTree tree)) {
					return;
				}
				done = true;
				List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
				if (sprites == null || tree.data.stage < sprites.Count + 1) {
					return;
				}
				MethodInfo GetFruit = tree.GetType().GetMethod("GetFruit", BindingFlags.Instance | BindingFlags.NonPublic);
				MethodInfo SetFruit = tree.GetType().GetMethod("SetFruit", BindingFlags.Instance | BindingFlags.NonPublic);
				for (int index = 0; index < tree.spots.Length; index++) {
					switch (index) {
					case 0: if (!tree.data.spot1) {continue;} break;
					case 1: if (!tree.data.spot2) {continue;} break;
					case 2: if (!tree.data.spot3) {continue;} break;
					}
					ItemData fruit = (ItemData) GetFruit.Invoke(tree, new object[] {index});
					Vector3 spot_pos = tree.spots[index].transform.position;
					Pickup.Spawn(spot_pos.x, spot_pos.y, spot_pos.z, fruit.id, 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					if (Utilities.Chance((float)GameSave.Exploration.GetNodeAmount("Exploration7c", 2) * 0.5f)) {
						Pickup.Spawn(spot_pos.x + 0.1f, spot_pos.y, spot_pos.z, fruit.id, 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					}
					if (Utilities.Chance(0.004f)) {
						Pickup.Spawn(spot_pos.x - 0.1f, spot_pos.y, spot_pos.z, Utilities.RandomItem(Tree.explorationMuseumItems), 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					}
					Player.Instance.AddEXP(ProfessionType.Exploration, (float) tree.GetType().GetField("forageEXP", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree));
					SetFruit.Invoke(tree, new object[] {index, false});
				}
				tree.SaveMeta();
			}

			void bulldoze_rock(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_rocks.Value || !GameManager.Instance.TryGetObjectSubTile<Rock>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Rock rock)) {
					return;
				}
				done = true;
				rock.Die();
			}

			void bulldoze_plant(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_weeds.Value || !GameManager.Instance.TryGetObjectSubTile<HealthDecoration>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out HealthDecoration item)) {
					return;
				}
				done = true;
				item.Die();
			}

			void bulldoze_wood(Vector2Int pos, ref bool done) {
				if (done || !m_harvest_trees.Value || !GameManager.Instance.TryGetObjectSubTile<Wood>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Wood tree)) {
					return;
				}
				done = true;
				tree.Die();
			}

			void water_soil(Vector2Int pos, ref bool done) {
				if (done || !m_water.Value || !TileManager.Instance.IsWaterable(pos) || TileManager.Instance.IsWatered(pos)) {
					return;
				}
				done = true;
				TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
			}

			if (!(m_harvest_crops.Value || m_harvest_trees.Value)) {
				return;
			}
			Vector2Int player_pos = new Vector2Int((int) Player.Instance.ExactPosition.x, (int) Player.Instance.ExactPosition.y);
			for (int y = player_pos.y - m_influence_radius.Value; y < player_pos.y + m_influence_radius.Value; y++) {
				for (int x = player_pos.x - m_influence_radius.Value; x <= player_pos.x + m_influence_radius.Value; x++) {
					Vector2Int pos = new Vector2Int(x, y);
					bool done = false;
					bulldoze_crop(pos, ref done);
					bulldoze_forageable(pos, ref done);
					bulldoze_fruit(pos, ref done);
					bulldoze_plant(pos, ref done);
					bulldoze_rock(pos, ref done);
					bulldoze_tree(pos, ref done);
					bulldoze_wood(pos, ref done);
					water_soil(pos, ref done);
				}
			}
		} catch (Exception e) {
			logger.LogError("** be_the_bulldozer ERROR - " + e);
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 0.1f;
		static float m_elapsed = CHECK_FREQUENCY;

		private static bool Prefix(ref Player __instance) {
			try {
				if (!m_enabled.Value || 
					(m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY || 
					GameManager.Instance == null || 
					TileManager.Instance == null ||
					Player.Instance == null
				) {
					return true;
				}
				m_elapsed = 0f;
				be_the_bulldozer();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}