using BepInEx.Configuration;
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

    public class ConfigDef<T> {
        public string category;
        public string key;
        public string description;
        public T default_value;
        public ConfigEntry<T> config;
    }

    public enum ConfigId {
        ID_SILENCE_SKILL_NODE_ROLLOVER
    };

    public static Dictionary<ConfigId, ConfigDef<bool>> m_bool_configs = new Dictionary<ConfigId, ConfigDef<bool>>() {
        {ConfigId.ID_SILENCE_SKILL_NODE_ROLLOVER, new ConfigDef<bool>() {
            category = "Silence",
            key = "Silence - Skill Node Rollover",
            default_value = false,
            description = "Silence the dings when moving cursor over skill tree nodes"
        }},
    };

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");
        foreach (KeyValuePair<ConfigId, ConfigDef<bool>> item in m_bool_configs) {
            item.Value.config = this.m_plugin.Config.Bind<bool>(item.Value.category, item.Value.key, item.Value.default_value, item.Value.description);
        }
    }
}