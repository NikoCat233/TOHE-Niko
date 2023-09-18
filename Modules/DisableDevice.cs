using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE;

//参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
class DisableDevice
{
    public static bool DoDisable => Options.DisableDevices.GetBool();
    private static List<byte> DesyncComms = new();
    private static int frame = 0;
    public static readonly Dictionary<string, Vector2> DevicePos = new()
    {
        ["SkeldAdmin"] = new Vector2 (3.48f, -8.62f),
        ["SkeldCamera"] = new Vector2 (-13.06f, -2.45f),
        ["MiraHQAdmin"] = new Vector2 (21.02f, 19.09f),
        ["MiraHQDoorLog"] = new Vector2 (16.22f, 5.82f),
        ["PolusLeftAdmin"] = new Vector2 (22.80f, -21.52f),
        ["PolusRightAdmin"] = new Vector2 (24.66f, -21.52f),
        ["PolusCamera"] = new Vector2 (2.96f, -12.74f),
        ["PolusVital"] = new Vector2 (26.70f, -15.94f),
        ["AirshipCockpitAdmin"] = new Vector2 (-22.32f, 0.91f),
        ["AirshipRecordsAdmin"] = new Vector2 (19.89f, 12.60f),
        ["AirshipCamera"] = new Vector2 (8.10f, -9.63f),
        ["AirshipVital"] = new Vector2 (25.24f, -7.94f)
    };
    public static float UsableDistance()
    {
        var Map = (MapNames)Main.NormalOptions.MapId;
        return Map switch
        {
            MapNames.Skeld => 1.8f,
            MapNames.Mira => 2.4f,
            MapNames.Polus => 1.8f,
            //MapNames.Dleks => 1.5f,
            MapNames.Airship => 1.8f,
            _ => 0.0f
        };
    }
    public static void FixedUpdate()
    {
        frame = frame == 3 ? 0 : ++frame;
        if (frame != 0) return;

        if (!DoDisable) return;
        foreach (var pc in Main.AllPlayerControls)
        {
            try
            {
                if (pc.IsModClient()) continue;

                bool doComms = false;
                Vector2 PlayerPos = pc.transform.position;
                bool ignore = (Options.DisableDevicesIgnoreImpostors.GetBool() && pc.Is(CustomRoleTypes.Impostor)) ||
                        (Options.DisableDevicesIgnoreNeutrals.GetBool() && pc.Is(CustomRoleTypes.Neutral)) ||
                        (Options.DisableDevicesIgnoreCrewmates.GetBool() && pc.Is(CustomRoleTypes.Crewmate)) ||
                        (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);

                if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                {
                    switch (Main.NormalOptions.MapId)
                    {
                        case 0:
                            if (Options.DisableSkeldAdmin.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                            if (Options.DisableSkeldCamera.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                            break;
                        case 1:
                            if (Options.DisableMiraHQAdmin.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                            if (Options.DisableMiraHQDoorLog.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                            break;
                        case 2:
                            if (Options.DisablePolusAdmin.GetBool())
                            {
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                            }
                            if (Options.DisablePolusCamera.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                            if (Options.DisablePolusVital.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                            break;
                        case 4:
                            if (Options.DisableAirshipCockpitAdmin.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                            if (Options.DisableAirshipRecordsAdmin.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                            if (Options.DisableAirshipCamera.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                            if (Options.DisableAirshipVital.GetBool())
                                doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                            break;
                    }
                }
                doComms &= !ignore;
                if (doComms && !pc.inVent)
                {
                    if (!DesyncComms.Contains(pc.PlayerId))
                        DesyncComms.Add(pc.PlayerId);

                    pc.RpcDesyncRepairSystem(SystemTypes.Comms, 128);
                }
                else if (!Utils.IsActive(SystemTypes.Comms) && DesyncComms.Contains(pc.PlayerId))
                {
                    DesyncComms.Remove(pc.PlayerId);
                    pc.RpcDesyncRepairSystem(SystemTypes.Comms, 16);

                    if (Main.NormalOptions.MapId == 1)
                        pc.RpcDesyncRepairSystem(SystemTypes.Comms, 17);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "DisableDevice");
            }
        }
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public class RemoveDisableDevicesPatch
{
    public static void Postfix()
    {
        if (!Options.DisableDevices.GetBool()) return;
        UpdateDisableDevices();
    }

    public static void UpdateDisableDevices()
    {
        var player = PlayerControl.LocalPlayer;
        bool ignore = player.Is(CustomRoles.GM) ||
            !player.IsAlive() ||
            (Options.DisableDevicesIgnoreImpostors.GetBool() && player.Is(CustomRoleTypes.Impostor)) ||
            (Options.DisableDevicesIgnoreNeutrals.GetBool() && player.Is(CustomRoleTypes.Neutral)) ||
            (Options.DisableDevicesIgnoreCrewmates.GetBool() && player.Is(CustomRoleTypes.Crewmate)) ||
            (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);
        var admins = UnityEngine.Object.FindObjectsOfType<MapConsole>(true);
        var consoles = UnityEngine.Object.FindObjectsOfType<SystemConsole>(true);
        if (admins == null || consoles == null) return;
        switch (Main.NormalOptions.MapId)
        {
            case 0:
                if (Options.DisableSkeldAdmin.GetBool())
                    admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore;
                if (Options.DisableSkeldCamera.GetBool())
                    consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = false || ignore);
                break;
            case 1:
                if (Options.DisableMiraHQAdmin.GetBool())
                    admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore;
                if (Options.DisableMiraHQDoorLog.GetBool())
                    consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                break;
            case 2:
                if (Options.DisablePolusAdmin.GetBool())
                    admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                if (Options.DisablePolusCamera.GetBool())
                    consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                if (Options.DisablePolusVital.GetBool())
                    consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                break;
            case 4:
                admins.Do(x =>
                {
                    if ((Options.DisableAirshipCockpitAdmin.GetBool() && x.name == "panel_cockpit_map") ||
                        (Options.DisableAirshipRecordsAdmin.GetBool() && x.name == "records_admin_map"))
                        x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore;
                });
                if (Options.DisableAirshipCamera.GetBool())
                    consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                if (Options.DisableAirshipVital.GetBool())
                    consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore);
                break;
        }
    }
}