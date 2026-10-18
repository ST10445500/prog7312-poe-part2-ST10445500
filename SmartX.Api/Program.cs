using System.Text.Json.Serialization;
using SmartX.Api.Collections;
using SmartX.Api.Hubs;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//Program

//.....................................o0oSTART OF FILEo0o........................................//

// Program starts the API, registers services and configures the request pipeline.

var builder = WebApplication.CreateBuilder(args);

// The thresholds and timings come from config, and a bad one stops the api here with its name.
var streamSettings = builder.Configuration.GetSection("Stream").Get<StreamSettings>() ?? new StreamSettings();
streamSettings.Validate();

// Application services
builder.Services.AddSingleton(streamSettings);
builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<DeploymentStore>();
builder.Services.AddSingleton<DeploymentTreeValidator>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<TelemetryHealth>();
builder.Services.AddSingleton<AttachmentStore>();
builder.Services.AddSingleton<AttachmentEncryption>();
builder.Services.AddSingleton<PacketQueue<StreamPacket>>();
builder.Services.AddSingleton<AlertQueue<StreamAlert>>();
builder.Services.AddSingleton(new HistoryLog<HistoryPoint>(streamSettings.HistoryCap));
builder.Services.AddSingleton<ActiveFaults>();
builder.Services.AddSingleton<AlertRules>();
builder.Services.AddSingleton<StreamStats>();
builder.Services.AddSingleton<DeviceCache>();
builder.Services.AddSingleton<CommandHistory<PumpCommand>>();
builder.Services.AddSingleton<PendingCommands>();
builder.Services.AddSingleton<InsightService>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<StreamProcessor>();

// Category names arrive as strings.
builder.Services.AddControllers()
	.AddJsonOptions(options =>
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// The client is a separate app. Its origin comes from config.
var allowedOrigins = builder.Configuration
	.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
	options.AddPolicy("SmartXClient", policy =>
		// The hub connection sends credentials, which only works with explicit origins.
		policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

var app = builder.Build();

// A small demo facility gives the dashboard something to draw at startup.
var seededSensors = app.Services.GetRequiredService<SensorStore>();
DemoFleet.Seed(seededSensors);

// The tree is built from the fleet, so the fleet has to be registered first.
app.Services.GetRequiredService<DeploymentStore>().SeedFromFleet(seededSensors.GetAll());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();

// Has to come before MapControllers or it is ignored.
app.UseCors("SmartXClient");

app.UseAuthorization();

app.MapControllers();
app.MapHub<StreamHub>("/hubs/stream");

app.Run();

//.....................................o0oEND OF FILEo0o..........................................//
