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
    public static ConfigEntry<string> m_subdir;
    public static ConfigEntry<string> m_default_username;
    public static ConfigEntry<string> m_hotkey_modifier;
    public static ConfigEntry<string> m_hotkey_reload;
    public static ConfigEntry<string> m_force_outfit;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");
        m_subdir = this.m_plugin.Config.Bind<string>("General", "Subfolder", "self_portrait", "Subfolder under 'plugins' in which per-user self portrait folders will be located (default: 'self_portrait').");
        m_default_username = this.m_plugin.Config.Bind<string>("General", "Default Username", "default", "Fallback self portrait directory to use if there is none for current user (default: default).");
        m_hotkey_modifier = this.m_plugin.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_reload = this.m_plugin.Config.Bind<string>("General", "Reload Hotkey", "F2", "Comma-separated list of Unity Keycodes, any of which (in combination with modifier key [if not blank]) will reload portrait images.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_force_outfit = this.m_plugin.Config.Bind<string>("General", "Force Outfit", "", "Specify one of (Summer, Fall, Winter, Wedding, or Swimsuit) to override the game logic for portrait outfit selection for self and NPC (set to empty or invalid string to disable this setting).");
    }
}