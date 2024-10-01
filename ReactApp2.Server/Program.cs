using KarttaBackEnd2.Server.Data;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

namespace ReactApp2.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddScoped<WaypointService>();
            builder.Services.AddScoped<IKMZService, KMZService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             .AddJwtBearer(options =>
             {
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidateLifetime = true,
                     ValidateIssuerSigningKey = true,
                     ValidIssuer = builder.Configuration["Jwt:Issuer"],
                     ValidAudience = builder.Configuration["Jwt:Audience"],
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                 };
             });
            var cultureInfo = new CultureInfo("fi-fi");  // Vaihtoehtoisesti valitse sopiva kulttuuri
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()   // Salli kaikki alkuperät (kaikki domainit ja portit)
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
            });
            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();
            app.UseCors("AllowAll");
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
