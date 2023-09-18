﻿using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using UnityEngine;
namespace TOHE.Roles.Neutral;

public static class Pelican
{
    private static readonly int Id = 12500;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    private static Dictionary<byte, List<byte>> eatenList = new();
    private static readonly Dictionary<byte, float> originalSpeed = new();
    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pelican, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PelicanKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican]);
    }
    public static void Init()
    {
        playerIdList = new();
        eatenList = new();
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
    private static void SyncEatenList(byte playerId)
    {
        SendRPC(byte.MaxValue);
        foreach (var el in eatenList)
            SendRPC(el.Key);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPelicanEtenNum, SendOption.Reliable, -1);
        writer.Write(playerId);
        if (playerId != byte.MaxValue)
        {
            writer.Write(eatenList[playerId].Count);
            foreach (var el in eatenList[playerId])
                writer.Write(el);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        if (playerId == byte.MaxValue)
        {
            eatenList.Clear();
        }
        else
        {
            int eatenNum = reader.ReadInt32();
            eatenList.Remove(playerId);
            List<byte> list = new();
            for (int i = 0; i < eatenNum; i++)
                list.Add(reader.ReadByte());
            eatenList.Add(playerId, list);
        }
    }
    public static bool IsEaten(PlayerControl pc, byte id) => eatenList.ContainsKey(pc.PlayerId) && eatenList[pc.PlayerId].Contains(id);
    public static bool IsEaten(byte id)
    {
        foreach (var el in eatenList)
            if (el.Value.Contains(id))
                return true;
        return false;
    }
    public static bool CanEat(PlayerControl pc, byte id)
    {
        if (!pc.Is(CustomRoles.Pelican) || GameStates.IsMeeting) return false;
        var target = Utils.GetPlayerById(id);
        return target != null && target.IsAlive() && !target.inVent && !Medic.ProtectList.Contains(target.PlayerId) && !target.Is(CustomRoles.GM) && !IsEaten(pc, id) && !IsEaten(id);
    }
    public static Vector2 GetBlackRoomPS()
    {
        return Main.NormalOptions.MapId switch
        {
            0 => new Vector2(-27f, 3.3f), // The Skeld
            1 => new Vector2(-11.4f, 8.2f), // MIRA HQ
            2 => new Vector2(42.6f, -19.9f), // Polus
            4 => new Vector2(-16.8f, -6.2f), // Airship
            _ => throw new System.NotImplementedException(),
        };
    }
    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return "Invalid";
        var eatenNum = 0;
        if (eatenList.ContainsKey(playerId))
            eatenNum = eatenList[playerId].Count;
        return Utils.ColorString(eatenNum < 1 ? Color.gray : Utils.GetRoleColor(CustomRoles.Pelican), $"({eatenNum})");
    }
    public static void EatPlayer(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !CanEat(pc, target.PlayerId)) return;
        if (!eatenList.ContainsKey(pc.PlayerId)) eatenList.Add(pc.PlayerId, new());
        eatenList[pc.PlayerId].Add(target.PlayerId);

        SyncEatenList(pc.PlayerId);

        originalSpeed.Remove(target.PlayerId);
        originalSpeed.Add(target.PlayerId, Main.AllPlayerSpeed[target.PlayerId]);

        target.RpcTeleport(new Vector2 (GetBlackRoomPS().x, GetBlackRoomPS().y));
        Main.AllPlayerSpeed[target.PlayerId] = 0.5f;
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        target.MarkDirtySettings();

        Utils.NotifyRoles(SpecifySeer: pc);
        Utils.NotifyRoles(SpecifySeer: target);
        Logger.Info($"{pc.GetRealName()} 吞掉了 {target.GetRealName()}", "Pelican");
    }

    public static void OnReportDeadBody()
    {
        foreach (var pc in eatenList)
        {
            foreach (var tar in pc.Value)
            {
                var target = Utils.GetPlayerById(tar);
                var killer = Utils.GetPlayerById(pc.Key);
                if (killer == null || target == null) continue;
                Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
                ReportDeadBodyPatch.CanReport[tar] = true;
                target.RpcExileV2();
                target.SetRealKiller(killer);
                Main.PlayerStates[tar].deathReason = PlayerState.DeathReason.Eaten;
                Main.PlayerStates[tar].SetDead();
                Utils.AfterPlayerDeathTasks(target, true);
                Logger.Info($"{killer.GetRealName()} 消化了 {target.GetRealName()}", "Pelican");
            }
        }
        eatenList.Clear();
        SyncEatenList(byte.MaxValue);
    }

    public static void OnPelicanDied(byte pc)
    {
        if (!eatenList.ContainsKey(pc)) return;
        foreach (var tar in eatenList[pc])
        {
            var target = Utils.GetPlayerById(tar);
            var player = Utils.GetPlayerById(pc);
            if (player == null || target == null) continue;
            target.RpcTeleport(player.transform.position);
            Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
            ReportDeadBodyPatch.CanReport[tar] = true;
            target.MarkDirtySettings();
            RPC.PlaySoundRPC(tar, Sounds.TaskComplete);
            Utils.NotifyRoles(SpecifySeer: target);
            Logger.Info($"{Utils.GetPlayerById(pc).GetRealName()} 吐出了 {target.GetRealName()}", "Pelican");
        }
        eatenList.Remove(pc);
        SyncEatenList(pc);
    }

    private static int Count = 0;
    public static void OnFixedUpdate()
    {
        if (!GameStates.IsInTask)
        {
            if (eatenList.Any())
            {
                eatenList.Clear();
                SyncEatenList(byte.MaxValue);
            }
            return;
        }

        if (!IsEnable) return; Count--; if (Count > 0) return; Count = 30;

        foreach (var pc in eatenList)
        {
            foreach (var tar in pc.Value)
            {
                var target = Utils.GetPlayerById(tar);
                if (target == null) continue;
                var pos = GetBlackRoomPS();
                var dis = Vector2.Distance(pos, target.transform.position);
                if (dis < 1f) continue;
                target.RpcTeleport(new Vector2 (pos.x, pos.y));
                Utils.NotifyRoles(SpecifySeer: target);
            }
        }
    }
}