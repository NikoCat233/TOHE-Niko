namespace TOHE.Modules
{
    public static class ForceMeeting
    {
        public static bool shouldSkip = false;
        public static void Action(bool countVote = false)
        {
            if (!GameStates.IsMeeting)
            {
                PlayerControl.LocalPlayer.NoCheckStartMeeting(null, true);
                shouldSkip = false;
                Logger.Info("Force start meeting", "ForceMeeting");
            }
            else
            {
                if (countVote)
                {
                    shouldSkip = true;
                    MeetingHud.Instance.CheckForEndVoting();
                    Logger.Info("Force end meeting count vote", "ForceMeeting");
                }
                else
                {
                    shouldSkip = false;
                    MeetingHud.Instance.RpcClose();
                    Logger.Info("Force end meeting without vote", "ForceMeeting");
                }
            }
        }
    }
}
