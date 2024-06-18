using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;

[BepInPlugin("devopsdinosaur.sunhaven.action_speed", "Action Speed", "0.0.2")]
public class ActionSpeedPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.action_speed");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_tool_speed;
	private static ConfigEntry<float> m_sword_speed;
	private static ConfigEntry<float> m_crossbow_speed;
	private static ConfigEntry<float> m_spell_speed;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_tool_speed = this.Config.Bind<float>("General", "Tool Speed", 2f, "Action speed for tools (float, 1 = game default, all perk modifiers will be applied in addition to this value)");
			m_sword_speed = this.Config.Bind<float>("General", "Sword Speed", 1f, "Action speed for swords (float, 1 = game default, all perk modifiers will be applied in addition to this value)");
			m_crossbow_speed = this.Config.Bind<float>("General", "Crossbow Speed", 1f, "Action speed for crossbows (float, 1 = game default, all perk modifiers will be applied in addition to this value)");
			m_spell_speed = this.Config.Bind<float>("General", "Spell Speed", 1f, "Action speed for spells (float, 1 = game default, all perk modifiers will be applied in addition to this value)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.action_speed v0.0.2" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Weapon), "AttackSpeed")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(
			Player ___player, 
			WeaponType ____weaponType, 
			float ____frameRate,
			bool ____isMetalTool,
			ref float __result
		) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				if (!((bool) ___player)) {
					return false;
				}
				switch (____weaponType)
				{
				case WeaponType.Sword:
					__result = m_sword_speed.Value * ____frameRate / 12f;
					break;
				case WeaponType.Crossbow:
					__result = (m_crossbow_speed.Value + (GameSave.CurrentCharacter.race == (int) Race.Elf ? 0.1f : 0f)) * ____frameRate / 12f;
					break;
				case WeaponType.Spell:
					__result = m_spell_speed.Value * ____frameRate / 12f;
					break;
				default:
					__result = m_tool_speed.Value * ____frameRate / 12f * 
						((____isMetalTool && GameSave.Mining.GetNode("Mining2b")) ? 
							(1.01f + 0.03f * (float) GameSave.Mining.GetNodeAmount("Mining2b")) : 
							1f
						);
					break;
				}
				if (GameSave.Combat.GetNode("Combat1b")) {
					__result += 0.04f;
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_SkillStats_GetStat.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}