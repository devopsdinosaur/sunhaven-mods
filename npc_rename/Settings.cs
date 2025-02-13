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
    public static ConfigEntry<string> m_player_name;

    // NPCs
    public static Dictionary<string, ConfigEntry<string>> m_npc_names;

    public ConfigEntry<T> create_entry<T>(string category, string name, T default_value, string description, EventHandler change_callback) {
        ConfigEntry<T> result = this.m_plugin.Config.Bind<T>(category, name, default_value, description);
        if (change_callback != null) {
            result.SettingChanged += change_callback;
        }
        return result;
    }

    public void early_load(DDPlugin plugin) {
        this.m_plugin = plugin;
        
        // General
        m_enabled = this.create_entry("General", "Enabled", true, "Set to false to disable this mod.", on_setting_changed);
        m_log_level = this.create_entry("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.", on_setting_changed);
        DDPlugin.set_log_level(m_log_level.Value);
        m_player_name = this.create_entry("General", "Player Name", "", "New name for the player character.  Leave blank to use default name.  Changing this value requires a game reload.", on_setting_changed);
    }

    public void late_load() {
        // NPCs
        char[] INVALID_NAME_CHARS = new char[] { '\n', '\t', '\\', '"', '\'', '[', ']' };
        m_npc_names = new Dictionary<string, ConfigEntry<string>>();
        foreach (NPCAI npc in Resources.FindObjectsOfTypeAll<NPCAI>()) {
            if (string.IsNullOrEmpty(npc.OriginalName)) {
                continue;
            }
            bool is_valid = true;
            foreach (char c in INVALID_NAME_CHARS) {
                if (npc.OriginalName.Contains(c)) {
                    is_valid = false;
                    break;
                }
            }
            if (!is_valid) {
                continue;
            }
            m_npc_names["RNPCName." + npc.OriginalName] = this.create_entry("NPCs", npc.OriginalName, "", $"New name for NPC '{npc.OriginalName}'.  Leave blank to use default name.  Changing this value requires a game reload.", on_setting_changed);
        }
    }

    public static void on_setting_changed(object sender, EventArgs e) {
		
	}
}