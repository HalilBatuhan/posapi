using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
        _database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
    }

    // Admin koleksiyonu için bağlantı
    public IMongoCollection<Admin> Admins => _database.GetCollection<Admin>("Admins");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("Categories");
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
    public IMongoCollection<Settings> Settings => _database.GetCollection<Settings>("Settings");



}