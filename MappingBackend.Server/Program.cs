using KarttaBackEnd2.Server.Data;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ReactApp2.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register dependencies for both implementations
            RegisterServices(builder.Services);

            var cultureInfo = new CultureInfo("fi-fi");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            
            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });

            var app = builder.Build();

            app.UseCors("AllowAll");
            app.UseDefaultFiles();

            app.UseHttpsRedirection();
            app.MapControllers();
            app.MapFallbackToFile("/index.html");

            app.Run();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            // STEP 2: Register both implementations during the transition period
            // Legacy service
            services.AddScoped<WaypointService>();
            
            // New service components
            services.AddScoped<IGeometryService, GeometryService>();
            services.AddScoped<IShapeService, RectangleShapeService>();
            services.AddScoped<IShapeService, PolygonShapeService>();
            services.AddScoped<IShapeService, CircleShapeService>();
            services.AddScoped<IShapeService, PolylineShapeService>();
            services.AddScoped<WaypointServiceV2>();
            
            // Use the adapter as the implementation of IWaypointService
            services.AddScoped<IWaypointService, WaypointServiceAdapter>();
            
            // Other services
            services.AddScoped<IKMZService, KMZService>();
        }
    }
}
