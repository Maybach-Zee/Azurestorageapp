using Azurestorageapp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<BlobService>();
builder.Services.AddSingleton<TableService>();
builder.Services.AddSingleton<CustomerTableService>();
builder.Services.AddSingleton<OrderTableService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<FileService>();

var app = builder.Build();

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
