using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices.WindowsRuntime;

public abstract class DDPlugin : BaseUnityPlugin {
    public Dictionary<string, string> m_plugin_info = null;
    protected static ManualLogSource logger;
    public enum LogLevel {
        None,
        Error,
        Warn,
        Info,
        Debug
    }
    private static readonly Dictionary<string, LogLevel> LOG_LEVEL_STRING_KEY_MAP = new Dictionary<string, LogLevel>() {
        {"none", LogLevel.None},
        {"error", LogLevel.Error},
        {"warn", LogLevel.Warn},
        {"info", LogLevel.Info},
        {"debug", LogLevel.Debug},
    };
    protected static LogLevel m_log_level = LogLevel.Info;

    public static LogLevel set_log_level(LogLevel level) {
        _info_log($"Setting log level to {level.ToString().ToUpper()}.");
        return (m_log_level = level);
    }

    public static LogLevel set_log_level(string level_string) {
        if (LOG_LEVEL_STRING_KEY_MAP.TryGetValue(level_string.ToLower(), out LogLevel value)) {
            return set_log_level(value);
        }
        return set_log_level(LogLevel.None);
    }

    public static void _debug_log(object text) {
        if (m_log_level >= LogLevel.Debug) {
            logger.LogInfo("[DEBUG] " + text);
        }
    }

    public static void _info_log(object text) {
        if (m_log_level >= LogLevel.Info) {
            logger.LogInfo(text);
        }
    }

    public static void _warn_log(object text) {
        if (m_log_level >= LogLevel.Warn) {
            logger.LogWarning(text);
        }
    }

    public static void _error_log(object text) {
        if (m_log_level >= LogLevel.Error) {
            logger.LogError(text);
        }
    }

    public string get_nexus_dir() {
        try {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (string file in new string[] { "nexus", this.m_plugin_info["guid"] }) {
                if (!Directory.Exists(path = Path.Combine(path, file))) {
                    return null;
                }
            }
            return path;
        } catch (Exception e) {
            _error_log("** DDPlugin.get_nexus_dir ERROR - " + e);
        }
        return null;
    }

    protected void create_nexus_page() {
        if (m_plugin_info == null) {
            logger.LogWarning("* create_nexus_page WARNING - m_plugin_info dict must be initialized before calling this method.");
            return;
        }
        string nexus_dir = this.get_nexus_dir();
        if (nexus_dir == null) {
            return;
        }
        string template_path = Path.Combine(nexus_dir, "template.txt");
        string output_path = Path.Combine(nexus_dir, "generated.txt");
        if (!File.Exists(template_path)) {
            return;
        }
        string template_data = File.ReadAllText(template_path);
        Dictionary<string, List<string[]>> categories = new Dictionary<string, List<string[]>>();
        List<string> hotkey_lines = new List<string>();
        foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> kvp in this.Config.ToArray()) {
            if (!categories.Keys.Contains(kvp.Key.Section)) {
                categories[kvp.Key.Section] = new List<string[]>();
            }
            categories[kvp.Key.Section].Add(new string[] {
                kvp.Key.Key,
                $"[*][b][i]{kvp.Key.Key}[/i][/b] - {kvp.Value.Description.Description}"
            });
            if (kvp.Key.Section != "Hotkeys") {
                continue;
            }
            hotkey_lines.Add($"[*][b][i]{(kvp.Key.Key.EndsWith("Modifier") ? "" : "[Modifier_Key] + ")}{kvp.Value.DefaultValue.ToString().Replace(",", " or ")}[/i][/b] - {kvp.Key.Key.Replace("Hotkey - ", "").Replace(" Hotkey", "")}");
        }
        this.m_plugin_info["hotkeys"] = (hotkey_lines.Count > 0 ? $"\n[b][u][size=4]Hotkeys[/size][/u][/b]\n\n[list]\n{string.Join("\n", hotkey_lines)}\n[/list]" : "");
        List<string> ordered_categories = new List<string>(categories.Keys);
        ordered_categories.Sort();
        foreach (List<string[]> items in categories.Values) {
            items.Sort((x, y) => x[0].CompareTo(y[0]));
        }
        string lines = "";
        foreach (string category in ordered_categories) {
            lines += $"[b][size=3]{category}[/size][/b]\n\n[list]\n";
            foreach (string[] option in categories[category]) {
                lines += option[1] + "\n";
            }
            lines += "[/list]\n";
        }
        this.m_plugin_info["config_options"] = lines;
        this.m_plugin_info["extra_details"] = (this.m_plugin_info.ContainsKey("extra_details") ? $"\n{this.m_plugin_info["extra_details"]}\n" : "");
        foreach (KeyValuePair<string, string> kvp in this.m_plugin_info) {
            template_data = template_data.Replace("[[" + kvp.Key + "]]", kvp.Value);
        }
        File.WriteAllText(output_path, template_data);
    }
}

