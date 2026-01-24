using RemoteDirSync.Bot;

var host = await WebApiHost.StartAsync(args, "localhost", port: 5000);
await host.WaitForShutdownAsync();