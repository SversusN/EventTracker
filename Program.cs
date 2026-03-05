using EventTracker.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();
app.Run();