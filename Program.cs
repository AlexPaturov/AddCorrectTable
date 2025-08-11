using AddCorrectTable.Data;
using AddCorrectTable.Services;

namespace AddCorrectTable;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddScoped<FirebirdDbContext>();
        builder.Services.AddScoped<MaterialService>();

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
