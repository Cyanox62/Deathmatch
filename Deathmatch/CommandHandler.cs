using System;
using Smod2.Commands;

namespace Deathmatch
{
	class CommandHandler : ICommandHandler
	{
		public string GetCommandDescription()
		{
			return "Enables the Deathmatch gamemode.";
		}

		public string GetUsage()
		{
			return "DM / DEATHMATCH";
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			Plugin.isToggled = !Plugin.isToggled;
			return new[] { $"Toggled Deathmatch {(Plugin.isToggled ? "on" : "off")}." };
		}
	}
}
