using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Blackmailer
{
    private static readonly int Id = 50000;
    private static List<byte> playerIdList = new();
    private static OptionItem SkillCooldown;
    private static OptionItem SkillLimit;
    public static OptionItem KillCoolDown;
    public static OptionItem TargetSpeakTimes;
    public static OptionItem TryHideMsg;
    public static Dictionary<byte, int> BlackmailerSkill;
    public static Dictionary<byte, int> ForBlackmailer = new();
    public static bool IsEnable = false;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Blackmailer, canPublic: true);
        KillCoolDown = FloatOptionItem.Create(Id + 14, "KillCooldown", new(2.5f, 600f, 2.5f), 20f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blackmailer]);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blackmailer])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimit = IntegerOptionItem.Create(Id + 11, "BlackmailerSkillLimit", new(0, 100, 1), 2, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blackmailer]);
        TargetSpeakTimes = IntegerOptionItem.Create(Id + 12, "BlackmailerTargetChances", new(0, 100, 1), 1, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blackmailer]);
        TryHideMsg = BooleanOptionItem.Create(Id + 13, "BlackmailerHideTargetMsg", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blackmailer]);
    }
    public static void Init()
    {
        playerIdList = new();
        BlackmailerSkill = new();
        ForBlackmailer = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BlackmailerSkill.Add(playerId, 0);
        IsEnable = true;
    }
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public static void OnShapeShift(PlayerControl player, PlayerControl target)
    {
        if (!player.Is(CustomRoles.Blackmailer) || !IsEnable) return;
        if (player.Data.IsDead || target.Data.IsDead || GameStates.IsMeeting) return;
       
        if (BlackmailerSkill[player.PlayerId] < SkillLimit.GetInt() && !ForBlackmailer.ContainsKey(target.PlayerId)
            && CanBeBlackmailed(target))
        {
            BlackmailerSkill[player.PlayerId]++;
            ForBlackmailer.Add(target.PlayerId, 0);
            player.Notify(GetString("BlackmailerSsMsg"));
            return;
        }
        else
        {
            if (BlackmailerSkill[player.PlayerId] >= SkillLimit.GetInt())
            {
                player.Notify(GetString("BlackmailerFailMsg1"));
                return; 
            }
            else if (ForBlackmailer.ContainsKey(target.PlayerId) || target.Data.IsDead)
            {
                player.Notify(GetString("BlackmailerFailMsg2"));
                return;
            }
        }
    }

    public static bool TargetCheckMsg(PlayerControl pc, bool isUI = false)
    {
        if (pc.AmOwner && ForBlackmailer.ContainsKey(pc.PlayerId)&& !AmongUsClient.Instance.AmHost) return true;
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!pc.Data.IsDead && ForBlackmailer.ContainsKey(pc.PlayerId) && GameStates.IsMeeting)
        {
            if (ForBlackmailer[pc.PlayerId] >= TargetSpeakTimes.GetInt())
            {
                GuessManager.RpcGuesserMurderPlayer(pc);
                _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("BlackmailerSucceed"), pc.GetRealName()), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmailerTitle"))); }, 0.6f, "Blackmailer Skill Kill");
            }
            else
            {
                ForBlackmailer[pc.PlayerId]++;
                if (TryHideMsg.GetBool()) GuessManager.TryHideMsg();
                _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("BlackmailerTarget"), ForBlackmailer[pc.PlayerId].ToString()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmailerTitle"))); }, 0.6f, "Blackmailer Target Chat");
            }
            return true;
        }
        return false;
    }
    public static void AfterMeetingTasks()
    {
        ForBlackmailer = new();
    }
    public static bool CanBeBlackmailed(PlayerControl pc)
    {
        return true;
    }
}