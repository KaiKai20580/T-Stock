using MongoDB.Bson;
using MongoDB.Driver;
using T_Stock.Models;

var builder = WebApplication.CreateBuilder(args);

// Add console logging
builder.Logging.AddConsole();

// ----------------------
// MongoDB Setup
// ----------------------
const string connectionUri = "mongodb+srv://t-stock-123:oczgaj7c8lnRa5fr@t-stock.dr8vmsk.mongodb.net/?appName=T-Stock";
var settings = MongoClientSettings.FromConnectionString(connectionUri);
settings.ServerApi = new ServerApi(ServerApiVersion.V1);

var client = new MongoClient(settings);

// Use the T-Stock database
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

builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton<DB>();

// ----------------------
// Add MVC services
// ----------------------
builder.Services.AddControllersWithViews();

// ----------------------
// Build app
// ----------------------
var app = builder.Build();

// ----------------------
// Configure HTTP request pipeline
// ----------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
