using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Jinx
{
    private static readonly int Id = 12200;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    public static OptionItem CanVent;
   // private static OptionItem HasImpostorVision;
    public static OptionItem JinxSpellTimes;

    public static void SetupCustomOption()
    
    {
        //Jinxは1人固定
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Jinx, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
    //    HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        JinxSpellTimes = IntegerOptionItem.Create(Id + 14, "JinxSpellTimes", new(1, 15, 1), 3, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
        .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
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
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(true);
    public static void CanUseVent(PlayerControl player)
    {
        bool Jinx_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Jinx_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Jinx_canUse;
    }
}
