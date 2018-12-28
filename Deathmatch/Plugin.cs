using scp4aiur;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using System.Collections.Generic;

namespace Deathmatch
{
	[PluginDetails(
	author = "Cyanox",
	name = "Deathmatch",
	description = "A custom gamemode for SCP:SL",
	id = "cyanox.gamemode.deathmatch",
	version = "1.0.0",
	SmodMajor = 3,
	SmodMinor = 0,
	SmodRevision = 0
	)]
	public class Plugin : Smod2.Plugin
	{
		public static Plugin instance;

		public static Dictionary<string, int> pKills = new Dictionary<string, int>();

		public static bool isToggled = false;
		public static bool isRoundStarted = false;
		public static bool isDeathmatch = false;

		public override void OnEnable() { }

		public override void OnDisable() { }

		public override void Register()
		{
			instance = this;

			Timing.Init(this, Smod2.Events.Priority.Normal, false);

			AddEventHandlers(new EventHandler(), Smod2.Events.Priority.High);
			// Doing this to allow me to override other plugins potentially blocking friendly fire damage,
			// don't angrylaserboi me

			AddCommands(new[] { "deathmatch", "dm" }, new CommandHandler());

			AddConfig(new ConfigSetting("dm_start_delay", 30f, SettingType.FLOAT, true, "Time until the round starts."));
			AddConfig(new ConfigSetting("dm_grace_period", 5f, SettingType.FLOAT, true, "Time until a player gets a gun after a respawn."));
			AddConfig(new ConfigSetting("dm_respawn_time", 8f, SettingType.FLOAT, true, "Time until a player respawns."));
			AddConfig(new ConfigSetting("dm_round_length", 600, SettingType.NUMERIC, true, "Time in seconds of the round."));
			AddConfig(new ConfigSetting("dm_leaderboard_update", 10, SettingType.NUMERIC, true, "How often in seconds the leaderboard updates."));
			AddConfig(new ConfigSetting("dm_minute_announcements", new[] 
			{
				8,
				6,
				4,
				2
			}, SettingType.NUMERIC_LIST, true, "Times in minutes for CASSIE to announce how many more minutes are in the round."));
			AddConfig(new ConfigSetting("dm_allow_dropin", true, SettingType.BOOL, true, "Allow players to join the game mid round."));
			AddConfig(new ConfigSetting("dm_medkit_on_kill", true, SettingType.BOOL, true, "Give players a medkit after a kill if they don't have one."));
			AddConfig(new ConfigSetting("dm_lobby_role", 15, SettingType.NUMERIC, true, "Role ID for players to be before the fight starts."));
			AddConfig(new ConfigSetting("dm_fight_role", 14, SettingType.NUMERIC, true, "Role ID for players to fight as."));
		}
	}
}
