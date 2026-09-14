using System.Text.Json.Serialization;
using SmartX.Api.Services;

//ST10445500 - PROG7312 - SmartX POE
//Program

//.....................................o0oSTART OF FILEo0o........................................//

// Program starts the API, registers services and configures the request pipeline.

var builder = WebApplication.CreateBuilder(args);

// Application services
builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<DeploymentStore>();
builder.Services.AddSingleton<DeploymentTreeValidator>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<TelemetryHealth>();
builder.Services.AddSingleton<AttachmentStore>();
builder.Services.AddSingleton<AttachmentEncryption>();

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
		policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
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

app.Run();

//.....................................o0oEND OF FILEo0o..........................................//
