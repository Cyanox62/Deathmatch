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

			Timing.Init(this);

			AddEventHandlers(new EventHandler(), Smod2.Events.Priority.High);
			// setting to high to override mode plugins friends fire

			AddCommands(new[] { "deathmatch", "dm" }, new CommandHandler());

			AddConfig(new ConfigSetting("dm_start_delay", 30f, SettingType.FLOAT, true, "Time until the round starts."));
		}
	}
}
