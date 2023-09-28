using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    public static class President
    {
        private static readonly int Id = 51000;
        private static List<byte> playerIdList = new();
        public static bool IsEnable = false;
        public static Dictionary<byte, float>CheckLimit = new();

        public static OptionItem SkillLimit;
        public static OptionItem RevealRole;
        public static OptionItem countVote;
        public static OptionItem SkillGainPreTask;
        public static OverrideTasksData PresidentTasks;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.President, 1, canPublic: true);
            SkillLimit = IntegerOptionItem.Create(Id + 10, "PresidentSkillLimit", new(0, 100, 1), 2, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
            RevealRole = BooleanOptionItem.Create(Id + 11, "EveryoneKnowPresident", false, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
            countVote = BooleanOptionItem.Create(Id + 12, "PresidentcountVote", false, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
            SkillGainPreTask = FloatOptionItem.Create(Id + 13, "PresidentSkillGainPreTask", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
            PresidentTasks = OverrideTasksData.Create(Id + 14, TabGroup.CrewmateRoles, CustomRoles.President);
        }

        public static void Init()
        {
            IsEnable = false;
            playerIdList = new();
            CheckLimit = new();
        }
        public static void Add(byte playerId)
        {
            IsEnable = true;
            playerIdList.Add(playerId);
            CheckLimit.Add(playerId, 0);
        }

        public static bool CheckMsg(PlayerControl pc, string msg, bool isUI = false)
        {
            var originMsg = msg;
            if (!AmongUsClient.Instance.AmHost) return false;
            if (!GameStates.IsInGame || pc == null) return false;
            if (!pc.Is(CustomRoles.President)) return false;

            if (pc.Data.IsDead)
            {
                if (!isUI) Utils.SendMessage(GetString("PresidentDead"), pc.PlayerId);
                else pc.ShowPopUp(GetString("PresidentDead"));
                return true;
            }

            if (!CheckLimit.ContainsKey(pc.PlayerId))
                CheckLimit.Add(pc.PlayerId, 0);

            if (CheckCommond(ref msg, "skip|过|跳|sk", false))
            {
                if (!GameStates.IsMeeting) return false;
                if (!RevealRole.GetBool()) GuessManager.TryHideMsg();
                    else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

                if (CheckLimit[pc.PlayerId] >= SkillLimit.GetFloat())
                {
                    if (!isUI) Utils.SendMessage(GetString("PresidentSkillMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("PresidentSkillMax"));

                    return true;
                }

                CheckLimit[pc.PlayerId] += 1f;
                _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("PresidentSkip"), (RevealRole.GetBool() ? pc.GetRealName() : "")), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.President), GetString("PresidentTitle"))); }, 0.3f, "President Msg");

                ForceMeeting.Action(countVote.GetBool());
                Logger.Info("President action: " + pc.GetNameWithRole() + " Skill used: " + CheckLimit[pc.PlayerId].ToString(), "President action");
                return true;
            }
            return false;
        }
        //private static void SendRPC(byte playerId)
        //{
        //    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncPresidentSkill, SendOption.Reliable, -1);
        //    writer.Write(playerId);
        //    writer.Write(CheckLimit[playerId].ToString());
        //    AmongUsClient.Instance.FinishRpcImmediately(writer);
        //}
        //public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
        //{
        //    byte PlayerId = reader.ReadByte();
        //    float usedtimes = reader.
            
        //}
        public static bool CheckCommond(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            for (int i = 0; i < comList.Count(); i++)
            {
                if (exact)
                {
                    if (msg == "/" + comList[i]) return true;
                }
                else
                {
                    if (msg.StartsWith("/" + comList[i]))
                    {
                        msg = msg.Replace("/" + comList[i], string.Empty);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
