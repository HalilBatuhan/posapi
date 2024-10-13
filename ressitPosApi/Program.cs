using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// CORS'u ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Diğer servisler
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(s => 
    new MongoClient(builder.Configuration.GetSection("MongoDbSettings")["ConnectionString"]));
builder.Services.AddSingleton<MongoDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // CORS Middleware'ini burada etkinleştir
app.UseHttpsRedirection();

// Admin CRUD API
app.MapPost("/login", async (LoginRequest loginRequest, MongoDbContext dbContext) =>
{
    // Kullanıcı adı ve şifre ile kullanıcıyı bul
    var user = await dbContext.Admins
        .Find(x => x.Username == loginRequest.Username && x.PasswordHash == loginRequest.PasswordHash)
        .FirstOrDefaultAsync();

    // Kullanıcı varsa bilgilerini döndür
    if (user != null)
    {
        return Results.Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.RestaurantName,
            user.Username
        });
    }
    else
    {
        // Kullanıcı bulunamazsa hata mesajı döndür
        return Results.Unauthorized();
    }
}).WithOpenApi();

app.MapPost("/createAdmin", async (Admin admin, MongoDbContext dbContext) =>
{
    var maxAdmin = await dbContext.Admins.Find(_ => true)
        .SortByDescending(a => a.Id)
        .FirstOrDefaultAsync();

    admin.Id = (maxAdmin != null ? maxAdmin.Id + 1 : 1);

    await dbContext.Admins.InsertOneAsync(admin);
    return Results.Created($"/createAdmin/{admin.Id}", admin);
}).WithOpenApi();

app.MapGet("/getAllAdmins", async (MongoDbContext dbContext) =>
{
    var admins = await dbContext.Admins.Find(_ => true).ToListAsync();
    return Results.Ok(admins);
}).WithOpenApi();

app.MapGet("/getAdmin/{id}", async (int id, MongoDbContext dbContext) =>
{
    var admin = await dbContext.Admins.Find(x => x.Id == id).FirstOrDefaultAsync();
    return admin is not null ? Results.Ok(admin) : Results.NotFound();
}).WithOpenApi();

app.MapPut("/updateAdmin/{id}", async (int id, Admin updatedAdmin, MongoDbContext dbContext) =>
{
    var result = await dbContext.Admins.ReplaceOneAsync(x => x.Id == id, updatedAdmin);
    return result.ModifiedCount > 0 ? Results.Ok(updatedAdmin) : Results.NotFound();
}).WithOpenApi();

app.MapDelete("/deleteAdmin/{id}", async (int id, MongoDbContext dbContext) =>
{
    var result = await dbContext.Admins.DeleteOneAsync(x => x.Id == id);
    return result.DeletedCount > 0 ? Results.Ok() : Results.NotFound();
}).WithOpenApi();

// Category CRUD API
app.MapPost("/createCategory", async (Category category, MongoDbContext dbContext) =>
{
    var maxCategory = await dbContext.Categories.Find(_ => true)
        .SortByDescending(c => c.Id)
        .FirstOrDefaultAsync();

    category.Id = (maxCategory != null ? maxCategory.Id + 1 : 1);

    await dbContext.Categories.InsertOneAsync(category);
    return Results.Created($"/createCategory/{category.Id}", category);
}).WithOpenApi();

app.MapGet("/getAllCategories", async (MongoDbContext dbContext) =>
{
    var categories = await dbContext.Categories.Find(_ => true).ToListAsync();
    return Results.Ok(categories);
}).WithOpenApi();

app.MapGet("/getCategory/{id}", async (int id, MongoDbContext dbContext) =>
{
    var category = await dbContext.Categories.Find(x => x.Id == id).FirstOrDefaultAsync();
    return category is not null ? Results.Ok(category) : Results.NotFound();
}).WithOpenApi();

app.MapPut("/updateCategory/{id}", async (int id, Category updatedCategory, MongoDbContext dbContext) =>
{
    var result = await dbContext.Categories.ReplaceOneAsync(x => x.Id == id, updatedCategory);
    return result.ModifiedCount > 0 ? Results.Ok(updatedCategory) : Results.NotFound();
}).WithOpenApi();

app.MapDelete("/deleteCategory/{id}", async (int id, MongoDbContext dbContext) =>
{
    var result = await dbContext.Categories.DeleteOneAsync(x => x.Id == id);
    return result.DeletedCount > 0 ? Results.Ok() : Results.NotFound();
}).WithOpenApi();

// Product CRUD API
app.MapPost("/createProduct", async (Product product, MongoDbContext dbContext) =>
{
    var maxProduct = await dbContext.Products.Find(_ => true)
        .SortByDescending(p => p.Id)
        .FirstOrDefaultAsync();

    product.Id = (maxProduct != null ? maxProduct.Id + 1 : 1);

    if (product.HasVariations && product.Variations != null)
    {
        int variationId = 1;
        foreach (var variation in product.Variations)
        {
            variation.Id = variationId++;
        }
    }

    await dbContext.Products.InsertOneAsync(product);
    return Results.Created($"/createProduct/{product.Id}", product);
}).WithOpenApi();

app.MapGet("/getAllProducts", async (MongoDbContext dbContext) =>
{
    var products = await dbContext.Products.Find(_ => true).ToListAsync();
    return Results.Ok(products);
}).WithOpenApi();

app.MapGet("/getProduct/{id}", async (int id, MongoDbContext dbContext) =>
{
    var product = await dbContext.Products.Find(x => x.Id == id).FirstOrDefaultAsync();
    return product is not null ? Results.Ok(product) : Results.NotFound();
}).WithOpenApi();

