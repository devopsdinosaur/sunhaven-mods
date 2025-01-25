using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wish;
using UnityEngine;

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

    // Skills
    public static ConfigEntry<float> m_base_gold_fruit_chance;
    public static ConfigEntry<float> m_midas_gold_fruit_chance;
    public static ConfigEntry<float> m_fruit_spawn_chance;

    public static ConfigEntry<float> m_horn_of_plenty_chance;
    public static ConfigEntry<int> m_horn_of_plenty_checks;


    public ConfigEntry<T> create_entry<T>(string category, string name, T default_value, string description, EventHandler change_callback) {
        ConfigEntry<T> result = this.m_plugin.Config.Bind<T>(category, name, default_value, description);
        if (change_callback != null) {
            result.SettingChanged += change_callback;
        }
        return result;
    }

    public void load(DDPlugin plugin, EventHandler change_callback = null) {
        this.m_plugin = plugin;
        change_callback = on_setting_changed;

        // General
        m_enabled = this.create_entry("General", "Enabled", true, "Set to false to disable this mod.", change_callback);
        m_log_level = this.create_entry("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.", change_callback);
        DDPlugin.set_log_level(m_log_level.Value);

        // Skills
        m_base_gold_fruit_chance = this.create_entry("Skills", "Fruits of Midas - Base Gold Fruit Chance", 0.01f, "Base chance that fruit will be golden (added to the Fruits of Midas chance) [default 0.01f (1%)].", change_callback);
        m_midas_gold_fruit_chance = this.create_entry("Skills", "Fruits of Midas - Added Gold Fruit Chance Per Skill Point", 0.02f, "Percent chance per skill point of 'Fruits of Midas' added to the base chance that fruit will be golden [default 0.02f (2%)].", change_callback);
        m_fruit_spawn_chance = this.create_entry("Skills", "Fruits of Midas - Fruit Spawn Chance", 0.33f, "Percent chance that a fruit will spawn overnight [default 0.33f (33%)].  This chance is used up to three times per tree each night for each of the possible fruit spots.", change_callback);
        m_horn_of_plenty_chance = this.create_entry("Skills", "Horn of Plenty - Additional Fruit Chance Per Skill Point", 0.5f, "Percent chance per skill point of 'Horn of Plenty' to grant an additional fruit [float, game default 0.5 (50%)].", change_callback);
        m_horn_of_plenty_checks = this.create_entry("Skills", "Horn of Plenty - Number of Fruit Checks", 1, "Number of times the 'Horn of Plenty' skill will check for possible additional fruit [int, game default 1].  The game will roll this number of times using the 'Horn of Plenty - Percent Chance per Skill Point' value for each roll, effectively granting from zero to this number of extra fruit drops.", change_callback);
    }

    public static void on_setting_changed(object sender, EventArgs e) {
		
	}
}