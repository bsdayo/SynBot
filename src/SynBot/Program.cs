using FluentValidation;
using Lagrange.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SynBot.Infrastructure;

AppConfiguration.CreateIfNotExists();

var builder = new HostApplicationBuilder();

builder.Configuration.AddAppConfigurationSources(args);

builder.Services.AddOptions<BotDeviceInfo>().Bind(builder.Configuration.GetSection("Device"));
builder.Services.AddOptions<BotKeystore>().Bind(builder.Configuration.GetSection("Keystore"));

builder.Services.AddHttpClient();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var host = builder.Build();

await new SynBot.SynBot(host).RunAsync();