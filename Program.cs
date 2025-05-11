using CORE.SECURITY;
using CORE.SECURITY.LOADER;
using CORE.SERVICE;
using CORE.SERVICE.MainLayout;
using CORE.SERVICE.NOTIFICATION;
using FastReport.Data;
using FastReport.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Win32;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});
builder.Services.AddScoped<NotificationMessageService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<PaymentVerificationService>();

// Configure the database context
//builder.Services.AddDbContext<INTELDbContext>(options =>
//{
//    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
//        ?? throw new InvalidOperationException("Sorry, Connection not found"));
//});

// Register HttpClient
builder.Services.AddHttpClient();

//builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.Configuration["BaseAddress"]) });
builder.Services.AddFastReport();

builder.Services.AddMemoryCache();


// Add the UserService registration
builder.Services.AddScoped<AuthService>(provider =>
{
    var memoryCache = provider.GetRequiredService<IMemoryCache>();
    var connectionString = "workstation id=SmssCore.mssql.somee.com;packet size=4096;user id=Smss_SQLLogin_1;pwd=rh5eysynka;data source=SmssCore.mssql.somee.com;persist security info=False;initial catalog=SmssCore;TrustServerCertificate=True;";
    return new AuthService(connectionString, memoryCache);
});

// Register MenuService with the necessary connection string
builder.Services.AddScoped<MenuService>(provider => new MenuService("workstation id=SmssCore.mssql.somee.com;packet size=4096;user id=Smss_SQLLogin_1;pwd=rh5eysynka;data source=SmssCore.mssql.somee.com;persist security info=False;initial catalog=SmssCore;TrustServerCertificate=True;"));

builder.Services.AddScoped<ContentLoaderTemplate>();
builder.Services.AddTransient<UserService>();


//for fastreport
RegisteredObjects.AddConnection(typeof(MsSqlDataConnection));

builder.Services.AddScoped<LoadingComponent>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.UseFastReport(); // Register FastReport
//app.MapFallbackToFile("index.html");

app.Run();