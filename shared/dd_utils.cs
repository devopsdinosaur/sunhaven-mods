using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx.Logging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

    public static void list_ancestors(Transform obj, Action<object> log_method) {
        List<string> strings = new List<string>();
        for (;;) {
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

    public const BindingFlags BINDING_FLAGS_ALL = (BindingFlags) 65535;

    public static string list_members(object obj) {
        List<string> lines = new List<string>();
        if (obj == null) {
            return "object is null";
        }
        lines.Add($"list_members (object type: {obj.GetType().Name})");
        foreach (FieldInfo field in obj.GetType().GetFields(BINDING_FLAGS_ALL)) {
            lines.Add($"--> field: {field.Name} ({field.FieldType.Name})");
        }
        foreach (MethodInfo method in obj.GetType().GetMethods(BINDING_FLAGS_ALL)) {
            lines.Add($"--> method: {method.Name}");
        }
        foreach (PropertyInfo property in obj.GetType().GetProperties(BINDING_FLAGS_ALL)) {
            lines.Add($"--> property: {property.Name}");
        }
        return string.Join("\n", lines);
    }

    public static FieldInfo get_field(object obj, string name) {
        return obj.GetType().GetField(name, BINDING_FLAGS_ALL);
    }

    public static object get_field_value(object obj, string name) {
        return get_field(obj, name)?.GetValue(obj);
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
    public static PropertyInfo get_property(object obj, string name) {
        return obj.GetType().GetProperty(name, BINDING_FLAGS_ALL);
    }

    public static object get_property_value(object obj, string name) {
        return get_property(obj, name)?.GetValue(obj);
    }

    public static MethodInfo get_method(object obj, string name) {
        return obj.GetType().GetMethod(name, BINDING_FLAGS_ALL);
    }

    public static object invoke_method(object obj, string name, object[] _params = null) {
        return get_method(obj, name)?.Invoke(obj, (_params == null ? new object[] { } : _params));
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
            skip_methods = new string[] {};
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
