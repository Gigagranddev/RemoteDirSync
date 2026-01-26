using RemoteDirSync.Bot;

var host = await WebApiHost.StartAsync(args, port: 5000);
await host.WaitForShutdownAsync();