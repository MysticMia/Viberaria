using System;
using System.Threading.Tasks;

using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using Terraria.ModLoader;
using Color = Microsoft.Xna.Framework.Color;
using static Viberaria.ViberariaConfig;


namespace Viberaria;

public static class bClient
{
    public static readonly ButtplugClient _client = new("Viberaria");
    private static readonly ButtplugWebsocketConnector _connector = new(new Uri("ws://"+IntifaceConnectionAddress));

    public static void ClientHandles()
    {
        _client.DeviceAdded += HandleDeviceAdded;
        _client.DeviceRemoved += HandleDeviceRemoved;
        _client.ServerDisconnect += HandleServerDisconnect;
    }

    public static async void ClientConnect()
    {
        if (_client.Connected)
            return;

        try
        {
            await _client.ConnectAsync(_connector);
            await Task.Delay(1000);
            await _client.StartScanningAsync();
        }
        catch (Exception ex) // todo: specify exception type
        {
            tChat.LogToPlayer("Viberaria: Likely couldn't connect to Intiface. Make sure you have Intiface Central " +
                              "running on this pc or disable the mod in the mod configuration.", Color.Orange);
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't connect to Intiface Client ({0}): {1}",
                ex.GetType(), ex.StackTrace);
            await Task.Delay(4000);
            ClientConnect();
        }
    }

    public static async void ClientDisconnect()
    {
        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            ModContent.GetInstance<Viberaria>().Logger.ErrorFormat("Couldn't disconnect from Intiface Client ({0}): {1}",
                ex.GetType(), ex.StackTrace);
        }
    }

    private static void HandleDeviceAdded(object obj, DeviceAddedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been added!", Color.Fuchsia);
    private static void HandleDeviceRemoved(object obj, DeviceRemovedEventArgs args)
        => tChat.LogToPlayer($"{args.Device.Name} has been removed!", Color.Aqua);

    private static void HandleServerDisconnect(object obj, EventArgs args)
        => tChat.LogToPlayer("Intiface server disconnected!", Color.Aqua);
}