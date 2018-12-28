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
		IEventHandlerPlayerHurt, IEventHandlerPlayerDie, IEventHandlerTeamRespawn, IEventHandlerWaitingForPlayers
	{
		private Broadcast broadcast;
		private MTFRespawn cassie;

		private const float cassieDelay = 7.6f;

		private float roundStartTime;
		private float gracePeriod;
		private float respawnTime;
		private int roundLength;
		private int leaderboardUpdateTime;
		private int[] minuteAnnouncements;
		private bool allowDropIn;
		private bool giveMedkitOnKill;
		private Role lobbyRole;
		private Role fightRole;

		private Player curAttacker = null;

		private List<string> pBlacklist = new List<string>();

		private string BroadcastExplanation = "Welcome to Deathmatch, a custom gamemode. If you have never played it, press [`] or [~] for more info.";

		private string ConsoleExplanation =
			$"Welcome to Deathmatch!\n" +
			$"When the round starts you will turn into a different role and will be given weapons. " +
			$"Your goal is to kill as many other players as you can before time runs out! " +
			$"If you die, there will be a short respawn time, you will also have a short " +
			$"grace period after you spawn. You will be told how many at every time you get" +
			$" a kill in this console. When time runs out, whoever has the most kills wins!\n" +
			$"Good Luck!";

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			roundStartTime = Plugin.instance.GetConfigFloat("dm_start_delay");
			gracePeriod = Plugin.instance.GetConfigFloat("dm_grace_period");
			respawnTime = Plugin.instance.GetConfigFloat("dm_respawn_time");
			roundLength = Plugin.instance.GetConfigInt("dm_round_length");
			leaderboardUpdateTime = Plugin.instance.GetConfigInt("dm_leaderboard_update");
			minuteAnnouncements = Plugin.instance.GetConfigIntList("dm_minute_announcements");
			allowDropIn = Plugin.instance.GetConfigBool("dm_allow_dropin");
			giveMedkitOnKill = Plugin.instance.GetConfigBool("dm_medkit_on_kill");
			lobbyRole = (Role)Plugin.instance.GetConfigInt("dm_lobby_role");
			fightRole = (Role)Plugin.instance.GetConfigInt("dm_fight_role");
		}

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
			if (Plugin.isToggled && allowDropIn && Plugin.isRoundStarted)
			{
				if (Plugin.isDeathmatch)
					SpawnPlayer(ev.Player, true);
				else
					ev.Player.ChangeRole(lobbyRole, false);
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

					if (giveMedkitOnKill && !curAttacker.GetInventory().Any(x => x.ItemType == ItemType.MEDKIT))
						curAttacker.GiveItem(ItemType.MEDKIT);
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
				// ensure other plugins dont cancel out class damage
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
