using TodoApp.Data;
using TodoApp.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<TodoDbContext>("tododb");
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
