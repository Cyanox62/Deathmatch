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
using System;

namespace Deathmatch
{
	public partial class EventHandler
	{
		private void StartRound()
		{
			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
			{
				SpawnPlayer(player, false);
				player.SetGodmode(false);
			}

			int remainder = roundLength % 60;
			Timing.In(x => AnnounceTimeCheck((roundLength - remainder) / 60, x), remainder);

			Plugin.isDeathmatch = true;

			UpdateLeaderboard();     
		}

		private void EndRound()
		{
			PluginManager.Manager.Logger.Info("", "ending round");
			Plugin.pKills = Plugin.pKills.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

			Plugin.isRoundStarted = false;
			Plugin.isDeathmatch = false;

			broadcast.CallRpcAddElement(
				$"The winner is " +
				$"{FindPlayer(Plugin.pKills.ElementAt(0).Key).Name}" +
				$" with " +
				$"{Plugin.pKills.ElementAt(0).Value} kill{(Plugin.pKills.ElementAt(0).Value > 1 ? "s" : "")}!",
				30, false);
		}

		private void SpawnPlayer(Player player, bool giveGracePeriod)
		{
			if (!Plugin.pKills.ContainsKey(player.SteamId))
				Plugin.pKills.Add(player.SteamId, 0);

			if (giveGracePeriod)
			{
				GrantGracePeriod(player);
			}
			else
			{
				if (player.TeamRole.Role != Role.SPECTATOR)
				{
					GameObject pObj = (GameObject)player.GetGameObject();
					PlyMovementSync sync = pObj.GetComponent<PlyMovementSync>();
					float rot = sync.rotation;

					player.ChangeRole(fightRole, false, false);

					Timing.In(x =>
					{
						sync.SetRotation(rot - pObj.GetComponent<PlyMovementSync>().rotation);
						player.SetCurrentItemIndex(0);
					}, 0.2f);
					// fix rotation
				}
				else
				{
					player.ChangeRole(fightRole, false, false);
				}

				player.GiveItem(ItemType.E11_STANDARD_RIFLE);
				player.GiveItem(ItemType.MEDKIT);
				player.GiveItem(ItemType.FRAG_GRENADE);

				player.SetAmmo(AmmoType.DROPPED_5, 300);
				player.SetAmmo(AmmoType.DROPPED_7, 300);
				player.SetAmmo(AmmoType.DROPPED_9, 300);
			}
		}

		private void GrantGracePeriod(Player player)
		{
			player.ChangeRole(Role.CLASSD, false, false);
			player.Teleport(PluginManager.Manager.Server.Map.GetRandomSpawnPoint(Role.FACILITY_GUARD));
			player.SetGodmode(true);
			Timing.In(x =>
			{
				player.SetGodmode(false);
				SpawnPlayer(player, false);
			}, gracePeriod);
		}

		private void UpdateLeaderboard()
		{
			foreach (Player player in Plugin.pKills.Select(x => FindPlayer(x.Key)).Where(x => x == null))
				Plugin.pKills.Remove(player.SteamId);

			if (Plugin.pKills.Count > 0)
			{
				Plugin.pKills = Plugin.pKills.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

				broadcast.CallRpcAddElement(
					$"<color=#AA9000>1) {FindPlayer(Plugin.pKills.ElementAt(0).Key).Name}: {Plugin.pKills.ElementAt(0).Value}</color>" +
					(Plugin.pKills.Count > 1 ? $"<color=#888888>\n2) {FindPlayer(Plugin.pKills.ElementAt(1).Key).Name}: {Plugin.pKills.ElementAt(1).Value}</color>" +
					(Plugin.pKills.Count > 2 ? $"<color=#AA5D00>\n3) {FindPlayer(Plugin.pKills.ElementAt(2).Key).Name}: {Plugin.pKills.ElementAt(2).Value}</color>" : "") : ""),
					Convert.ToUInt32(leaderboardUpdateTime), false);
			}
			if (Plugin.isDeathmatch)
				Timing.In(x => UpdateLeaderboard(), leaderboardUpdateTime);
		}

		private Player FindPlayer(string steamid)
		{
			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
				if (player.SteamId == steamid)
					return player;
			return null;
		}

		private void GamePrep()
		{
			Plugin.isRoundStarted = true;
			Plugin.pKills.Clear();

			foreach (Smod2.API.Door door in PluginManager.Manager.Server.Map.GetDoors()
				.Where(x => x.Name.ToUpper().Contains("CHECKPOINT") || x.Name.ToUpper().Contains("GATE")))
				door.Locked = true;

			foreach (Pickup item in Object.FindObjectsOfType<Pickup>())
				item.Delete();

			broadcast = Object.FindObjectOfType<Broadcast>();
			cassie = PlayerManager.localPlayer.GetComponent<MTFRespawn>();

			broadcast.CallRpcAddElement(BroadcastExplanation, 15, false);

			foreach (Player player in PluginManager.Manager.Server.GetPlayers())
			{
				player.ChangeRole(lobbyRole, false);
				player.SendConsoleMessage(ConsoleExplanation);
				player.SetGodmode(true);
			}
		}

		private void CleanItems()
		{
			Timing.Next(() =>
			{
				foreach (Pickup item in Object.FindObjectsOfType<Pickup>())
					item.Delete();
			});
		}

		private void AnnounceTimeCheck(int minutes, float inaccuracy = 0)
		{
			string cassieLine = minuteAnnouncements.Contains(minutes) ? $"{minutes} MINUTE{(minutes == 1 ? "" : "S")} REMAINING" : "";

			if (minutes == 0)
				EndRound();
			else
				Timing.In(x => AnnounceTimeCheck(--minutes, x), 60 + inaccuracy);

			if (!string.IsNullOrWhiteSpace(cassieLine))
				cassie.CallRpcPlayCustomAnnouncement(cassieLine, false);
		}
	}
}