public static class UnityUtils {

    public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent, Action<object> log_method) {
        Transform child;
        string indent_string = "";
        for (int counter = 0; counter < indent; counter++) {
            indent_string += " => ";
        }
        for (int index = 0; index < parent.childCount; index++) {
            child = parent.GetChild(index);
            log_method(indent_string + child.gameObject.name);
            if (callback != null) {
                if (callback(child) == false) {
                    return false;
                }
            }
            list_descendants(child, callback, indent + 1, log_method);
        }
        return true;
    }

    public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
        Transform child;
        for (int index = 0; index < parent.childCount; index++) {
            child = parent.GetChild(index);
            if (callback != null) {
                if (callback(child) == false) {
                    return false;
                }
            }
            enum_descendants(child, callback);
        }
        return true;
    }

    public static Transform find_first_descendant(Transform parent, string name) {
        Transform match = null;
        bool callback(Transform transform) {
            if (transform.name == name) {
                match = transform;
                return false;
            }
            return true;
        }
        enum_descendants(parent, callback);
        return match;
    }

    public static Transform find_by_path(string path) {
        Transform __find_by_path__(Scene scene, Transform parent, string[] path_parts, int part_index) {
            Transform result = null;
            if (parent == null) {
                foreach (GameObject obj in scene.GetRootGameObjects()) {
                    if ((result = __find_by_path__(scene, obj.transform, path_parts, 0)) != null) {
                        return result;
                    }
                }
            } else {
                if (parent.name == path_parts[part_index]) {
                    if (part_index == path_parts.Length - 1) {
                        return parent;
                    }
                    foreach (Transform child in parent) {
                        if ((result = __find_by_path__(scene, child, path_parts, part_index + 1)) != null) {
                            return result;
                        }
                    }
                }
            }
            return null;
        }
        Transform result = null;
        string[] parts = path.Replace("\\", "/").Trim('/').Trim().Split('/');
        Scene active_scene = SceneManager.GetActiveScene();
        if ((result = __find_by_path__(active_scene, null, parts, -1)) != null) {
            return result;
        }
        for (int scene_index = 0; scene_index < SceneManager.sceneCount; scene_index++) {
            Scene scene = SceneManager.GetSceneAt(scene_index);
            if (scene != active_scene && (result = __find_by_path__(scene, null, parts, -1)) != null) {
                return result;
            }
        }
        return null;
    }

    public static void list_ancestors(Transform obj, Action<object> log_method) {
        List<string> strings = new List<string>();
        for (; ; ) {
            if (obj == null) {
                break;
            }
            strings.Add((!string.IsNullOrEmpty(obj.name) ? obj.name : "<unnamed>"));
            obj = obj.parent;
        }
        log_method(string.Join(" => ", strings));
    }

    public static void list_component_types(Transform obj, Action<object> log_method) {
        foreach (Component component in obj.GetComponents<Component>()) {
            log_method(component.GetType().ToString());
        }
    }

    public static void json_dump(Transform obj, string path) {
        const int TAB_SIZE = 4;
        List<string> lines = new List<string>();
        string tab_string = "";
        int tab_count = 0;

        string tabbed_text(string text) {
            string space = "";
            for (int counter = 0; counter < tab_count; counter++) {
                space += tab_string;
            }
            return space + text;
        }

        void add_line(string text) {
            lines.Add(tabbed_text(text));
        }

        void add_obj(Transform transform, bool add_trailing_comma) {
            add_line("{");
            tab_count++;
            add_line($"\"name\": \"{transform.name}\",");
            add_line("\"components\": [");
            tab_count++;
            lines.Add(string.Join(",\n", transform.GetComponents<Component>().Select(component =>
                tabbed_text($"\"{component.GetType().ToString()}\"")))
            );
            tab_count--;
            add_line("],");
            add_line("\"children\": [");
            tab_count++;
            for (int counter = 0; counter < transform.childCount; counter++) {
                add_obj(transform.GetChild(counter), counter < transform.childCount - 1);
            }
            tab_count--;
            add_line("]");
            tab_count--;
            add_line("}" + (add_trailing_comma ? "," : ""));
        }

        for (int counter = 0; counter < TAB_SIZE; counter++) {
            tab_string += " ";
        }
        add_obj(obj, false);
        StreamWriter f = File.CreateText(path);
        f.Write(string.Join("\n", lines));
        f.Close();
    }

    public static void list_stack(Action<object> log_method) {
        List<string> strings = new List<string>();
        int index = 0;
        foreach (StackFrame frame in new StackTrace(1, true).GetFrames()) {
            strings.Add($"[{index++}] method: {frame.GetMethod().Name}, file: {frame.GetFileName()}, line: {frame.GetFileLineNumber()}");
        }
        log_method.Invoke(string.Join("\n", strings));
    }
}

