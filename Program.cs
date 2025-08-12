using AddCorrectTable.Data;

namespace AddCorrectTable;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        // builder.Logging.AddEventLog(); // <-- Закомментируйте или удалите эту строку
        builder.Logging.AddDebug();
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        //builder.Services.AddSingleton<IDbContext, FirebirdDbContext>();
        //builder.Services.AddScoped<IMaterialService, FirebirdMaterialService>();

        builder.Services.AddScoped<IDbContext, SqlServerDbContext>();
        builder.Services.AddScoped<IMaterialService, SqlServerMaterialService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
        app.Run();
    }
}
