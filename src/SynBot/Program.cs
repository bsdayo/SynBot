using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SynBot;
using SynBot.Options;
using SynBot.Services;

var builder = new HostApplicationBuilder(args);

builder.Services.AddOptions<AppOptions>().Bind(builder.Configuration);
builder.Services.AddOptions<PowerShellOptions>().Bind(builder.Configuration.GetSection("PowerShell"));
builder.Services.AddSingleton<SignService>();
builder.Services.AddSingleton<PowerShellService>();

var host = builder.Build();

await new Bot(host).RunAsync();