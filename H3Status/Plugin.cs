using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace H3Status;

[BepInProcess("h3vr.exe")]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "xyz.bacur.plugins.h3status";
    public const string Name = "H3Status";
    public const string Version = "0.1.0";


    internal static new ManualLogSource Logger;
    private static Harmony _harmony;

    public static HttpServer server;
    public static int port = 9504;

    private void Awake()
    {
        Logger = base.Logger;
        server = new HttpServer(port);
        server.AddWebSocketService<Server.ServerBehavior>("/");
        server.Start();

        Logger.LogInfo($"Server started on port {port}");

        _harmony = new Harmony(Guid);
        _harmony.PatchAll(typeof(Patches.SceneHandler));
        _harmony.PatchAll(typeof(Patches.TNHScoreHandler));
        _harmony.PatchAll(typeof(Patches.TNHPhaseHandler));
        _harmony.PatchAll(typeof(Patches.PlayerHealthHandler));
        _harmony.PatchAll(typeof(Patches.WeaponAmmoHandler));
    }

    private void OnDestroy()
    {
        server.Stop();
        _harmony.UnpatchSelf();
    }
}
