using Bulkivore.Api.Infrastructure;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("bulkivore-db");

builder.Services.AddInfrastructure();
builder.Services.AddFastEndpoints().OpenApiDocument();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseFastEndpoints(config => { config.Endpoints.RoutePrefix = "/api"; });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
