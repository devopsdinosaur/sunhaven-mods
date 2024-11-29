using BepInEx;
using HarmonyLib;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Wish;

public static class PluginInfo {

    public const string TITLE = "One Ring";
    public const string NAME = "one_ring";
    public const string SHORT_DESCRIPTION = "Tired of having to marry a dud just cuz the ring is good?  Now, regardless of your spouse, you can choose to get the stats of a particular ring (if you have a certain hearts level [configurable]), the best of all the stats, or the sum of all rings' stats (all based on configurable settings).  One ring to rule them all!";
	public const string EXTRA_DETAILS = "This mod does not make any permanent changes to any items.  It simply modifies the stats on the item in memory for the duration of the game.  Removing the mod and restarting the game will revert the item to its default state.";

	public const string VERSION = "0.0.2";

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
            Settings.Instance.load(this, OneRingController.on_setting_changed);
            DDPlugin.set_log_level(Settings.m_log_level.Value);
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

	public class OneRingController : MonoBehaviour {
		private const float RELATIONSHIP_LEVEL_PER_HEART = 5f;
		private const float STATS_UPDATE_FREQUENCY = 1f;

		private static OneRingController m_instance = null;
		public static OneRingController Instance => m_instance;
		private Dictionary<int, ArmorData> m_ring_datas;
		private int[] m_ring_ids;
		private Dictionary<int, List<Stat>> m_ring_stats;
		private bool m_all_rings_loaded = false;
		private bool m_settings_are_dirty = false;
		private Dictionary<string, float> m_prev_relationship_levels = null;

		public static void initialize(PlayerInventory player_inventory) {
			m_instance = player_inventory.gameObject.AddComponent<OneRingController>();
			m_instance.StartCoroutine(m_instance.load_rings_routine());
		}

		private IEnumerator load_rings_routine() {
			this.m_ring_datas = new Dictionary<int, ArmorData>();
			this.m_ring_stats = new Dictionary<int, List<Stat>>();
			int loaded_counter = 0;
			int failed_counter = 0;
			foreach (int id in Settings.m_rings.Keys) {
				Database.GetData(id, delegate (ItemData _data) {
					try {
						ArmorData data = _data as ArmorData;
						this.m_ring_datas[data.id] = data;
						this.m_ring_stats[data.id] = new List<Stat>(data.stats);
						loaded_counter++;
					} catch (Exception e) {
						_error_log("** Database.GetData callback ERROR - " + e);
						failed_counter++;
					}
				}, delegate {
					failed_counter++;
				});
			}
			for (;;) {
				if (loaded_counter + failed_counter >= Settings.m_rings.Count) {
					break;
				}
				yield return null;
			}
			this.m_ring_ids = this.m_ring_datas.Keys.ToArray();
			this.m_all_rings_loaded = true;
			this.StartCoroutine(this.periodic_update_routine());
		}

		public static void on_setting_changed(object sender, EventArgs e) {
			if (m_instance != null) {
				m_instance.m_settings_are_dirty = true;
			}
		}

		private IEnumerator periodic_update_routine() {
			for (;;) {
				if (Settings.m_enabled.Value) {
					if (this.m_settings_are_dirty) {
						_debug_log("Settings changed; updating rings.");
						this.update_ring_stats();
					} else {
						Dictionary<string, float> relationship_levels = new Dictionary<string, float>();
						foreach (Settings.RingInfo ring_info in Settings.m_rings.Values) {
							if (!SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships.TryGetValue(ring_info.name, out float relationship_level)) {
								relationship_level = 0;
							}
							relationship_levels[ring_info.name] = relationship_level;
						}
						bool is_dirty = this.m_prev_relationship_levels == null;
						if (!is_dirty) {
							foreach (string key in relationship_levels.Keys) {
								if (relationship_levels[key] != this.m_prev_relationship_levels[key]) {
									is_dirty = true;
									break;
								}
							}
						}
						this.m_prev_relationship_levels = relationship_levels;
						if (is_dirty) {
							_debug_log("One or more relationships changed or first load; updating rings.");
							this.update_ring_stats();
						}
					}
				}
				yield return new WaitForSeconds(STATS_UPDATE_FREQUENCY);
			}
		}

		private void update_ring_stats() {
			try {
				if (!Settings.m_enabled.Value || !this.m_all_rings_loaded) {
					return;
				}
				List<SlotItemData> ring_slots = new List<SlotItemData>();
				foreach (Inventory inventory in Resources.FindObjectsOfTypeAll<Inventory>()) {
					try {
						foreach (SlotItemData slot in inventory.Items) {
							if (slot.item == null || !(slot.item is ArmorItem item) || !m_ring_ids.Contains(item.id)) {
								continue;
							}
							ring_slots.Add(slot);
						}
					} catch {}
				}
				if (ring_slots.Count == 0) {
					_debug_log("No wedding rings found; nothing to do.");
					return;
				}
				_debug_log($"{ring_slots.Count} wedding rings found.");
				int used_counter = 0;
				Dictionary<StatType, float> total_stats = new Dictionary<StatType, float>();
				foreach (Settings.RingInfo ring_info in Settings.m_rings.Values) {
					float relationship_level = this.m_prev_relationship_levels[ring_info.name];
					_debug_log($"{ring_info.name} - relationship: {relationship_level}, enabled: {ring_info.enabled.Value}, sufficient_hearts: {relationship_level < Settings.m_required_hearts.Value * RELATIONSHIP_LEVEL_PER_HEART}");
					if (!ring_info.enabled.Value || relationship_level < Settings.m_required_hearts.Value * RELATIONSHIP_LEVEL_PER_HEART) {
						continue;
					}
					if (!m_ring_stats.TryGetValue(ring_info.id, out List<Stat> stats)) {
						_warn_log($"* update_ring_stats WARNING - no data found for {ring_info.name}'s wedding ring (id: {ring_info.id}).");
						continue;
					}
					used_counter++;
					foreach (Stat stat in stats) {
						if (total_stats.ContainsKey(stat.statType)) {
							if (Settings.m_combine_stats.Value) {
								total_stats[stat.statType] += stat.value;
							} else if (stat.value > total_stats[stat.statType]) {
								total_stats[stat.statType] = stat.value;
							}
						} else {
							total_stats[stat.statType] = stat.value;
						}
					}
				}
				if (used_counter == 0) {
					_debug_log($"No enabled rings or insufficient relationship; falling back to default stats for all wedding rings.");
					foreach (SlotItemData slot in ring_slots) {
						this.m_ring_datas[slot.item.ID()].stats = this.m_ring_stats[slot.item.ID()];
						slot.item = this.m_ring_datas[slot.item.ID()].GetItem();
					}
				} else {
					List<Stat> data_stats = new List<Stat>();
					_debug_log("Setting the following stats on all wedding rings:");
					foreach (KeyValuePair<StatType, float> kvp in total_stats) {
						data_stats.Add(new Stat(kvp.Key, kvp.Value));
						_debug_log($"--> {kvp.Key} = {kvp.Value}");
					}
					foreach (SlotItemData slot in ring_slots) {
						this.m_ring_datas[slot.item.ID()].stats = data_stats;
						slot.item = this.m_ring_datas[slot.item.ID()].GetItem();
					}
				}
			} catch (Exception e) {
				_error_log("** OneRingController.update_ring_stats ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerInventory), "LoadPlayerInventory")]
	class HarmonyPatch_PlayerInventory_LoadPlayerInventory {
		private static void Postfix(PlayerInventory __instance) {
			OneRingController.initialize(__instance);
		}
	}
}
