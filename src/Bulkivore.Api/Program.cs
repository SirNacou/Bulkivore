using Bulkivore.Api.Endpoints.Common;
using Bulkivore.Api.Endpoints.Common.Middlewares;
using Bulkivore.Api.Infrastructure;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddKeyedNpgsqlDataSource("bulkivore-test-db");

builder.Services.AddInfrastructure();
builder.Services.AddFastEndpoints()
    .OpenApiDocument(o =>
    {
        o.DocumentName = "v1";
        o.ShortSchemaNames = true;
    });

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultExceptionHandler()
    .UseFastEndpoints(config =>
    {
        config.Binding.UsePropertyNamingPolicy = true;
        config.Serializer.Options.Configure();

        config.Endpoints.RoutePrefix = "/api";
        config.Endpoints.ShortNames = true;
        config.Endpoints.NameGenerator = context => context.EndpointType.Name.TrimEnd("Endpoint").ToString();

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
                        .ProducesProblemDetails());
                }
            };
    });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(pattern: "/openapi/{documentName}.yaml");
    app.MapScalarApiReference();
}

app.Run();
