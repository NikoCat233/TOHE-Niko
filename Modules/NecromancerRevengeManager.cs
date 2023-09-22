using HarmonyLib;
using Hazel;
using System;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class NecromancerRevengeManager
{
    public static bool NecromancerMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Necromancer)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;
        if (Options.NecromancerCanKillNum.GetInt() < 1)
        {
            if (!isUI) Utils.SendMessage(GetString("NecromancerKillDisable"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NecromancerKillDisable"));
            return true;
        }

        if (!pc.Data.IsDead)
        {
            Utils.SendMessage(GetString("NecromancerAliveKill"), pc.PlayerId);
            return true;
        }

        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (Main.NecromancerRevenged.ContainsKey(pc.PlayerId))
        {
            if (Main.NecromancerRevenged[pc.PlayerId] >= Options.NecromancerCanKillNum.GetInt())
            {
                if (!isUI) Utils.SendMessage(GetString("NecromancerKillMax"), pc.PlayerId);
                else pc.ShowPopUp(GetString("NecromancerKillMax"));
                return true;
            }
        }
        else
        {
            Main.NecromancerRevenged.Add(pc.PlayerId, 0);
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/rv", string.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            if (!isUI) Utils.SendMessage(GetString("NecromancerKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NecromancerKillDead"));
            return true;
        }

        if (target == null || target.Data.IsDead)
        {
            if (!isUI) Utils.SendMessage(GetString("NecromancerKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NecromancerKillDead"));
            return true;
        }
        if (target.Is(CustomRoles.Pestilence))
        {
            if (!isUI) Utils.SendMessage(GetString("PestilenceImmune"), pc.PlayerId);
            else pc.ShowPopUp(GetString("PestilenceImmune"));
            return true;
        }
        if (target.Is(CustomRoles.MiniCrew) && MiniCrew.Age < 18)
        {
            if (!isUI) Utils.SendMessage(GetString("GuessMiniCrew"), pc.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("GuessMiniCrew")));
            else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("GuessMiniCrew")));
            return true;
        }
        if (target.Is(CustomRoles.Onbound) && Options.OnBoundAntiAvange.GetBool())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessOnBound"), pc.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("GuessMiniCrew")));
            else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("GuessOnBound")));
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Necromancer");

        string Name = target.GetRealName();

        Main.NecromancerRevenged[pc.PlayerId]++;

        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            target.SetRealKiller(pc);

            if (GameStates.IsMeeting)
            {
                GuessManager.RpcGuesserMurderPlayer(target);

                //死者检查
                Utils.AfterPlayerDeathTasks(target, true);

                Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
            }
            else
            {
                target.RpcMurderPlayerV3(target);
                Main.PlayerStates[target.PlayerId].SetDead();
            }

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("NecromancerKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Necromancer), GetString("NecromancerRevengeTitle"))); }, 0.6f, "Necromancer Kill");

        }, 0.2f, "Necromancer Kill");
        return true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NecromancerRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        NecromancerMsgCheck(pc, $"/rv {PlayerId}", true);
    }

    private static void NecromancerOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Necromancer UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) NecromancerMsgCheck(PlayerControl.LocalPlayer, $"/rv {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Necromancer) && !PlayerControl.LocalPlayer.IsAlive())
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;
            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("TargetIcon");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => NecromancerOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}