public static class ReflectionUtils {

    public const BindingFlags BINDING_FLAGS_ALL = BindingFlags.Instance |
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod | BindingFlags.CreateInstance;

    public static string list_members(object obj, Type type = null) {
        if (type == null) {
            type = obj.GetType();
        }
        List<string> lines = new List<string>();
        if (obj == null) {
            return "object is null";
        }
        lines.Add($"list_members (object type: {type.Name})");
        foreach (FieldInfo field in obj.GetType().GetFields(BINDING_FLAGS_ALL)) {
            lines.Add($"--> field: {field.Name} ({field.FieldType.Name})");
        }
        foreach (MethodInfo method in type.GetMethods(BINDING_FLAGS_ALL)) {
            lines.Add($"--> method: {method.Name}");
        }
        foreach (PropertyInfo property in type.GetProperties(BINDING_FLAGS_ALL)) {
            lines.Add($"--> property: {property.Name}");
        }
        return string.Join("\n", lines);
    }

    public static FieldInfo get_field(object obj, string name, Type type = null) {
        return (type == null ? obj.GetType() : type).GetField(name, BINDING_FLAGS_ALL);
    }

    public static object get_field_value(object obj, string name, Type type = null) {
        return get_field(obj, name, type)?.GetValue(obj);
    }
    /*
    public static Il2CppSystem.Reflection.FieldInfo il2cpp_get_field(Il2CppSystem.Object obj, string name) {
        return obj.GetIl2CppType().GetField(name, (Il2CppSystem.Reflection.BindingFlags) BINDING_FLAGS_ALL);
    }

    public static T il2cpp_get_field_value<T>(Il2CppSystem.Object obj, string name) {
        return (T) Marshal.PtrToStructure(
            Il2CppInterop.Runtime.IL2CPP.il2cpp_object_unbox(il2cpp_get_field(obj, name).GetValue(obj).Pointer),
            (typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T))
        );
    }
    */
    public static PropertyInfo get_property(object obj, string name, Type type = null) {
        return (type == null ? obj.GetType() : type).GetProperty(name, BINDING_FLAGS_ALL);
    }

    public static object get_property_value(object obj, string name, Type type = null) {
        return get_property(obj, name, type)?.GetValue(obj);
    }

