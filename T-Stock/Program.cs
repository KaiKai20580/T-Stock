using MongoDB.Bson;
using MongoDB.Driver;
using T_Stock.Models;
using T_Stock.Helpers;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<MongoPagingService>();

// Add console logging
builder.Logging.AddConsole();

// ----------------------
// MongoDB Setup
// ----------------------
const string connectionUri = "mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock";
var settings = MongoClientSettings.FromConnectionString(connectionUri);
settings.ServerApi = new ServerApi(ServerApiVersion.V1);

var client = new MongoClient(settings);

// Use the T-Stock database (Inventory)
var database = client.GetDatabase("Inventory");

// TEMP logger – works BEFORE Build()
var tempLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
var tempLogger = tempLoggerFactory.CreateLogger("MongoDBTest");

try
{
    var result = database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
    tempLogger.LogInformation("Pinged MongoDB successfully!");
}
catch (Exception ex)
{
    tempLogger.LogError("MongoDB connection failed: " + ex.Message);
}

// Register MongoDB services
builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton<DB>();
builder.Services.AddSingleton<IMongoClient>(s =>new MongoClient("mongodb://localhost:5275"));

// ----------------------
// Add MVC services
// ----------------------
builder.Services.AddControllersWithViews();

// ----------------------
// Add SESSION (required for login)
// ----------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
});

// ----------------------
// Build app
// ----------------------
var app = builder.Build();

// ----------------------
// Configure middleware
// ----------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable Session BEFORE Authorization
app.UseSession();

app.UseAuthorization();

// ----------------------
// Default Route
// ----------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
