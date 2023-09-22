using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;

namespace TOHE.Modules
{
    public class HostPublicSettings
    {
        public static void ChangeSettings()
        {
            if (!Main.HostPublic.Value) return;
            
            if (Options.MadmateSpawnMode.GetInt() == 1)
                Options.MadmateSpawnMode.SetValue(0, false);

            Jackal.SidekickAssignMode.SetValue(3, false);
            Jackal.SidekickCanKillJackal.SetValue(1, false);
            Jackal.JackalCanKillSidekick.SetValue(1, false);

            Sheriff.MisfireKillsTarget.SetValue(1, false);

            Options.ImpCanKillMadmate.SetValue(1, false);
            Options.MadmateCanKillImp.SetValue(1, false);

            Logger.Info("Host Public changed settings", "HostPublicPatch");
            OptionItem.SyncAllOptions();
        }
    }
}
