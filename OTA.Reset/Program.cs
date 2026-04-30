using MongoDB.Driver;

const string connectionString = "mongodb://iot.ssmsportal.com:39999/?replicaSet=fiot-rs&serverSelectionTimeoutMS=5000&connectTimeoutMS=5000";
const string databaseName     = "OTAPlatform";

var collections = new[]
{
    "Devices",
    "OtaJobs",
    "Firmwares",
    "Rollouts",
    "RolloutPolicies",
    "RepositoryMasters",
    "RepositoryEvents",
    "Projects",
    "clients",
    "AuditLogs",
    "DeviceOtaEvents",
};

Console.WriteLine("\n=== OTAPlatform Data Reset (Users preserved) ===\n");

var client   = new MongoClient(connectionString);
var database = client.GetDatabase(databaseName);

foreach (var name in collections)
{
    try
    {
        var col    = database.GetCollection<MongoDB.Bson.BsonDocument>(name);
        var result = await col.DeleteManyAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);
        Console.WriteLine($"  ✓  {name,-22} {result.DeletedCount} document(s) deleted");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✗  {name,-22} ERROR: {ex.Message}");
    }
}

Console.WriteLine("\nDone. Users collection untouched.\n");
