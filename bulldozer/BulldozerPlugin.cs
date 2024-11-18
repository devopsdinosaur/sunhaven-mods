using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using System;
using System.Reflection;

public static class PluginInfo {

	public const string TITLE = "Bulldozer";
	public const string NAME = "bulldozer";
	public const string SHORT_DESCRIPTION = "Become a walking bulldozer. No need for scythes, axes, or pickaxes. Just walk by fully grown crops, trees, weeds, pots, and rocks/ores to get your stuff! Each item is individually configurable.";

	public const string VERSION = "0.0.5";

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
public class SoundManagerPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private static List<int> m_excluded_crop_ids = new List<int>();

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			List<string> excluded = new List<string>();
			string[] vals = Settings.m_excluded_crops.Value.Split(',');
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
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void be_the_bulldozer() {
		try {

			void bulldoze_crop(Vector2Int pos, ref bool done) {
				if (done || !(Settings.m_harvest_crops.Value || Settings.m_fertilize_earth2.Value || Settings.m_fertilize_fire2.Value || Settings.m_water.Value) || !GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Crop crop)) {
					return;
				}
				done = true;
				if (Settings.m_water.Value  && TileManager.Instance.IsWaterable(pos) && !TileManager.Instance.IsWatered(pos)) {
					TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
					Player.Instance.AddEXP(ProfessionType.Farming, 1f);
				}
				if (Settings.m_harvest_crops.Value && crop.CheckGrowth && !m_excluded_crop_ids.Contains(crop.id)) {
					crop.ReceiveDamage(new DamageInfo {hitType = HitType.Scythe});
					return;
				}
				if (Settings.m_fertilize_earth2.Value && crop.data.fertilizerType == FertilizerType.None) {
					crop.Fertilize(FertilizerType.Earth2);
				}
				if (Settings.m_fertilize_fire2.Value && crop.data.fertilizerType == FertilizerType.None) {
					crop.Fertilize(FertilizerType.Fire2);
				}
				if (crop.data.fertilizerType == FertilizerType.None) {
					return;
				}
				GameObject _fertilized = (GameObject) crop.GetType().GetTypeInfo().GetField("_fertilized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crop);
				ParticleSystem.MainModule main = _fertilized.GetComponent<ParticleSystem>().main;
				if (Settings.m_fertilize_earth2.Value && Settings.m_fertilize_fire2.Value) {
					main.startColor = new Color(1f, 1f, 1f);
				}
			}

			void bulldoze_forageable(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_harvest_breakables.Value || !GameManager.Instance.TryGetObjectSubTile<Forageable>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Forageable item)) {
					return;
				}
				done = true;
				if ((ForageCollectType) item.GetType().GetField("collectionType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item) != ForageCollectType.Breakable) {
					return;
				}
				item.Break();
			}

			void bulldoze_tree(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_harvest_trees.Value || !GameManager.Instance.TryGetObjectSubTile<Wish.Tree>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Wish.Tree tree)) {
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
				if (done || !Settings.m_harvest_fruit.Value || !GameManager.Instance.TryGetObjectSubTile<ForageTree>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out ForageTree tree)) {
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
					int fruit_id = (int) GetFruit.Invoke(tree, new object[] {index});
					Vector3 spot_pos = tree.spots[index].transform.position;
					Pickup.Spawn(spot_pos.x, spot_pos.y, spot_pos.z, fruit_id, 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					if (Utilities.Chance((float)GameSave.Exploration.GetNodeAmount("Exploration7c", 2) * 0.5f)) {
						Pickup.Spawn(spot_pos.x + 0.1f, spot_pos.y, spot_pos.z, fruit_id, 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					}
					if (Utilities.Chance(0.004f)) {
						Pickup.Spawn(spot_pos.x - 0.1f, spot_pos.y, spot_pos.z, Utilities.RandomItem(Wish.Tree.explorationMuseumItems), 1, homeIn: false, 0.1f, Pickup.BounceAnimation.Fall, 1.5f, 125f);
					}
					Player.Instance.AddEXP(ProfessionType.Exploration, (float) tree.GetType().GetField("forageEXP", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree));
					SetFruit.Invoke(tree, new object[] {index, false});
				}
				tree.SaveMeta();
			}

			void bulldoze_rock(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_harvest_rocks.Value || !GameManager.Instance.TryGetObjectSubTile<Rock>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Rock rock)) {
					return;
				}
				done = true;
				rock.Die();
			}

			void bulldoze_plant(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_harvest_weeds.Value || !GameManager.Instance.TryGetObjectSubTile<HealthDecoration>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out HealthDecoration item)) {
					return;
				}
				done = true;
				item.Die();
			}

			void bulldoze_wood(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_harvest_trees.Value || !GameManager.Instance.TryGetObjectSubTile<Wood>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Wood tree)) {
					return;
				}
				done = true;
				tree.Die();
			}

			void water_soil(Vector2Int pos, ref bool done) {
				if (done || !Settings.m_water.Value || !TileManager.Instance.IsWaterable(pos) || TileManager.Instance.IsWatered(pos)) {
					return;
				}
				done = true;
				TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
			}

			if (!(Settings.m_harvest_crops.Value || Settings.m_harvest_trees.Value)) {
				return;
			}
			Vector2Int player_pos = new Vector2Int((int) Player.Instance.ExactPosition.x, (int) Player.Instance.ExactPosition.y);
			for (int y = player_pos.y - Settings.m_influence_radius.Value; y < player_pos.y + Settings.m_influence_radius.Value; y++) {
				for (int x = player_pos.x - Settings.m_influence_radius.Value; x <= player_pos.x + Settings.m_influence_radius.Value; x++) {
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
				if (!Settings.m_enabled.Value || 
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