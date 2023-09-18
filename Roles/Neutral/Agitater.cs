﻿using Hazel;
using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public static class Agitater
{
    private static readonly int Id = 12420;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem BombExplodeCooldown;
    public static OptionItem PassCooldown;
    public static OptionItem AgitaterCanGetBombed;
    public static OptionItem AgiTaterBombCooldown;
    public static OptionItem AgitaterAutoReportBait;

    public static byte CurrentBombedPlayer = byte.MaxValue;
    public static byte LastBombedPlayer = byte.MaxValue;
    public static bool AgitaterHasBombed = false;
    public static long CurrentBombedPlayerTime = new();
    public static long AgitaterBombedTime = new();


    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Agitater);
        AgiTaterBombCooldown = FloatOptionItem.Create(Id + 10, "AgitaterBombCooldown", new(10f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        PassCooldown = FloatOptionItem.Create(Id + 11, "AgitaterPassCooldown", new(0f, 5f, 0.25f), 1f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        BombExplodeCooldown = FloatOptionItem.Create(Id + 12, "BombExplodeCooldown", new(1f, 10f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        AgitaterCanGetBombed = BooleanOptionItem.Create(Id + 13, "AgitaterCanGetBombed", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        AgitaterAutoReportBait = BooleanOptionItem.Create(Id + 14, "AgitaterAutoReportBait", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
    }
    public static void Init()
    {
        playerIdList = new();
        CurrentBombedPlayer = byte.MaxValue;
        LastBombedPlayer = byte.MaxValue;
        AgitaterHasBombed = false;
        CurrentBombedPlayerTime = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void ResetBomb()
    {
        CurrentBombedPlayer = byte.MaxValue;
        CurrentBombedPlayerTime = new();
        LastBombedPlayer = byte.MaxValue;
        AgitaterHasBombed = false;
        SendRPC(CurrentBombedPlayer, LastBombedPlayer);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AgiTaterBombCooldown.GetFloat();

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsEnable) return false;
        if (AgitaterAutoReportBait.GetBool() && target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence) || (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)))
        {
            target.RpcMurderPlayer(killer);
            ResetBomb();
            return false;
        }

        CurrentBombedPlayer = target.PlayerId;
        LastBombedPlayer = killer.PlayerId;
        CurrentBombedPlayerTime = Utils.GetTimeStamp();
        killer.RpcGuardAndKill(killer);
        killer.Notify(GetString("AgitaterPassNotify"));
        target.Notify(GetString("AgitaterTargetNotify"));
        AgitaterHasBombed = true;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        _ = new LateTask(() =>
        {
            if (CurrentBombedPlayer != byte.MaxValue && GameStates.IsInTask)
            {
                var pc = Utils.GetPlayerById(CurrentBombedPlayer);
                if (pc != null && pc.IsAlive() && killer != null)
                {
                    pc.RpcMurderPlayerV3(pc);
                    Main.PlayerStates[CurrentBombedPlayer].deathReason = PlayerState.DeathReason.Bombed;
                    pc.SetRealKiller(Utils.GetPlayerById(playerIdList[0]));
                    Logger.Info($"{killer.GetNameWithRole()}  bombed {pc.GetNameWithRole()} bomb cd complete", "Agitater");
                    ResetBomb();
                }

            }
        }, BombExplodeCooldown.GetFloat(), "AgitaterBombKill");
        return false;
    }

    public static void OnReportDeadBody()
    {
        if (!IsEnable) return;
        if (CurrentBombedPlayer == byte.MaxValue) return;
        var target = Utils.GetPlayerById(CurrentBombedPlayer);
        var killer = Utils.GetPlayerById(playerIdList[0]);
        if (target == null || killer == null) return;
        target.RpcExileV2();
        target.SetRealKiller(killer);
        Main.PlayerStates[CurrentBombedPlayer].deathReason = PlayerState.DeathReason.Bombed;
        Main.PlayerStates[CurrentBombedPlayer].SetDead();
        Utils.AfterPlayerDeathTasks(target, true);
        ResetBomb();
        Logger.Info($"{killer.GetRealName()} bombed {target.GetRealName()} on report", "Agitater");
    }

    public static void PassBomb(PlayerControl player, PlayerControl target, bool IsAgitater = false)
    {
        if (!IsEnable) return;
        if (!AgitaterHasBombed) return;
        if (target.Data.IsDead) return;

        var now = Utils.GetTimeStamp();
        if (now - CurrentBombedPlayerTime < PassCooldown.GetFloat()) return;
        if (target.PlayerId == LastBombedPlayer) return;
        if (!AgitaterCanGetBombed.GetBool() && target.Is(CustomRoles.Agitater)) return;


        if (target.Is(CustomRoles.Pestilence) || (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)))
        {
            target.RpcMurderPlayer(player);
            ResetBomb();
            return;
        }
        LastBombedPlayer = CurrentBombedPlayer;
        CurrentBombedPlayer = target.PlayerId;
        CurrentBombedPlayerTime = now;
        Utils.MarkEveryoneDirtySettings();


        player.Notify(GetString("AgitaterPassNotify"));
        target.Notify(GetString("AgitaterTargetNotify"));

        SendRPC(CurrentBombedPlayer, LastBombedPlayer);
        Logger.Msg($"{player.GetNameWithRole()} passed bomb to {target.GetNameWithRole()}", "Agitater Pass");
    }

    public static void SendRPC(byte newbomb, byte oldbomb)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RpcPassBomb, SendOption.Reliable, -1);
        writer.Write(newbomb);
        writer.Write(oldbomb);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        CurrentBombedPlayer = reader.ReadByte();
        LastBombedPlayer = reader.ReadByte();
    }
}
