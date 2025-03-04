using BepInEx.Configuration;
using System;
using System.Collections.Generic;

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

    public static string[] m_table_names = {
		"Advance Furniture Table", 
		"Alchemy Table",
		"Anvil", 
		"Bakers Station",
		"Basic Furniture Table", 
		"BeeHiveBox",
		"Composter Table", 
		"Construction Table", 
		"Cooking Pot", 
		"Crafting Table", 
		"Elven Crafting Table", 
		"Elven Furnace",
		"Elven Furniture Table",
		"Elven Juicer", 
		"Elven Loom", 
		"Elven Seed Maker", 
		"Farmers Crafting Table",
		"Farmers Table", 
		"Fish Grill", 
		"Furnace", 
		"Furniture Table",
		"Grinder", 
		"Ice Cream Cart",
		"Industrial Cooking Stove",
		"Jam Maker",
		"Jewelry Table",
		"Juicer", 
		"Keg",
		"Loom", 
		"Mana Anvil", 
		"Mana Composter",
		"Monster Crafting Table", 
		"Mana Infuser Table", 
		"Mana Siphoner",
		"Monster Anvil", 
		"Monster Composter", 
		"Monster Furnace", 
		"Monster Furniture Table", 
		"Monster Juicer", 
		"Monster Loom", 
		"Monster Seed Maker", 
		"Monster Sushi Table", 
		"Nursery Crafting Table",
		"Oven", 
		"Painters Easel", 
		"Recycling Machine", 
		"Seed Maker", 
		"Soda Machine", 
		"Sushi Table", 
		"Tea Kettle", 
		"Tile Maker",
		"Withergate Anvil",
		"Withergate Furnace",
		"Wizard Crafting Table"
	};
	public static ConfigEntry<float> m_craft_speed;
	public static ConfigEntry<bool>[] m_table_enabled = new ConfigEntry<bool>[m_table_names.Length];
	public static ConfigEntry<float>[] m_table_speeds = new ConfigEntry<float>[m_table_names.Length];
	private static int m_beebox_index = -1;
	private static Dictionary<int, float> m_honey_times = new Dictionary<int, float>();

    public ConfigEntry<T> create_entry<T>(string category, string name, T default_value, string description, EventHandler change_callback) {
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
        DDPlugin.set_log_level(m_log_level.Value);
        change_callback = on_setting_changed;
		m_craft_speed = this.create_entry("General", "Craft Speed Multiplier", 10f, "Speed multiplier for item crafting (float, 1 = game default (1.2 for humans) [note: this stomps the human 20% passive; should not affect anything else])", change_callback);
		for (int index = 0; index < m_table_names.Length; index++) {
			if (m_table_names[index] == "BeeHiveBox") {
				m_beebox_index = index;
			}
			m_table_enabled[index] = this.create_entry("General", m_table_names[index] + " Enabled", true, "If true then the '" + m_table_names[index] + "' table will use the craft speed multiplier; if false then it will use the game default speed.", change_callback);
			m_table_speeds[index] = this.create_entry("General", m_table_names[index] + " Speed Multiplier", 0f, "If this value is non-zero and '" + m_table_names[index] + " Enabled' is true then this will be the craft speed multiplier used for the '" + m_table_names[index] + "' table (overriding the global one).  If this value is 0 then the global multiplier will be used (if table is enabled).", change_callback);
		}
    }

    public static void on_setting_changed(object sender, EventArgs e) {
		
	}
}