var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithImage("pgvector/pgvector","pg16").WithVolume("regulationdb-data", "/var/lib/postgresql/data");
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithImage("rabbitmq","3.12-management").WithVolume("rabbitmq-data", "/var/lib/rabbitmq");
var regulationDb = postgres.AddDatabase("regulationdb");
builder.Build().Run();
