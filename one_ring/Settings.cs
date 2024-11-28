using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class Settings {
    private static Settings m_instance = null;
    public static Settings Instance {
        get {
            if (m_instance == null) {
                m_instance = new Settings();
            }
            return m_instance;
        }
    }
    private DDPlugin m_plugin = null;

    // General
    public static ConfigEntry<bool> m_enabled;
    public static ConfigEntry<string> m_log_level;
    public static ConfigEntry<int> m_required_hearts;
    public static ConfigEntry<bool> m_combine_stats;

    // Rings
    public class RingInfo {
        public int id;
        public string name;
        public ConfigEntry<bool> enabled;
    }
    public static Dictionary<int, RingInfo> m_rings = new Dictionary<int, RingInfo>();

    private ConfigEntry<T> create_entry<T>(string category, string name, T default_value, string description, EventHandler change_callback) {
        ConfigEntry<T> result = this.m_plugin.Config.Bind<T>(category, name, default_value, description);
        if (change_callback != null) {
            result.SettingChanged += change_callback;
        }
        return result;
    }

    public void load(DDPlugin plugin, EventHandler change_callback = null) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.create_entry("General", "Enabled", true, "Set to false to disable this mod.", change_callback);
        m_log_level = this.create_entry("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.", change_callback);
        m_required_hearts = this.create_entry("General", "Required Hearts", 10, "The number of relationship hearts required with the NPC to use the respective ring.  This defaults to 10: the maximum achievable with platonic friendship.  Set this to 0 to not require relationship.  If you do not have sufficient hearts with any of your ring-enabled NPCs then your ring will fall back to its default stats.  For example: If you're married to Anne but you want Kitty's ring stats then you set 'Ring - Kitty Enabled' to true.  If your relationship with Kitty is less than 'Required Hearts' then your ring will default to the Anne Wedding Ring stats until you reach the required relationship level.", change_callback);
        m_combine_stats = this.create_entry("General", "Combine Ring Stats", false, "If only one ring is enabled then its stats will simply override your current ring's stats (provided you have the required hearts, see 'Required Hearts' setting).  If multiple rings are enabled then this setting is utilized.  The default behavior for merging enabled rings is to take the maximum of the stat value if the rings affect the same stat (i.e. health).  Set this to true to instead add the stat values together.  This produces massive (i.e. cheaty ;) buffs.", change_callback);
        foreach (FieldInfo field in typeof(ItemID).GetFields()) {
            if (!field.Name.EndsWith("WeddingRing")) {
                continue;
            }
            RingInfo ring = new RingInfo() {
                id = (int) field.GetValue(null),
                name = field.Name.Substring(0, field.Name.Length - 11),
            };
            if (ring.id != ItemID.EnchantedWeddingRing) {
                ring.enabled = this.create_entry("Rings", $"Ring - {ring.name} Enabled", false, $"Set to true to use the stats of {ring.name}'s wedding ring.  See 'Required Hearts' and 'Combine Ring Stats' settings for details.", change_callback);
                m_rings.Add(ring.id, ring);
            }
        }
    }
}