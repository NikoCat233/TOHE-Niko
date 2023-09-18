using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.ShowButtons))]
public class EndGameManagerPatch
{
    public static bool IsRestarting { get; private set; }
    //private static string _playAgainText = "Re-entering lobby in {0}s";
    //private static TextMeshPro autoPlayAgainText;

    public static void Postfix(EndGameManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.AutoPlayAgain.GetBool()) return;
        IsRestarting = false;

        _ = new LateTask(() =>
        {
            Logger.Msg("Beginning Auto Play Again Countdown!", "AutoPlayAgain");
            IsRestarting = true;
            BeginAutoPlayAgainCountdown(__instance, Options.AutoPlayAgainCountdown.GetInt());
        }, 0.5f, "Auto Play Again");
    }

    public static void CancelPlayAgain()
    {
        IsRestarting = false;
        //if (autoPlayAgainText != null) autoPlayAgainText.gameObject.SetActive(false);
    }

    private static void BeginAutoPlayAgainCountdown(EndGameManager endGameManager, int seconds)
    {
        if (!IsRestarting) return;
        if (endGameManager == null) return;
        EndGameNavigation navigation = endGameManager.Navigation;
        if (navigation == null) return;

        /*if (autoPlayAgainText == null)
        {
            autoPlayAgainText = Object.Instantiate(navigation.gameObject.GetComponentInChildren<TextMeshPro>(), navigation.transform);
            autoPlayAgainText.fontSize += 6;
            autoPlayAgainText.color = Color.white;
            autoPlayAgainText.transform.localScale += new Vector3(0.25f, 0.25f);
            _ = new LateTask(() => autoPlayAgainText.text = _playAgainText.StringBuilder(seconds.ToString()), 0.001f);
            autoPlayAgainText.transform.localPosition += new Vector3(3.5f, 2.6f); 
        }*/

        //   autoPlayAgainText.text = _playAgainText.Formatted(seconds.ToString());
        if (seconds == 0) navigation.NextGame();
        else _ = new LateTask(() => BeginAutoPlayAgainCountdown(endGameManager, seconds - 1), 1.1f);
    }
}