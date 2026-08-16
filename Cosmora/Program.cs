using Cosmora.Context;
using Cosmora.Context.SeedData;
using Cosmora.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CosmoraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IForecastService, ForecastService>();
builder.Services.AddScoped<IBinaryClassificationService, BinaryClassificationService>();
builder.Services.AddScoped<IMulticlassService, MulticlassService>();
builder.Services.AddScoped<IAnomalyService, AnomalyService>();
builder.Services.AddScoped<IClusterService, ClusterService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();
builder.Services.AddScoped<ISalesChatService, SalesChatService>();


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CosmoraDbContext>();
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;

    DimensionSeeder.Seed(db);
    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        SalesSeeder.Seed(db, connStr);
        sw.Stop();
        Console.WriteLine($"[Seeder] Süre: {sw.Elapsed.TotalSeconds:N1} sn");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[Seeder HATA] " + ex);
    }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
