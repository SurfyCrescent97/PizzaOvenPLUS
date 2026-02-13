using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace PizzaOven
{
    internal class PLUSRPC
    {
        public static class DiscordPresenceService
        {
            private static DiscordRpcClient? client;

            public static void Initialize()
			{
				using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/PizzaOven;component/PLUSSECRETS.json"))!.Stream;

				using var reader = new StreamReader(stream);
				var json = reader.ReadToEnd();

				var appId = JsonSerializer
					.Deserialize<JsonElement>(json)
					.GetProperty("discord_appid")
					.GetString();

				client = new DiscordRpcClient(appId);

				client.Logger = new ConsoleLogger
				{
					Level = LogLevel.Warning
				};

				client.Initialize();

				client.SetPresence(new RichPresence
				{
					Details = "PizzaOven but More",
					State = "Tool by SurfyCrescent97",
                    Assets = new Assets
					{
                        LargeImageText = "Pizza Oven+"
					},
					Timestamps = Timestamps.Now
				});
			}


            public static void Shutdown()
            {
				try
				{
					client.ClearPresence();
					client.Deinitialize();
					client?.Dispose();
					client = null;
				}
				catch { }
            }
        }
    }
}
