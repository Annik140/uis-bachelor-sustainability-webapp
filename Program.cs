using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using uis_bachelor_sustainability_webapp.Data;
using uis_bachelor_sustainability_webapp.Models;
using uis_bachelor_sustainability_webapp.Services;

namespace uis_bachelor_sustainability_webapp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        // Authentication: cookie-based admin sign-in
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/admin/login";
                options.Cookie.Name = "sustain_admin";
                options.Cookie.HttpOnly = true;
                // Default to Strict, but relax in development so the Vite proxy and cross-port dev setup can persist cookies.
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                if (builder.Environment.IsDevelopment())
                {
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                }
                else
                {
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }
                options.ExpireTimeSpan = TimeSpan.FromHours(4);

                // For XHR/API calls, return 401 instead of redirecting to the login page
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/admin"))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }
                        ctx.Response.Redirect(ctx.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/brands", async (AppDbContext db) =>
            await db.ClothingBrands
                .Include(x => x.EvidenceSources)
                .Include(x => x.CriteriaItems)
                .Include(x => x.Certifications)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync());

        app.MapGet("/brands/{id:int}", async (int id, AppDbContext db) =>
        {
            var brand = await db.ClothingBrands
                .Include(b => b.EvidenceSources)
                .Include(b => b.CriteriaItems)
                .Include(b => b.Certifications)
                .FirstOrDefaultAsync(b => b.Id == id);
            return brand is null ? Results.NotFound() : Results.Ok(brand);
        })
        .WithName("GetBrandById");

        app.MapGet("/admin/clothingbrands", async (AppDbContext db) =>
            await db.ClothingBrands
                .Include(x => x.EvidenceSources)
                .Include(x => x.CriteriaItems)
                .Include(x => x.Certifications)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync())
            .RequireAuthorization("AdminOnly");

        app.MapGet("/admin/clothingbrands/{id:int}", async (int id, AppDbContext db) =>
        {
            var brand = await db.ClothingBrands
                .Include(b => b.EvidenceSources)
                .Include(b => b.CriteriaItems)
                .Include(b => b.Certifications)
                .FirstOrDefaultAsync(b => b.Id == id);
            return brand is null ? Results.NotFound() : Results.Ok(brand);
        })
        .RequireAuthorization("AdminOnly");

        // Admin login endpoint, signs in cookie if credentials match env vars
        app.MapPost("/admin/login", async (HttpContext ctx) =>
        {
            var dto = await ctx.Request.ReadFromJsonAsync<Models.LoginDto>();
            var adminUser = app.Configuration["ADMIN_USER"] ?? Environment.GetEnvironmentVariable("ADMIN_USER");
            var adminPass = app.Configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            if (string.IsNullOrEmpty(adminUser) || string.IsNullOrEmpty(adminPass))
            {
                app.Logger.LogWarning("Admin credentials not configured (ADMIN_USER/ADMIN_PASSWORD)");
                return Results.Problem("Admin credentials not configured on the server.", statusCode: 500);
            }

            if (dto is null || dto.Username != adminUser || dto.Password != adminPass)
            {
                app.Logger.LogWarning("Failed admin login attempt for user {User}", dto?.Username ?? "(null)");
                return Results.Unauthorized();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminUser),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            app.Logger.LogInformation("Admin {User} signed in", adminUser);
            return Results.Ok();
        }).AllowAnonymous();

        app.MapPost("/admin/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        // Admin-protected CRUD endpoints for ClothingBrands
        app.MapPost("/admin/clothingbrands", async (ClothingBrand input, AppDbContext db) =>
        {
            var entity = new ClothingBrand
            {
                BrandName = input.BrandName,
                Category = input.Category,
                MaterialSustainabilityScore = input.MaterialSustainabilityScore,
                LaborPracticesScore = input.LaborPracticesScore,
                CarbonFootprintScore = input.CarbonFootprintScore,
                ProductLongevityScore = input.ProductLongevityScore,
                EvidenceSourceCount = input.EvidenceSourceCount,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            AddEvidenceSources(entity, input);
            AddCriteriaItems(entity, input);
            AddCertifications(entity, input);

            BrandScoreCalculator.ApplyScores(entity);

            db.ClothingBrands.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/brands/{entity.Id}", entity);
        }).RequireAuthorization("AdminOnly");

        app.MapPut("/admin/clothingbrands/{id:int}", async (int id, ClothingBrand input, AppDbContext db) =>
        {
            var existing = await db.ClothingBrands
                .Include(b => b.EvidenceSources)
                .Include(b => b.CriteriaItems)
                .Include(b => b.Certifications)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (existing is null) return Results.NotFound();
            existing.BrandName = input.BrandName;
            existing.Category = input.Category;
            existing.MaterialSustainabilityScore = input.MaterialSustainabilityScore;
            existing.LaborPracticesScore = input.LaborPracticesScore;
            existing.CarbonFootprintScore = input.CarbonFootprintScore;
            existing.ProductLongevityScore = input.ProductLongevityScore;
            existing.EvidenceSourceCount = input.EvidenceSourceCount;
            db.BrandEvidenceSources.RemoveRange(existing.EvidenceSources);
            AddEvidenceSources(existing, input);
            db.BrandCriterionItems.RemoveRange(existing.CriteriaItems);
            AddCriteriaItems(existing, input);
            db.BrandCertifications.RemoveRange(existing.Certifications);
            AddCertifications(existing, input);
            BrandScoreCalculator.ApplyScores(existing);
            await db.SaveChangesAsync();
            return Results.Ok(existing);
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/admin/clothingbrands/{id:int}", async (int id, AppDbContext db) =>
        {
            var existing = await db.ClothingBrands.FindAsync(id);
            if (existing is null) return Results.NotFound();
            db.ClothingBrands.Remove(existing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        app.Run();

        static void AddEvidenceSources(ClothingBrand target, ClothingBrand input)
        {
            if (input.EvidenceSources is null)
            {
                return;
            }

            foreach (var source in input.EvidenceSources.Where(source => !string.IsNullOrWhiteSpace(source.SourceTitle) && !string.IsNullOrWhiteSpace(source.SourceUrl)))
            {
                target.EvidenceSources.Add(new BrandEvidenceSource
                {
                    SourceTitle = source.SourceTitle.Trim(),
                    SourceUrl = source.SourceUrl.Trim(),
                    SourceType = source.SourceType?.Trim(),
                    PublishedAtUtc = source.PublishedAtUtc,
                    Notes = source.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            target.EvidenceSourceCount = Math.Max(target.EvidenceSourceCount, target.EvidenceSources.Count);
        }

        static void AddCriteriaItems(ClothingBrand target, ClothingBrand input)
        {
            if (input.CriteriaItems is null)
            {
                return;
            }

            foreach (var criterion in input.CriteriaItems.Where(criterion => !string.IsNullOrWhiteSpace(criterion.Category) && !string.IsNullOrWhiteSpace(criterion.Name)))
            {
                target.CriteriaItems.Add(new BrandCriterionItem
                {
                    Category = criterion.Category.Trim(),
                    Name = criterion.Name.Trim(),
                    NumericValue = criterion.NumericValue,
                    Unit = criterion.Unit?.Trim(),
                    Weight = criterion.Weight,
                    Notes = criterion.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            BrandScoreCalculator.NormalizeCriteria(target);
        }

        static void AddCertifications(ClothingBrand target, ClothingBrand input)
        {
            if (input.Certifications is null)
            {
                return;
            }

            var uniqueNames = input.Certifications
                .Select(c => c.Name?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in uniqueNames)
            {
                target.Certifications.Add(new BrandCertification
                {
                    Name = name!,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
    }
}
