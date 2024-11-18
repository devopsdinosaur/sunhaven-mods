using BepInEx.Configuration;
using System.Collections.Generic;

public class Settings {
    public static Settings m_instance = null;
    public static Settings Instance {
        get {
            if (m_instance == null) {
                m_instance = new Settings();
            }
            return m_instance;
        }
    }
    public DDPlugin m_plugin = null;

    // General
    public static ConfigEntry<bool> m_enabled;
    public static ConfigEntry<string> m_log_level;
    public static ConfigEntry<bool> m_harvest_breakables;
    public static ConfigEntry<bool> m_harvest_crops;
    public static ConfigEntry<string> m_excluded_crops;
    public static ConfigEntry<bool> m_harvest_trees;
    public static ConfigEntry<bool> m_harvest_fruit;
    public static ConfigEntry<bool> m_harvest_rocks;
    public static ConfigEntry<bool> m_harvest_weeds;
    public static ConfigEntry<bool> m_water;
    public static ConfigEntry<bool> m_fertilize_earth2;
    public static ConfigEntry<bool> m_fertilize_fire2;
    public static ConfigEntry<int> m_influence_radius;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");
        m_influence_radius = this.m_plugin.Config.Bind<int>("General", "Bulldoze Radius", 2, "Radius of tiles around the player to bulldoze (int, note that larger values could significantly increase computation time)");
        m_harvest_breakables = this.m_plugin.Config.Bind<bool>("General", "Harvest Breakables", true, "Set to false to disable bulldozing jars and pots.");
        m_harvest_crops = this.m_plugin.Config.Bind<bool>("General", "Harvest Crops", true, "Set to false to disable crop harvest.");
        m_excluded_crops = this.m_plugin.Config.Bind<string>("General", "Excluded Crops", "HoneyFlowerSeeds,LavenderSeeds,HibiscusSeeds,LilySeeds,OrchidSeeds,SunflowerSeeds,RedRoseSeeds,BlueRoseSeeds,TulipSeeds,LotusSeeds,DaisySeeds", "[Advanced] Comma-separated list of crop seed IDs to exclude from bulldozing.  By default this is a list of flower seeds in order to protect honey production.  NOTE: This value is parsed when the mod is loaded; changing the value with ConfigurationManager will have no effect.");
        m_harvest_fruit = this.m_plugin.Config.Bind<bool>("General", "Harvest Fruit", true, "Set to false to disable tree-fruit harvest.");
        m_harvest_rocks = this.m_plugin.Config.Bind<bool>("General", "Harvest Rocks", true, "Set to false to disable bulldozing rocks and ores.");
        m_harvest_trees = this.m_plugin.Config.Bind<bool>("General", "Harvest Trees", true, "Set to false to disable bulldozing fully-grown trees.");
        m_harvest_weeds = this.m_plugin.Config.Bind<bool>("General", "Harvest Weeds", true, "Set to false to disable bulldozing weeds.");
        m_water = this.m_plugin.Config.Bind<bool>("General", "Water Tilled Tiles", true, "Set to false to disable auto-watering of tilled tiles.");
        m_fertilize_earth2 = this.m_plugin.Config.Bind<bool>("General", "Fertilize Earth2", true, "If true then all crops in radius will be automatically fertilized with Earth Fertilizer 2 (can be combined with Fertilize Fire2 [combined fertilizer will produce a white floating particle])");
        m_fertilize_fire2 = this.m_plugin.Config.Bind<bool>("General", "Fertilize Fire2", true, "If true then all crops in radius will be automatically fertilized with Fire Fertilizer 2 (can be combined with Fertilize Earth2 [combined fertilizer will produce a white floating particle])");
    }
}