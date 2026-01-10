var builder = DistributedApplication.CreateBuilder(args);

// Redis for caching (PostgreSQL is hosted on Neon, not local)
var redis = builder.AddRedis("redis")
	.WithDataVolume("redis-data")
	.WithRedisCommander();

// API project
var api = builder.AddProject<Projects.API>("api")
	.WithReference(redis)
	.WaitFor(redis)
	.WithExternalHttpEndpoints();

// Frontend (Vite + React)
var frontend = builder.AddNpmApp("frontend", "../Front", "dev")
	.WithReference(api)
	.WaitFor(api)
	.WithHttpEndpoint(port: 5173, targetPort: 5173, env: "PORT", isProxied: false)
	.WithExternalHttpEndpoints();

builder.Build().Run();
