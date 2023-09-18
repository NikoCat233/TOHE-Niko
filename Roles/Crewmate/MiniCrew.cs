using Hazel;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Crewmate
{
    internal class MiniCrew
    {
        private static readonly int Id = 9900000;
        public static bool isEnable;
        public static bool IsEvilMini;
        public static void SetMiniTeam()
        {
            if (CanBeEvil.GetBool())
            {
                var rand = IRandom.Instance;
                IsEvilMini = rand.Next(1, 100) < EvilMiniSpawnChances.GetInt();
            }
            else IsEvilMini = false;
        }
        //private static List<byte> playerIdList = new();
        public static int Age = new();
        public static float GrowUpTime = new();
        private static long LastFixedUpdate = new();

        public static OptionItem GrowUpDuration;
        public static OptionItem EveryoneCanKnowMini;
        public static OptionItem ShowMiniAge;
        public static OptionItem CanBeEvil;
        public static OptionItem EvilMiniSpawnChances;
        public static OptionItem CountMeetingTime;
        public static OptionItem MiniBeginCD;
        public static OptionItem MiniFinalCD;
        public static float MiniKillCoolDown;
        public static float DKillCoolDownPreAge;
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.MiniCrew, 1, zeroOne: false);
            GrowUpDuration = FloatOptionItem.Create(Id + 10, "GrowUpDuration", new(0, 800, 15), 300, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.MiniCrew])
                .SetValueFormat(OptionFormat.Seconds);
            EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 11, "EveryoneCanKnowMini", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.MiniCrew]);
            ShowMiniAge = BooleanOptionItem.Create(Id + 12, "ShowMiniAge", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.MiniCrew]);
            CountMeetingTime = BooleanOptionItem.Create(Id + 13, "CountMeetingTime", false, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.MiniCrew]);
            CanBeEvil = BooleanOptionItem.Create(Id + 14, "MiniCanBeEvil", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.MiniCrew]);
            EvilMiniSpawnChances = IntegerOptionItem.Create(Id + 15, "EvilMiniSpawnChances", new(0, 100, 5), 20, TabGroup.OtherRoles, false)
                .SetParent(CanBeEvil)
                .SetValueFormat(OptionFormat.Percent);
            MiniBeginCD = FloatOptionItem.Create(Id + 16, "MiniBeginCD", new(0f, 180f, 2.5f), 45f, TabGroup.OtherRoles, false)
                .SetParent(CanBeEvil)
                .SetValueFormat(OptionFormat.Seconds);
            MiniFinalCD = FloatOptionItem.Create(Id + 17, "MiniFinalCD", new(0f, 180f, 2.5f), 15f, TabGroup.OtherRoles, false)
                .SetParent(CanBeEvil)
               .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            SetMiniTeam();
            Age = 0;
            isEnable = false;
            LastFixedUpdate = new();
            DKillCoolDownPreAge = MiniFinalCD.GetFloat() < MiniBeginCD.GetFloat() ? (MiniBeginCD.GetFloat() - MiniFinalCD.GetFloat()) / 18 : 0f;
            MiniKillCoolDown = MiniBeginCD.GetFloat();
    }
        public static void Add(byte playerId)
        {
            //playerIdList.Add(playerId);
            isEnable = true;
        }

        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInGame || !AmongUsClient.Instance.AmHost) return;
            if (!player.Is(CustomRoles.MiniCrew) || !isEnable) return;
            if (Age >= 18 || (!CountMeetingTime.GetBool() && GameStates.IsMeeting)) return;

            if (LastFixedUpdate == Utils.GetTimeStamp()) return;
            LastFixedUpdate = Utils.GetTimeStamp();
            GrowUpTime++;

            bool GrowUpUpdate = false;
            if (GrowUpTime >= GrowUpDuration.GetInt() / 18)
            {
                GrowUpTime = 0;
                Age += 1;
                GrowUpUpdate = true;
            }

            if (GrowUpUpdate)
            {
                Utils.NotifyRoles();
                SendRPC(player.PlayerId);
                if (IsEvilMini)
                {
                    if (Age < 18)
                    {
                        MiniKillCoolDown -= DKillCoolDownPreAge;
                    }
                    else
                    {
                        MiniKillCoolDown = MiniFinalCD.GetFloat();
                    }
                    Main.AllPlayerKillCooldown[player.PlayerId] = MiniKillCoolDown;
                    player.SetKillCooldown(forceAnime: true);
                    player.MarkDirtySettings();
                }
            }
        }

        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncMiniCrewAge, SendOption.Reliable, -1);
            writer.Write(Age);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            Age = reader.ReadInt32();
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = MiniKillCoolDown;
        public static string GetAge() => Utils.ColorString(Color.yellow, Age < 18 ? $"({Age})" : "(18)");
    }
}