app.MapPut("/updateProduct/{id}", async (int id, Product updatedProduct, MongoDbContext dbContext) =>
{
    var result = await dbContext.Products.ReplaceOneAsync(x => x.Id == id, updatedProduct);
    return result.ModifiedCount > 0 ? Results.Ok(updatedProduct) : Results.NotFound();
}).WithOpenApi();

app.MapDelete("/deleteProduct/{id}", async (int id, MongoDbContext dbContext) =>
{
    var result = await dbContext.Products.DeleteOneAsync(x => x.Id == id);
    return result.DeletedCount > 0 ? Results.Ok() : Results.NotFound();
}).WithOpenApi();

// Order CRUD API
app.MapPost("/createOrder", async (Order order, MongoDbContext dbContext) =>
{
    var maxOrder = await dbContext.Orders.Find(_ => true)
        .SortByDescending(o => o.Id)
        .FirstOrDefaultAsync();

    order.Id = (maxOrder != null ? maxOrder.Id + 1 : 1);

    order.OrderDate = DateTime.ParseExact(order.OrderDateString, "yyyy-MM-dd / HH:mm", null);

    if (order.HasVariations && order.Variations != null)
    {
        int variationId = 1;
        foreach (var variation in order.Variations)
        {
            variation.Id = variationId++;
        }
    }

    await dbContext.Orders.InsertOneAsync(order);
    return Results.Created($"/createOrder/{order.Id}", order);
}).WithOpenApi();

app.MapGet("/getAllOrders", async (MongoDbContext dbContext) =>
{
    var orders = await dbContext.Orders.Find(_ => true).ToListAsync();
    return Results.Ok(orders);
}).WithOpenApi();

// Settings CRUD API
app.MapPost("/createSettings", async (Settings settings, MongoDbContext dbContext) =>
{
    var maxSettings = await dbContext.Settings.Find(_ => true)
        .SortByDescending(s => s.Id)
        .FirstOrDefaultAsync();

    settings.Id = (maxSettings != null ? maxSettings.Id + 1 : 1);

    await dbContext.Settings.InsertOneAsync(settings);
    return Results.Created($"/createSettings/{settings.Id}", settings);
}).WithOpenApi();

app.MapGet("/getSettings", async (MongoDbContext dbContext) =>
{
    var settings = await dbContext.Settings.Find(_ => true).FirstOrDefaultAsync();
    return Results.Ok(settings);
}).WithOpenApi();

// Z Report API
app.MapPost("/generateZReport", async (DateTime reportDate, MongoDbContext dbContext) =>
{
    var startTime = reportDate.Date;
    var endTime = reportDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

    var orders = await dbContext.Orders.Find(o => o.OrderDate >= startTime && o.OrderDate <= endTime).ToListAsync();

    var taxGroup26 = orders.Where(o => o.TaxValue == 2.6m).ToList(); // 2.6% KDV oranlı siparişler
    var taxGroup81 = orders.Where(o => o.TaxValue == 8.1m).ToList(); // 8.1% KDV oranlı siparişler

    int totalOrders = orders.Count;

    decimal totalAmount26 = taxGroup26.Sum(o => o.TotalPrice);
    decimal netto26 = totalAmount26 / (1 + 0.026m);
    decimal mwst26 = totalAmount26 - netto26;

    decimal totalAmount81 = taxGroup81.Sum(o => o.TotalPrice);
    decimal netto81 = totalAmount81 / (1 + 0.081m);
    decimal mwst81 = totalAmount81 - netto81;

    decimal totalSales = orders.Sum(o => o.TotalPrice);

    var categoryGroups = orders.GroupBy(o => o.CategoryName)
        .Select(g => new
        {
            CategoryName = g.Key,
            TotalOrders = g.Count(),
            TotalAmount = g.Sum(o => o.TotalPrice)
        }).ToList();

    var zReport = new
    {
        GünSonuRaporu = new
        {
            Başlangıç = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Bitiş = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ToplamSatış = new
            {
                SiparişSayısı = totalOrders,
                Tutar = $"{totalSales:0.00} TL"
            },
            AnaGruplar = new
            {
                Yiyecek = new
                {
                    SiparişSayısı = totalOrders,
                    Tutar = $"{totalSales:0.00} TL"
                }
            },
            Vergiler = new
            {
                KDV81 = new
                {
                    Oran = "8.1% KDV A",
                    Brüt = $"{totalAmount81:0.00} TL",
                    Net = $"{netto81:0.00} TL",
                    KDV = $"{mwst81:0.00} TL"
                },
                KDV26 = new
                {
                    Oran = "2.6% KDV B",
                    Brüt = $"{totalAmount26:0.00} TL",
                    Net = $"{netto26:0.00} TL",
                    KDV = $"{mwst26:0.00} TL"
                }
            }
        }
    };

    return Results.Ok(zReport);
}).WithOpenApi();

app.Run();

// Models
public record Admin
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string RestaurantName { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
}

public record Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
}

public record Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal InStoreTaxRate { get; set; }
    public decimal OutStoreTaxRate { get; set; }
    public int CategoryId { get; set; }
    public bool HasVariations { get; set; }
    public List<Variation> Variations { get; set; }
}

public record Variation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public record Order
{
    public int Id { get; set; }
    public string CategoryName { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TaxFreePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal NetPrice { get; set; }
    public decimal GrossPrice { get; set; }
    public string PaymentType { get; set; }
    public bool HasVariations { get; set; }
    public List<Variation> Variations { get; set; }
    public string OrderDateString { get; set; }  
    public DateTime OrderDate { get; set; }  
    public decimal TaxValue { get; set; }
}

public record Settings
{
    public int Id { get; set; }
    public string ReportPassword { get; set; }
}
public record LoginRequest
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
}