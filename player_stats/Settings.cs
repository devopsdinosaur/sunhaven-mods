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

    // Stats
    public static Dictionary<StatType, ConfigEntry<float>> m_stats;

    // Skills
    public static SkillNodeDict m_skills;

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
        m_stats = new Dictionary<StatType, ConfigEntry<float>>();
		foreach (string stat_name in System.Enum.GetNames(typeof(StatType))) {
			m_stats[(StatType) System.Enum.Parse(typeof(StatType), stat_name)] = this.create_entry<float>("Stats", "Stats - Delta " + stat_name, 0f, "[float] Amount to increment/decrement the '" + stat_name + "' player stat (only during gameplay with mod enabled; not permanent).", change_callback);
		}
        m_skills = new SkillNodeDict(change_callback);
    }

    public static void on_setting_changed(object sender, EventArgs e) {
		m_skills.on_settings_changed();
	}
}