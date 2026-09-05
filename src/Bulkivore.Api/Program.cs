using System.Text.Json.Serialization;
using Bulkivore.Api.Endpoints.Common.Middlewares;
using Bulkivore.Api.Infrastructure;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddKeyedNpgsqlDataSource("bulkivore-test-db");

builder.Services.AddInfrastructure();
builder.Services.AddFastEndpoints().OpenApiDocument();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseFastEndpoints(config =>
    {
        config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());

        config.Endpoints.RoutePrefix = "/api";

        config.Errors.UseProblemDetails();
        config.Endpoints.Configurator =
            ep =>
            {
                if (ep.ResDtoType.IsAssignableTo(typeof(IErrorOr)))
                {
                    ep.DontAutoSendResponse();
                    ep.PostProcessor<ResponseSender>(Order.After);
                    ep.Description(b => b
                        .ClearDefaultProduces()
                        .Produces(200, ep.ResDtoType.GetGenericArguments().First())
                        .ProducesProblemDetails()
                    );
                }
            };
    }
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
