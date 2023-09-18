using Hazel;

namespace TOHE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (pc == null || !pc.Data.IsDead) return;
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        
        //pc.RpcSetPet("");
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.SetPetStr, SendOption.None, -1);
        messageWriter.Write("");
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
}