    public static MethodInfo get_method(object obj, string name, Type type = null) {
        return (type == null ? obj.GetType() : type).GetMethod(name, BINDING_FLAGS_ALL);
    }

    public static object invoke_method(object obj, string name, object[] _params = null, Type type = null) {
        return get_method(obj, name, type)?.Invoke(obj, (_params == null ? new object[] { } : _params));
    }

    public class EnumerateListEntriesCallbackParams {
        private object list;
        private int index;
        public object Index {
            get {
                return this.index;
            }
        }
        public object Value {
            get {
                return this.get_Item?.Invoke(this.list, new object[] { this.index });
            }
            set {
                this.set_Item?.Invoke(this.list, new object[] { this.index, value });
            }
        }
        private MethodInfo get_Item;
        private MethodInfo set_Item;

        public EnumerateListEntriesCallbackParams(object list, int index, MethodInfo get_Item, MethodInfo set_Item) {
            this.list = list;
            this.index = index;
            this.get_Item = get_Item;
            this.set_Item = set_Item;
        }
    }

    public static void enumerate_list_entries(object list, Func<EnumerateListEntriesCallbackParams, bool> callback) {
        MethodInfo get_Item = get_method(list, "get_Item");
        MethodInfo set_Item = get_method(list, "set_Item");
        for (int index = 0; index < (int) get_field_value(list, "_size"); index++) {
            EnumerateListEntriesCallbackParams callback_params = new EnumerateListEntriesCallbackParams(list, index, get_Item, set_Item);
            if (!callback.Invoke(callback_params)) {
                break;
            }
        }
    }

    public class EnumerateDictEntriesCallbackParams {
        private object dict;
        private object key;
        public object Key {
            get {
                return this.key;
            }
        }
        public object Value {
            get {
                return this.get_Item?.Invoke(this.dict, new object[] { this.Key });
            }
            set {
                this.set_Item?.Invoke(this.dict, new object[] { this.Key, value });
            }
        }
        private MethodInfo get_Item;
        private MethodInfo set_Item;

        public EnumerateDictEntriesCallbackParams(object dict, object key, MethodInfo get_Item, MethodInfo set_Item) {
            this.dict = dict;
            this.key = key;
            this.get_Item = get_Item;
            this.set_Item = set_Item;
        }
    }

    public static void enumerate_dict_entries(object dict, Func<EnumerateDictEntriesCallbackParams, bool> callback) {
        MethodInfo get_Item = get_method(dict, "get_Item");
        MethodInfo set_Item = get_method(dict, "set_Item");
        object entries = get_field_value(dict, "entries");
        if (entries == null) {
            entries = get_field_value(dict, "_entries");
            if (entries == null) {
                return;
            }
        }
        foreach (object entry in (Array) entries) {
            EnumerateDictEntriesCallbackParams callback_params = new EnumerateDictEntriesCallbackParams(dict, get_field_value(entry, "key"), get_Item, set_Item);
            if (callback_params.Key == null) {
                continue;
            }
            if (!callback.Invoke(callback_params)) {
                break;
            }
        }
    }

