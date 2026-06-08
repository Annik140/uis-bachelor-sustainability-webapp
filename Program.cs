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
            SeedDemoBrands(db);
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

        static void SeedDemoBrands(AppDbContext db)
        {
            if (db.ClothingBrands.Any())
            {
                return;
            }

            var allCriteria = GetDefaultCriteriaTemplate();

            var seededBrands = new List<ClothingBrand>
            {
                BuildBrand(
                    brandName: "Aurora Atelier",
                    category: "Luxury",
                    criteria: FillCriteria(allCriteria, new Dictionary<string, decimal>
                    {
                        ["Material:Fiber traceability"] = 95,
                        ["Labor:Living wage commitment & coverage"] = 90,
                        ["Carbon:Scope 1-3 measurement"] = 92,
                        ["Longevity:Durability Testing / Expected Lifetime"] = 93,
                    }),
                    certifications: ["GOTS", "SBTi"],
                    sourceTitle: "Aurora sustainability brief"
                ),
                BuildBrand(
                    brandName: "Baseline Basics",
                    category: "Fast fashion",
                    criteria: FillCriteria(allCriteria, new Dictionary<string, decimal>
                    {
                        ["Material:Fiber traceability"] = 20,
                        ["Material:Chemical management"] = 15,
                        ["Material:Recycled content / Preferred material content"] = 10,
                        ["Material:Certifications"] = 0,
                        ["Labor:Living wage commitment & coverage"] = 25,
                        ["Labor:Worker safety & working hours"] = 20,
                        ["Labor:Freedom of association / grievance mechanisms"] = 15,
                        ["Labor:Supplier audit transparency"] = 10,
                        ["Carbon:Reduction targets & progress"] = 20,
                        ["Carbon:Renewable energy"] = 15,
                        ["Carbon:Transport & logistics"] = 10,
                        ["Carbon:Scope 1-3 measurement"] = 25,
                        ["Longevity:Durability Testing / Expected Lifetime"] = 20,
                        ["Longevity:Repairability & Repair Services"] = 15,
                        ["Longevity:Circularity Programs"] = 10,
                        ["Longevity:Care Instructions & User Guidance"] = 25,
                    }),
                    certifications: [],
                    sourceTitle: "Baseline annual report"
                ),
                BuildBrand(
                    brandName: "Cedar Collective",
                    category: "Outdoor",
                    criteria: FillCriteria(allCriteria, new Dictionary<string, decimal>
                    {
                        ["Material:Fiber traceability"] = 90,
                        ["Material:Chemical management"] = 85,
                        ["Material:Recycled content / Preferred material content"] = 88,
                        ["Material:Certifications"] = 90,
                        ["Labor:Living wage commitment & coverage"] = 82,
                        ["Labor:Worker safety & working hours"] = 86,
                        ["Labor:Freedom of association / grievance mechanisms"] = 80,
                        ["Labor:Supplier audit transparency"] = 84,
                        ["Carbon:Reduction targets & progress"] = 88,
                        ["Carbon:Renewable energy"] = 86,
                        ["Carbon:Transport & logistics"] = 82,
                        ["Carbon:Scope 1-3 measurement"] = 90,
                        ["Longevity:Durability Testing / Expected Lifetime"] = 92,
                        ["Longevity:Repairability & Repair Services"] = 85,
                        ["Longevity:Circularity Programs"] = 80,
                        ["Longevity:Care Instructions & User Guidance"] = 88,
                    }),
                    certifications: ["bluesign", "Fair Wear Foundation", "SBTi"],
                    sourceTitle: "Cedar impact report"
                ),
                BuildBrand(
                    brandName: "Dusk Discount",
                    category: "Value retail",
                    criteria: FillCriteria(allCriteria, new Dictionary<string, decimal>
                    {
                        ["Material:Chemical management"] = 12,
                        ["Labor:Worker safety & working hours"] = 18,
                        ["Carbon:Transport & logistics"] = 10,
                        ["Longevity:Repairability & Repair Services"] = 8,
                    }),
                    certifications: [],
                    sourceTitle: "Dusk supplier statement"
                ),
                BuildBrand(
                    brandName: "Evergreen Loop",
                    category: "Contemporary",
                    criteria: FillCriteria(allCriteria, new Dictionary<string, decimal>
                    {
                        ["Material:Fiber traceability"] = 60,
                        ["Material:Chemical management"] = 55,
                        ["Material:Recycled content / Preferred material content"] = 58,
                        ["Material:Certifications"] = 50,
                        ["Labor:Living wage commitment & coverage"] = 52,
                        ["Labor:Worker safety & working hours"] = 55,
                        ["Labor:Supplier audit transparency"] = 50,
                        ["Carbon:Reduction targets & progress"] = 60,
                        ["Carbon:Renewable energy"] = 54,
                        ["Longevity:Durability Testing / Expected Lifetime"] = 57,
                    }),
                    certifications: ["GRS"],
                    sourceTitle: "Evergreen responsibility page"
                ),
            };

            db.ClothingBrands.AddRange(seededBrands);
            db.SaveChanges();
        }

        static ClothingBrand BuildBrand(
            string brandName,
            string category,
            List<BrandCriterionItem> criteria,
            IReadOnlyList<string> certifications,
            string sourceTitle)
        {
            var brand = new ClothingBrand
            {
                BrandName = brandName,
                Category = category,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            foreach (var criterion in criteria)
            {
                brand.CriteriaItems.Add(criterion);
            }

            brand.EvidenceSources.Add(new BrandEvidenceSource
            {
                SourceTitle = sourceTitle,
                SourceUrl = "https://example.com/report",
                SourceType = "Report",
                CreatedAtUtc = DateTime.UtcNow,
            });

            foreach (var certification in certifications.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                brand.Certifications.Add(new BrandCertification
                {
                    Name = certification,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            brand.EvidenceSourceCount = brand.EvidenceSources.Count;
            BrandScoreCalculator.NormalizeCriteria(brand);
            BrandScoreCalculator.ApplyScores(brand);
            return brand;
        }

        static List<BrandCriterionItem> GetDefaultCriteriaTemplate()
        {
            return
            [
                new() { Category = "Material", Name = "Fiber traceability", Unit = "%", Weight = 1m },
                new() { Category = "Material", Name = "Chemical management", Weight = 1m },
                new() { Category = "Material", Name = "Recycled content / Preferred material content", Unit = "%", Weight = 1m },
                new() { Category = "Material", Name = "Certifications", Weight = 1m },
                new() { Category = "Labor", Name = "Living wage commitment & coverage", Weight = 1m },
                new() { Category = "Labor", Name = "Worker safety & working hours", Weight = 1m },
                new() { Category = "Labor", Name = "Freedom of association / grievance mechanisms", Weight = 1m },
                new() { Category = "Labor", Name = "Supplier audit transparency", Weight = 1m },
                new() { Category = "Carbon", Name = "Reduction targets & progress", Weight = 1m },
                new() { Category = "Carbon", Name = "Renewable energy", Unit = "%", Weight = 1m },
                new() { Category = "Carbon", Name = "Transport & logistics", Weight = 1m },
                new() { Category = "Carbon", Name = "Scope 1-3 measurement", Weight = 1m },
                new() { Category = "Longevity", Name = "Durability Testing / Expected Lifetime", Weight = 1m },
                new() { Category = "Longevity", Name = "Repairability & Repair Services", Weight = 1m },
                new() { Category = "Longevity", Name = "Circularity Programs", Weight = 1m },
                new() { Category = "Longevity", Name = "Care Instructions & User Guidance", Weight = 1m },
            ];
        }

        static List<BrandCriterionItem> FillCriteria(List<BrandCriterionItem> template, IReadOnlyDictionary<string, decimal> overrides)
        {
            return template.Select(item =>
            {
                var key = $"{item.Category}:{item.Name}";
                overrides.TryGetValue(key, out var value);

                return new BrandCriterionItem
                {
                    Category = item.Category,
                    Name = item.Name,
                    NumericValue = overrides.ContainsKey(key) ? value : null,
                    Unit = item.Unit,
                    Weight = item.Weight,
                    Notes = null,
                    CreatedAtUtc = DateTime.UtcNow,
                };
            }).ToList();
        }
    }
}
