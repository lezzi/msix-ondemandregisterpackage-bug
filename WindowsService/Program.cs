using WindowsService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "NetworkProtection");
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();