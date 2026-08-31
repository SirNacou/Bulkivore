var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("bulkivore-db");

var api = builder.AddProject<Projects.Bulkivore_Api>("api")
	.WithReference(db)
	.WaitFor(db)
	.WithHttpHealthCheck("/health");

builder.Build().Run();