using ProbMind.Application;
using ProbMind.Infrastructure;
using ProbMind.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddProbMindApplication();
builder.Services.AddProbMindInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AnalyticsWorker>();

var host = builder.Build();
await host.RunAsync();