    public static void generate_trace_patcher(
        Type type,
        string path,
        string additional_usings = "",
        string[] skip_methods = null,
        bool echo_skip_messages = false
    ) {
        if (skip_methods == null) {
            skip_methods = new string[] { };
        }
        string type_name = type.Name;
        List<string> lines = new List<string>();
        lines.Add($@"using HarmonyLib;
using System;
{additional_usings}

public class TracePatcher_{type_name} {{
    
    public class TracerParams {{
        public int method_id;
        public string method_name;
        
    }}

    public static Action<TracerParams> callback = null;
");
        List<string> field_accessor_names_list = new List<string>();
        foreach (FieldInfo field in type.GetFields(BINDING_FLAGS_ALL)) {
            string field_name = (field.Name.StartsWith("NativeFieldInfoPtr_") ? field.Name.Substring(19) : field.Name);
            field_accessor_names_list.Add("get_" + field_name);
            field_accessor_names_list.Add("set_" + field_name);
        }
        string[] field_accessor_names = field_accessor_names_list.ToArray();
        int counter = 0;
        foreach (MethodInfo method in type.GetMethods(BINDING_FLAGS_ALL)) {
            string skip_reason = null;
            if (skip_methods.Contains(method.Name)) {
                skip_reason = "Specified in 'skip_methods' param";
            } else if (field_accessor_names.Contains(method.Name)) {
                skip_reason = "Field accessor";
            }
            if (skip_reason != null) {
                if (echo_skip_messages) {
                    lines.Add($"    // Skipping {method.Name} ({skip_reason}).");
                }
                continue;
            }
            List<string> param_strings = method.GetParameters().Select(param => $"typeof({param.ParameterType.Name})").ToList();
            lines.Add($@"
    [HarmonyPatch(typeof({type_name}), ""{method.Name}"", new Type[] {{{string.Join(", ", param_strings)}}})]
    class HarmonyPatch_{method.Name} {{
        private static void Postfix() {{
            if (callback != null) {{
                callback(new TracerParams() {{
                    method_id = {counter},
                    method_name = ""{method.Name}""
                }});
            }}
        }}
    }}");
            counter++;
        }
        lines.Add("}");
        StreamWriter f = File.CreateText(path);
        f.Write(string.Join("\n", lines));
        f.Close();
    }
}

public class PluginUpdater : MonoBehaviour {

    private static PluginUpdater m_instance = null;
    public static PluginUpdater Instance {
        get {
            return m_instance;
        }
    }
    private class UpdateInfo {
        public string name;
        public float frequency;
        public float elapsed;
        public Action action;
    }
    private UpdateInfo[] m_actions = new UpdateInfo[0];
    private ManualLogSource m_logger;
    private bool m_is_dirty = false;

    public static PluginUpdater create(GameObject parent, ManualLogSource logger) {
        if (m_instance != null) {
            return m_instance;
        }
        m_instance = parent.AddComponent<PluginUpdater>();
        m_instance.m_logger = logger;
        m_instance.m_is_dirty = false;
        return m_instance;
    }
    /*
    public static PluginUpdater create(BasePlugin parent, ManualLogSource logger) {
        if (m_instance != null) {
            return m_instance;
        }
        m_instance = parent.AddComponent<PluginUpdater>();
        m_instance.m_logger = logger;
        m_instance.m_is_dirty = false;
        return m_instance;
    }
    */
    public void register(string name, float frequency, Action action) {
        UpdateInfo[] new_actions = new UpdateInfo[m_actions.Length + 1];
        for (int index = 0; index < this.m_actions.Length; index++) {
            new_actions[index] = this.m_actions[index];
        }
        new_actions[m_actions.Length] = new UpdateInfo {
            name = name,
            frequency = frequency,
            elapsed = frequency,
            action = action
        };
        this.m_actions = new_actions;
        m_is_dirty = true;
        DDPlugin._debug_log(this.m_actions.Length);
    }

    public void unregister(string name) {
        UpdateInfo[] new_actions = new UpdateInfo[m_actions.Length - 1];
        bool found = false;
        int index = 0;
        foreach (UpdateInfo info in this.m_actions) {
            if (info.name == name) {
                found = true;
            } else {
                new_actions[index++] = info;
            }
        }
        if (!found) {
            return;
        }
        this.m_actions = new_actions;
        this.m_is_dirty = true;
    }

    public void trigger(string name) {
        foreach (UpdateInfo info in this.m_actions) {
            if (info.name == name) {
                info.elapsed = info.frequency;
                return;
            }
        }
    }

    public void Update() {
        foreach (UpdateInfo info in this.m_actions) {
            if (this.m_is_dirty) {
                this.m_is_dirty = false;
                return;
            }
            if ((info.elapsed += Time.deltaTime) >= info.frequency) {
                info.elapsed = 0f;
                try {
                    info.action();
                } catch (Exception e) {
                    this.m_logger.LogError((object) $"PluginUpdater.Update.{info.name} Exception - {e.ToString()}");
                }
            }
        }
    }
}
