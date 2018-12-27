using System;
using Smod2;
using scp4aiur;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Linq;
using Object = UnityEngine.Object;
using UnityEngine;

namespace Deathmatch
{
	class EventHandler : IEventHandlerRoundStart, IEventHandlerPlayerJoin, IEventHandlerCheckRoundEnd,
		IEventHandlerPlayerHurt
	{
		private Broadcast broadcast;
		private MTFRespawn cassie;

		private const float cassieDelay = 8.2f;
		private const float roundStartTime = 30f; // change to config
		private const float gracePeriod = 5; // make config
		private const float respawnTime = 8; // make config
		private const int roundLength = 600; // make config
		private const int leaderboardUpdateTime = 10; // make config

		private int[] minuteAnnouncements = { 8, 6, 4, 2 }; // make config

		private bool allowDropIn = true; // make config

		private string BroadcastExplanation = "This is Deathmatch, a custom gamemode. If you have never played it, press [`] or [~] for more info.";

		private string ConsoleExplanation =
			$"Welcome to Deathmatch!\n" +
			$"When the round starts you will turn into a Tutorial and will be given weapons. " +
			$"Your goal is to kill as many other players as you can before time runs out! " +
			$"If you die, there will be a {respawnTime} second respawn time, you will also have a {gracePeriod} second " +
			$"grace period after you spawn. You will be told how many at every time you get" +
			$" a kill in this console. When time runs out, whoever has the most kills wins!\n" +
			$"Good Luck!";

		public void StartRound()
		{
			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
				SpawnPlayer(player, false);

			Plugin.isDeathmatch = true;

			UpdateLeaderboard();

			// start timer for checking round
		}

		private void EndRound()
		{
			Plugin.pKills = Plugin.pKills.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

			Plugin.isRoundStarted = false;
			Plugin.isDeathmatch = false;

			broadcast.CallRpcAddElement(
				$"The winner is " +
				$"{ FindPlayer(Plugin.pKills.ElementAt(0).Key).Name}" +
				$" with " +
				$"{ Plugin.pKills.ElementAt(0).Value} kills!",
				10, false);
		}

		public void SpawnPlayer(Player player, bool giveGracePeriod)
		{
			if (!Plugin.pKills.ContainsKey(player.SteamId))
				Plugin.pKills.Add(player.SteamId, 0);

			if (giveGracePeriod)
			{
				GrantGracePeriod(player);
			}
			else
			{
				player.ChangeRole(Role.NTF_LIEUTENANT, false, false);

				player.GiveItem(ItemType.E11_STANDARD_RIFLE);
				player.GiveItem(ItemType.MEDKIT);
				player.GiveItem(ItemType.FRAG_GRENADE);

				player.SetAmmo(AmmoType.DROPPED_5, 100);
				player.SetAmmo(AmmoType.DROPPED_7, 100);
				player.SetAmmo(AmmoType.DROPPED_9, 100);
			}
		}

		public void GrantGracePeriod(Player player)
		{
			player.ChangeRole(Role.CLASSD, false, false);
			player.Teleport(PluginManager.Manager.Server.Map.GetRandomSpawnPoint(Role.SCIENTIST));
			Timing.In(x =>
			{
				SpawnPlayer(player, false);
			}, gracePeriod);
		}

		public void UpdateLeaderboard()
		{
			Plugin.pKills = Plugin.pKills.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

			broadcast.CallRpcAddElement(
				$"1) {FindPlayer(Plugin.pKills.ElementAt(0).Key).Name}: {Plugin.pKills.ElementAt(0).Value} " +
				(Plugin.pKills.Count > 1 ? $"\n2) {FindPlayer(Plugin.pKills.ElementAt(1).Key).Name}: {Plugin.pKills.ElementAt(1).Value}" +
				(Plugin.pKills.Count > 2 ? $"\n3) {FindPlayer(Plugin.pKills.ElementAt(2).Key).Name}: {Plugin.pKills.ElementAt(2).Value}" : "") : ""),
				leaderboardUpdateTime, false);
			if (Plugin.isDeathmatch)
				Timing.In(x => UpdateLeaderboard(), leaderboardUpdateTime);
		}

		public Player FindPlayer(string steamid)
		{
			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
				if (player.SteamId == steamid)
					return player;
			return null;
		}

		private void AnnounceTimeCheck(int minutes, float inaccuracy = 0)
		{
			if (minutes == 0)
				return;

			string cassieLine = minuteAnnouncements.Contains(minutes) ? $"{minutes} MINUTE{(minutes == 1 ? "" : "S")} REMAINING" : "";

			if (minutes == 0)
				EndRound();
			else
				Timing.In(x => AnnounceTimeCheck(--minutes, x), 60 + inaccuracy);

			if (!string.IsNullOrWhiteSpace(cassieLine))
				cassie.CallRpcPlayCustomAnnouncement(cassieLine, false);
		}

		public void GamePrep()
		{
			Plugin.isRoundStarted = true;
			Plugin.pKills.Clear();

			foreach (Smod2.API.Door door in PluginManager.Manager.Server.Map.GetDoors()
				.Where(x => x.Name.ToUpper().Contains("CHECKPOINT")))
					door.Locked = true;

			foreach (Pickup item in Object.FindObjectsOfType<Pickup>())
				item.Delete();

			broadcast = Object.FindObjectOfType<Broadcast>();
			cassie = PlayerManager.localPlayer.GetComponent<MTFRespawn>();

			broadcast.CallRpcAddElement(BroadcastExplanation, 10, false);

			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
			{
				player.ChangeRole(Role.SCIENTIST, false);
				player.SendConsoleMessage(ConsoleExplanation);
			}
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
			if (Plugin.isToggled)
			{
				if (allowDropIn)
				{
					if (Plugin.isDeathmatch)
						SpawnPlayer(ev.Player, true);
					else
						ev.Player.ChangeRole(Role.SCIENTIST, false);
				}
			}
		}

		public void OnPlayerHurt(PlayerHurtEvent ev)
		{
			if (Plugin.isToggled && Plugin.isDeathmatch && ev.Damage >= ev.Player.GetHealth())
			{
				foreach (Smod2.API.Item item in ev.Player.GetInventory())
					item.Remove();
				if (Plugin.pKills.ContainsKey(ev.Attacker.SteamId) && ev.Player.SteamId != ev.Attacker.SteamId)
				{
					Plugin.pKills[ev.Attacker.SteamId]++;
					ev.Attacker.SendConsoleMessage($"You now have {Plugin.pKills[ev.Attacker.SteamId]} kills.");
				}

				ev.Player.SendConsoleMessage($"You will respawn in {respawnTime} second{(respawnTime != 1 ? "s" : "")}.");
				Timing.In(x =>
				{
					SpawnPlayer(ev.Player, true);
				}, respawnTime);
			}
		}

		public void OnCheckRoundEnd(CheckRoundEndEvent ev)
		{
			if (Plugin.isToggled && Plugin.isRoundStarted)
				ev.Status = ROUND_END_STATUS.ON_GOING; // SET ISROUNDSTARTED TO FALSE WHEN TIME RUNS OUT
		}
	}
}
