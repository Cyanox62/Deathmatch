using System.Collections.Generic;
using Smod2;
using scp4aiur;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Linq;
using Object = UnityEngine.Object;
using UnityEngine;
using Smod2.EventSystem.Events;

namespace Deathmatch
{
	public partial class EventHandler : IEventHandlerRoundStart, IEventHandlerPlayerJoin, IEventHandlerCheckRoundEnd,
		IEventHandlerPlayerHurt, IEventHandlerPlayerDie, IEventHandlerTeamRespawn
	{
		private Broadcast broadcast;
		private MTFRespawn cassie;

		private const float cassieDelay = 7.6f;
		private const float roundStartTime = 30f; // change to config
		private const float gracePeriod = 5; // make config
		private const float respawnTime = 8; // make config
		private const int roundLength = 600; // make config
		private const int leaderboardUpdateTime = 10; // make config

		private int[] minuteAnnouncements = { 8, 6, 4, 2 }; // make config

		private bool allowDropIn = true; // make config

		private Player curAttacker = null;

		private List<string> pBlacklist = new List<string>();

		private string BroadcastExplanation = "This is Deathmatch, a custom gamemode. If you have never played it, press [`] or [~] for more info.";

		private string ConsoleExplanation =
			$"Welcome to Deathmatch!\n" +
			$"When the round starts you will turn into a Tutorial and will be given weapons. " +
			$"Your goal is to kill as many other players as you can before time runs out! " +
			$"If you die, there will be a {respawnTime} second respawn time, you will also have a {gracePeriod} second " +
			$"grace period after you spawn. You will be told how many at every time you get" +
			$" a kill in this console. When time runs out, whoever has the most kills wins!\n" +
			$"Good Luck!";

		public void OnRoundStart(RoundStartEvent ev)
		{
			if (!Plugin.isToggled)
				return;

			GamePrep();

			Timing.In(x =>
			{
				cassie.CallRpcPlayCustomAnnouncement("ATTACK COMMENCING IN 3 . 2 . 1", false);

				Timing.In(y =>
				{
					StartRound();
				}, cassieDelay);
			}, roundStartTime - cassieDelay);
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (Plugin.isToggled)
			{
				if (allowDropIn)
				{
					if (Plugin.isDeathmatch)
						SpawnPlayer(ev.Player, true);
					else
						ev.Player.ChangeRole(Role.FACILITY_GUARD, false);
				}
			}
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (Plugin.isToggled && Plugin.isDeathmatch)
			{
				CleanItems();

				if (Plugin.pKills.ContainsKey(ev.Killer.SteamId) && ev.Player.SteamId != curAttacker.SteamId)
				{
					Plugin.pKills[curAttacker.SteamId]++;
					curAttacker.SendConsoleMessage($"You now have {Plugin.pKills[ev.Killer.SteamId]} kills.");
				}

				ev.Player.SendConsoleMessage($"You will respawn in {respawnTime} second{(respawnTime != 1 ? "s" : "")}.");
				Timing.In(x =>
				{
					SpawnPlayer(ev.Player, true);
				}, respawnTime);
			}
		}

		public void OnPlayerHurt(PlayerHurtEvent ev)
		{
			if (Plugin.isToggled && Plugin.isDeathmatch)
			{
				// ensure other plugins dont cancel out tutorial damage
				if (pBlacklist.Contains(ev.Player.SteamId))
					return;
				pBlacklist.Add(ev.Player.SteamId);
				ev.Player.SetHealth((int)(ev.Player.GetHealth() - ev.Damage));
				curAttacker = ev.Attacker;

				Timing.Next(() =>
				{
					pBlacklist.Remove(ev.Player.SteamId);
				});
			}
		}

		public void OnTeamRespawn(TeamRespawnEvent ev)
		{
			if (Plugin.isToggled)
			{
				ev.SpawnChaos = true;
				ev.PlayerList.Clear();
			}
		}

		public void OnCheckRoundEnd(CheckRoundEndEvent ev)
		{
			if (Plugin.isToggled && Plugin.isRoundStarted)
				ev.Status = ROUND_END_STATUS.ON_GOING; // SET ISROUNDSTARTED TO FALSE WHEN TIME RUNS OUT
		}
	}
}
