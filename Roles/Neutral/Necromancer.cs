using System.Linq;
using System.Collections.Generic;

namespace TOHE.Roles.Neutral;

public static class Necromancer
{
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

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
}