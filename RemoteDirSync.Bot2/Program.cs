using RemoteDirSync.Bot;

var host = await WebApiHost.StartAsync(args);
await host.WaitForShutdownAsync();