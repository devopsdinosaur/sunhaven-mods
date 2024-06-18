using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;
using System.Reflection;
using UnityEngine.Events;

[BepInPlugin("devopsdinosaur.sunhaven.consolidated_crafting", "Consolidated Crafting", "0.0.3")]
public class ConsolidatedCraftingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.consolidated_crafting");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static List<Recipe> m_all_recipes = null;
	private static Dictionary<string, RecipeList> m_table_recipes = new Dictionary<string, RecipeList>();
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.consolidated_crafting v0.0.3" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
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
	
	class RecipeListSelector : MonoBehaviour {
		
		private TMP_Dropdown m_dropdown = null;
		private RecipeList m_recipe_list;
		
		public void initialize(TMP_Dropdown dropdown, RecipeList recipe_list) {
			this.m_dropdown = dropdown;
			this.m_recipe_list = recipe_list;
		}

		public void refresh_dropdown() {
			this.m_dropdown.ClearOptions();
			int index = 0;
			int selected_index = -1;
			List<string> sorted_names = new List<string>(m_table_recipes.Keys);
			sorted_names.Sort();
			foreach (string name in sorted_names) {
				this.m_dropdown.options.Add(new TMP_Dropdown.OptionData(name));
				if (m_table_recipes[name] == this.m_recipe_list) {
					selected_index = index;
				}
				index++;
			}
			this.m_dropdown.value = selected_index;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "OpenUI")]
	class HarmonyPatch_CraftingTable_OpenUI {

		private static bool Prefix(GameObject ___ui) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				___ui.GetComponent<RecipeListSelector>().refresh_dropdown();
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_OpenUI_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "Interact")]
	class HarmonyPatch_CraftingTable_Interact {

        private static List<int> m_jam_ids = null;

        private static bool Prefix(CraftingTable __instance, GameObject ___ui, RecipeList ___recipeList, List<Recipe> ____craftingRecipes) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				if (m_jam_ids == null) {
					m_jam_ids = new List<int>();
					foreach (FieldInfo field_info in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static)) {
						if (field_info.IsLiteral && !field_info.IsInitOnly && field_info.Name.EndsWith("Jam")) {
							m_jam_ids.Add((int) field_info.GetRawConstantValue());
						}
					}
				}
                if (m_all_recipes == null) {
					m_all_recipes = new List<Recipe>(Resources.FindObjectsOfTypeAll<Recipe>());
					foreach (RecipeList recipes in Resources.FindObjectsOfTypeAll<RecipeList>()) {
						m_table_recipes[recipes.name.Replace("RecipeList_", "").Replace('_', ' ').Replace("RecipeList", "").Trim()] = recipes;
					}
                    if (!m_table_recipes.ContainsKey("Jam Maker")) {
						// jam maker has no RecipeList, for some reason
						RecipeList recipes = new RecipeList();
						recipes.name = "Jam_Maker";
						foreach (Recipe recipe in m_all_recipes) {
							if (m_jam_ids.Contains(recipe.output2.id)) {
								recipes.craftingRecipes.Add(recipe);
							}
						}
						m_table_recipes["Jam Maker"] = recipes;
					}
				}
                if (___recipeList == null) {
					if (____craftingRecipes.Count >= 1 && m_jam_ids.Contains(____craftingRecipes[0].output.item.id)) {
						___recipeList = m_table_recipes["Jam Maker"];
					}
				}
				Transform adjacent_transform = null;
				RectTransform adjacent_rect = null;
				
				bool find_high_to_low_price_toggle(Transform transform) {
					if (transform.name == "HighToLowPriceToggle") {
						adjacent_transform = transform;
						adjacent_rect = transform.GetComponent<RectTransform>();
						return false;
					}
					return true;
				}
				
				enum_descendants(___ui.transform, find_high_to_low_price_toggle);
				if (adjacent_rect == null) {
					logger.LogError("** HarmonyPatch_CraftingTable_Awake ERROR - unable to locate 'HighToLowPriceToggle' object; cannot create dropdown.");
					return true;
				}
				foreach (TMP_Dropdown tmp in Resources.FindObjectsOfTypeAll<TMP_Dropdown>()) {
					logger.LogInfo($"{tmp.name}, {tmp.transform.parent.name}");
				}
				//TMP_Dropdown dropdown = GameObject.Instantiate<TMP_Dropdown>(Resources.FindObjectsOfTypeAll<TMP_Dropdown>()[0], adjacent_transform.parent);
                TMP_Dropdown dropdown = GameObject.Instantiate<TMP_Dropdown>(Resources.FindObjectsOfTypeAll<TMP_Dropdown>()[2], adjacent_transform.parent);
                RectTransform dropdown_rect = dropdown.GetComponent<RectTransform>();
				dropdown_rect.localPosition = adjacent_rect.localPosition + Vector3.down * (adjacent_rect.rect.height / 2 + 20f);
                dropdown.name = "ConsolidatedCraftingPlugin_Choose_Recipe_List_Dropdown";
				dropdown.onValueChanged.AddListener(new UnityAction<int>(delegate {
					try {
						FieldInfo field_info = __instance.GetType().GetField("_craftingRecipes", BindingFlags.Instance | BindingFlags.NonPublic);
						if (field_info == null) {
							logger.LogError("** CraftingTable.TableTypeDropdown.onValueChanged ERROR - unable to access '_craftingRecipes' member and cannot set recipes.");
							return;
						}
						field_info.SetValue(__instance, m_table_recipes[dropdown.options[dropdown.value].text].craftingRecipes);
						__instance.Initialize();
					} catch (Exception e) {
						logger.LogError("** CraftingTable_RecipeListSelector_onValueChanged ERROR - " + e);
					}
				}));
				___ui.AddComponent<RecipeListSelector>().initialize(dropdown, ___recipeList);
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_Interact_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}