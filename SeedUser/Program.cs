using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

var connectionString = "mongodb://iot.ssmsportal.com:39999/?replicaSet=fiot-rs";
var databaseName     = "OTAPlatform";
var collectionName   = "Users";

var email    = "thiyagu@raxgbc.co.in";
var password = "Rax@123";
var name     = "Thiyagu";
var role     = "SuperAdmin";

var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

var client     = new MongoClient(connectionString);
var db         = client.GetDatabase(databaseName);
var collection = db.GetCollection<BsonDocument>(collectionName);

await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("email", email));

var now    = DateTime.UtcNow;
var userId = Guid.NewGuid().ToString();

var doc = new BsonDocument
{
    { "_id",          ObjectId.GenerateNewId() },
    { "userId",       userId },
    { "name",         name },
    { "email",        email },
    { "passwordHash", hash },
    { "role",         role },
    { "projectScope", new BsonArray() },
    { "isActive",     true },
    { "createdAt",    now },
    { "updatedAt",    now },
};

await collection.InsertOneAsync(doc);
Console.WriteLine("User created: " + email + " | role: " + role + " | userId: " + userId);
