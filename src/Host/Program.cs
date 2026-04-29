using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RNIF2.Core.Configuration;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Validation;
using RNIF2.Handlers;
using RNIF2.Host;
using RNIF2.Parsing;
using RNIF2.Security;
using RNIF2.Signals;
using RNIF2.Transport;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .Configure<RnifOptions>(builder.Configuration.GetSection("Rnif"))
    .AddSingleton<IMimeParser, MimeMessageParser>()
    .AddSingleton<RnifMessageExtractor>()
    .AddSingleton<IMessageValidator, RnifMessageValidator>()
    .AddSingleton<ISecurityService, SmimeSecurityService>()
    .AddSingleton<ISignalGenerator, SignalGenerator>()
    .AddSingleton<IRnifMessageSerializer, RnifMessageSerializer>()
    .AddSingleton<IMessageRouter, MessageRouter>()
    .AddDefaultRnifHandlers()
    .AddSingleton<IRnifRequestPipeline, RnifRequestPipeline>()
    .AddSingleton<HttpListenerServer>()
    .AddHostedService<Worker>();

var host = builder.Build();
host.Run();
