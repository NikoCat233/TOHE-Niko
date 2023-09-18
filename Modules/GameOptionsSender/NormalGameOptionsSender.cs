using System;
using AmongUs.GameOptions;

namespace TOHE.Modules;

public class NormalGameOptionsSender : GameOptionsSender
{
    public override IGameOptions BasedGameOptions =>
        GameOptionsManager.Instance.CurrentGameOptions;
    public override bool IsDirty
    {
        get
        {
            try
            {
                if (_logicOptions == null || !GameManager.Instance.LogicComponents.Contains(_logicOptions))
                {
                    foreach (var glc in GameManager.Instance?.LogicComponents)
                        if (glc.TryCast<LogicOptions>(out var lo))
                            _logicOptions = lo;
                }
                return _logicOptions != null && _logicOptions.IsDirty;
            }
            catch (Exception error)
            {
                Logger.Fatal(error.ToString(), "NormalGameOptionsSender.IsDirty.Get");
                return false;
            }
        }
        protected set
        {
            try
            {
                if (_logicOptions != null)
                    _logicOptions.ClearDirtyFlag();
            }
            catch (Exception error)
            {
                Logger.Fatal(error.ToString(), "NormalGameOptionsSender.IsDirty.ProtectedSet");
            }
        }
    }
    private LogicOptions _logicOptions;

    public override IGameOptions BuildGameOptions()
        => BasedGameOptions;
}