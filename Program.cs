using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.RegularExpressions;
using uis_bachelor_sustainability_webapp.Data;
using uis_bachelor_sustainability_webapp.Models;
using uis_bachelor_sustainability_webapp.Services;

namespace uis_bachelor_sustainability_webapp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isLocalLikeEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "sustain_csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = isLocalLikeEnvironment
                ? SameSiteMode.Lax
                : SameSiteMode.Strict;
            options.Cookie.SecurePolicy = isLocalLikeEnvironment
                ? CookieSecurePolicy.None
                : CookieSecurePolicy.Always;
        });
        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("app-testing"));
        }
        else
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
        builder.Services.AddScoped<IDbCommitter, DefaultDbCommitter>();
        builder.Services.AddSingleton<ILogoFileOperations, DefaultLogoFileOperations>();
        builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("AdminLoginPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
        // Authentication: cookie-based admin sign-in
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/admin/login";
                options.Cookie.Name = "sustain_admin";
                options.Cookie.HttpOnly = true;
                // Default to Strict, but relax in development so the Vite proxy and cross-port dev setup can persist cookies.
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                if (isLocalLikeEnvironment)
                {
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                }
                else
                {
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
                options.SlidingExpiration = true;

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
        ApplicationDataInitializer.Initialize(app);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseExceptionHandler();
            app.UseHsts();
        }

        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (ctx, next) =>
        {
            if (IsAdminStateChangingRequest(ctx.Request) &&
                !ctx.Request.Path.StartsWithSegments("/admin/login", StringComparison.OrdinalIgnoreCase))
            {
                var antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>();
                try
                {
                    await antiforgery.ValidateRequestAsync(ctx);
                }
                catch (AntiforgeryValidationException)
                {
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        message = "Invalid or missing CSRF token."
                    });
                    return;
                }
            }

            await next();
        });

        app.MapGet("/brands", async (AppDbContext db, int page = 1, int pageSize = 12, string? q = null, string? sort = "lastUpdatedDesc") =>
            Results.Ok(await GetPagedBrands(db, page, pageSize, q, sort)));

        app.MapGet("/brands/{id:int}", async (int id, AppDbContext db) =>
        {
            var brand = await db.ClothingBrands
                .Include(b => b.EvidenceSources)
                .Include(b => b.CriteriaItems)
                .Include(b => b.Certifications)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (brand is not null)
            {
                RefreshBrandScores(brand);
            }
            return brand is null ? Results.NotFound() : Results.Ok(brand);
        })
        .WithName("GetBrandById");

        app.MapGet("/admin/clothingbrands", async (AppDbContext db, int page = 1, int pageSize = 12, string? q = null, string? sort = "lastUpdatedDesc") =>
            Results.Ok(await GetPagedBrands(db, page, pageSize, q, sort)))
            .RequireAuthorization("AdminOnly");

        app.MapGet("/admin/clothingbrands/{id:int}", async (int id, AppDbContext db) =>
        {
            var brand = await db.ClothingBrands
                .Include(b => b.EvidenceSources)
                .Include(b => b.CriteriaItems)
                .Include(b => b.Certifications)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (brand is not null)
            {
                RefreshBrandScores(brand);
            }
            return brand is null ? Results.NotFound() : Results.Ok(brand);
        })
        .RequireAuthorization("AdminOnly");

        app.MapGet("/admin/csrf-token", (HttpContext ctx, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            return Results.Ok(new
            {
                token = tokens.RequestToken
            });
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/session", (HttpContext ctx) =>
        {
            return Results.Ok(new
            {
                authenticated = true,
                username = ctx.User.Identity?.Name
            });
        }).RequireAuthorization("AdminOnly");

        // Admin login endpoint, signs in cookie if credentials match env vars
        app.MapPost("/admin/login", async (HttpContext ctx, AppDbContext db, IPasswordHasher<AdminUser> passwordHasher) =>
        {
            if (!IsTrustedLoginRequest(ctx.Request, isLocalLikeEnvironment))
            {
                app.Logger.LogWarning(
                    "Blocked admin login attempt due to untrusted request origin. Host: {Host}, Origin: {Origin}, Referer: {Referer}",
                    ctx.Request.Host.Value,
                    ctx.Request.Headers.Origin.ToString(),
                    ctx.Request.Headers.Referer.ToString());
                return Results.BadRequest(new { message = "Untrusted login request origin." });
            }

            var dto = await ctx.Request.ReadFromJsonAsync<Models.LoginDto>();
            var username = dto?.Username?.Trim();
            var password = dto?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Results.Unauthorized();
            }

            var normalizedUsername = NormalizeUsername(username);
            var adminUser = await db.AdminUsers.FirstOrDefaultAsync(u =>
                u.NormalizedUsername == normalizedUsername && u.IsActive);

            if (adminUser is null)
            {
                app.Logger.LogWarning("Failed admin login attempt for user {User}", username);
                return Results.Unauthorized();
            }

            var verification = passwordHasher.VerifyHashedPassword(adminUser, adminUser.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                app.Logger.LogWarning("Failed admin login attempt for user {User}", username);
                return Results.Unauthorized();
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, password);
            }

            adminUser.LastLoginAtUtc = DateTime.UtcNow;
            adminUser.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                new Claim(ClaimTypes.Name, adminUser.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            app.Logger.LogInformation("Admin {User} signed in", adminUser.Username);
            return Results.Ok();
        })
        .RequireRateLimiting("AdminLoginPolicy")
        .AllowAnonymous();

        app.MapPost("/admin/users", async (Models.CreateAdminUserDto dto, AppDbContext db, IPasswordHasher<AdminUser> passwordHasher) =>
        {
            var username = dto.Username?.Trim();
            var password = dto.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || password.Length < 12)
            {
                return Results.BadRequest(new
                {
                    message = "Username is required and password must be at least 12 characters."
                });
            }

            var normalizedUsername = NormalizeUsername(username);
            var exists = await db.AdminUsers.AnyAsync(user => user.NormalizedUsername == normalizedUsername);
            if (exists)
            {
                return Results.Conflict(new
                {
                    message = "An admin with that username already exists."
                });
            }

            var adminUser = new AdminUser
            {
                Username = username,
                NormalizedUsername = normalizedUsername,
                PasswordHash = string.Empty,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, password);

            db.AdminUsers.Add(adminUser);
            await db.SaveChangesAsync();

            return Results.Created($"/admin/users/{adminUser.Id}", new
            {
                adminUser.Id,
                adminUser.Username,
                adminUser.IsActive,
                adminUser.CreatedAtUtc
            });
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/upload-logo", async (HttpRequest request, ILogoFileOperations fileOperations) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { message = "Expected multipart/form-data." });
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "No file was uploaded." });
            }

            const long maxBytes = 3 * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return Results.BadRequest(new { message = "Logo must be 3MB or smaller." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".webp"
            };
            if (!allowedExtensions.Contains(extension))
            {
                return Results.BadRequest(new { message = "Unsupported image format. Use PNG, JPG, JPEG, or WEBP." });
            }

            if (!HasValidImageSignature(file, extension))
            {
                return Results.BadRequest(new { message = "File content does not match the declared image format." });
            }

            var logosDirectory = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "brand-logos");
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(logosDirectory, fileName);
            try
            {
                fileOperations.EnsureDirectory(logosDirectory);
                await using (var stream = fileOperations.CreateWriteStream(fullPath))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (IOException)
            {
                return Results.Json(new { message = "Failed to save logo. Disk space may be full or file permissions insufficient." }, statusCode: StatusCodes.Status507InsufficientStorage);
            }
            catch (Exception)
            {
                return Results.Json(new { message = "An error occurred while uploading the logo." }, statusCode: StatusCodes.Status500InternalServerError);
            }

            var logoPath = $"/brand-logos/{fileName}";
            return Results.Ok(new { logoPath });
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/admin/upload-logo", (string logoPath, ILogoFileOperations fileOperations) =>
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return Results.BadRequest(new { message = "logoPath parameter is required." });
            }
            DeleteLogoFile(app, logoPath, fileOperations);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // Admin-protected CRUD endpoints for ClothingBrands
        app.MapPost("/admin/clothingbrands", async (BrandUpsertDto input, AppDbContext db, IDbCommitter dbCommitter, ILogger<Program> logger) =>
        {
            var validationErrors = ValidateBrandInput(input);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            try
            {
                var entity = new ClothingBrand
                {
                    BrandName = input.BrandName.Trim(),
                    LogoPath = input.LogoPath?.Trim(),
                    Description = input.Description?.Trim(),
                    EvidenceSourceCount = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                AddEvidenceSources(entity, input);
                AddCriteriaItems(entity, input);
                AddCertifications(entity, input);

                BrandScoreCalculator.ApplyScores(entity);

                db.ClothingBrands.Add(entity);
                await dbCommitter.CommitAsync(db);
                return Results.Created($"/brands/{entity.Id}", entity);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while creating brand: {BrandName}", input.BrandName);
                return Results.Json(new { message = "Failed to create brand. Please try again." }, statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating brand: {BrandName}", input.BrandName);
                return Results.Json(new { message = "An unexpected error occurred." }, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("AdminOnly");

        app.MapPut("/admin/clothingbrands/{id:int}", async (int id, BrandUpsertDto input, AppDbContext db, IDbCommitter dbCommitter, ILogoFileOperations fileOperations, ILogger<Program> logger) =>
        {
            var validationErrors = ValidateBrandInput(input);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            try
            {
                var existing = await db.ClothingBrands
                    .Include(b => b.EvidenceSources)
                    .Include(b => b.CriteriaItems)
                    .Include(b => b.Certifications)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (existing is null) return Results.NotFound();
                var previousLogoPath = existing.LogoPath?.Trim();
                existing.BrandName = input.BrandName.Trim();
                existing.LogoPath = input.LogoPath?.Trim();
                existing.Description = input.Description?.Trim();
                existing.EvidenceSourceCount = 0;
                db.BrandEvidenceSources.RemoveRange(existing.EvidenceSources);
                AddEvidenceSources(existing, input);
                db.BrandCriterionItems.RemoveRange(existing.CriteriaItems);
                AddCriteriaItems(existing, input);
                db.BrandCertifications.RemoveRange(existing.Certifications);
                AddCertifications(existing, input);
                BrandScoreCalculator.ApplyScores(existing);
                await dbCommitter.CommitAsync(db);
                var nextLogoPath = existing.LogoPath?.Trim();
                if (!string.IsNullOrWhiteSpace(previousLogoPath) && !string.Equals(previousLogoPath, nextLogoPath, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteLogoFile(app, previousLogoPath, fileOperations);
                }
                return Results.Ok(existing);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while updating brand with id {BrandId}", id);
                return Results.Json(new { message = "Failed to update brand. Please try again." }, statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while updating brand with id {BrandId}", id);
                return Results.Json(new { message = "An unexpected error occurred." }, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/admin/clothingbrands/{id:int}", async (int id, AppDbContext db, IDbCommitter dbCommitter, ILogoFileOperations fileOperations, ILogger<Program> logger) =>
        {
            try
            {
                var existing = await db.ClothingBrands.FindAsync(id);
                if (existing is null) return Results.NotFound();
                var logoPath = existing.LogoPath;
                db.ClothingBrands.Remove(existing);
                await dbCommitter.CommitAsync(db);
                DeleteLogoFile(app, logoPath, fileOperations);
                return Results.NoContent();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while deleting brand with id {BrandId}", id);
                return Results.Json(new { message = "Failed to delete brand. Please try again." }, statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while deleting brand with id {BrandId}", id);
                return Results.Json(new { message = "An unexpected error occurred." }, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("AdminOnly");

        app.Run();

        static string NormalizeUsername(string username)
        {
            return username.Trim().ToUpperInvariant();
        }

        static bool IsTrustedLoginRequest(HttpRequest request, bool allowLocalLike)
        {
            if (allowLocalLike)
            {
                return true;
            }

            if (TryParseHeaderUri(request.Headers.Origin.ToString(), out var originUri))
            {
                return IsSameHost(originUri!, request.Host);
            }

            if (TryParseHeaderUri(request.Headers.Referer.ToString(), out var refererUri))
            {
                return IsSameHost(refererUri!, request.Host);
            }

            return false;
        }

        static bool TryParseHeaderUri(string value, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Uri.TryCreate(value, UriKind.Absolute, out uri);
        }

        static bool IsSameHost(Uri uri, HostString host)
        {
            if (!host.HasValue)
            {
                return false;
            }

            if (!string.Equals(uri.Host, host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (host.Port.HasValue)
            {
                return uri.Port == host.Port.Value;
            }

            return uri.IsDefaultPort;
        }

        static bool HasValidImageSignature(IFormFile file, string extension)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[12];
            var bytesRead = stream.Read(header);

            return extension.ToLowerInvariant() switch
            {
                ".png" => bytesRead >= 8 &&
                          header[0] == 0x89 &&
                          header[1] == 0x50 &&
                          header[2] == 0x4E &&
                          header[3] == 0x47 &&
                          header[4] == 0x0D &&
                          header[5] == 0x0A &&
                          header[6] == 0x1A &&
                          header[7] == 0x0A,
                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                       header[0] == 0xFF &&
                                       header[1] == 0xD8 &&
                                       header[2] == 0xFF,
                ".webp" => bytesRead >= 12 &&
                           header[0] == (byte)'R' &&
                           header[1] == (byte)'I' &&
                           header[2] == (byte)'F' &&
                           header[3] == (byte)'F' &&
                           header[8] == (byte)'W' &&
                           header[9] == (byte)'E' &&
                           header[10] == (byte)'B' &&
                           header[11] == (byte)'P',
                _ => false,
            };
        }

        static bool IsAdminStateChangingRequest(HttpRequest request)
        {
            if (!request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return HttpMethods.IsPost(request.Method) ||
                   HttpMethods.IsPut(request.Method) ||
                   HttpMethods.IsDelete(request.Method) ||
                   HttpMethods.IsPatch(request.Method);
        }

        static async Task<PagedResult<ClothingBrand>> GetPagedBrands(AppDbContext db, int page, int pageSize, string? searchQuery, string? sort)
        {
            var safePageSize = Math.Clamp(pageSize, 1, 100);
            var normalizedPage = Math.Max(page, 1);
            var normalizedQuery = searchQuery?.Trim();

            var filtered = db.ClothingBrands.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var query = normalizedQuery.ToLower();
                filtered = filtered.Where(brand => brand.BrandName.ToLower().Contains(query));
            }

            var totalCount = await filtered.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)safePageSize));
            var safePage = Math.Min(normalizedPage, totalPages);

            var ordered = ApplyBrandSorting(filtered, sort);
            var items = await ordered
                .Include(brand => brand.EvidenceSources)
                .Include(brand => brand.CriteriaItems)
                .Include(brand => brand.Certifications)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync();

            foreach (var brand in items)
            {
                RefreshBrandScores(brand);
            }

            return new PagedResult<ClothingBrand>
            {
                Items = items,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
            };
        }

        static IQueryable<ClothingBrand> ApplyBrandSorting(IQueryable<ClothingBrand> query, string? sort)
        {
            return (sort ?? "lastUpdatedDesc").ToLowerInvariant() switch
            {
                "sustainabilitydesc" => query
                    .OrderBy(brand => brand.SustainabilityScore == null)
                    .ThenByDescending(brand => brand.SustainabilityScore)
                    .ThenBy(brand => brand.BrandName),
                "transparencydesc" => query
                    .OrderByDescending(brand => brand.TransparencyScore)
                    .ThenBy(brand => brand.SustainabilityScore == null)
                    .ThenByDescending(brand => brand.SustainabilityScore)
                    .ThenBy(brand => brand.BrandName),
                "alphabeticalasc" => query.OrderBy(brand => brand.BrandName),
                _ => query.OrderByDescending(brand => brand.UpdatedAtUtc).ThenBy(brand => brand.BrandName),
            };
        }

        static void RefreshBrandScores(ClothingBrand brand)
        {
            BrandScoreCalculator.NormalizeCriteria(brand);
            BrandScoreCalculator.ApplyScores(brand);
        }

        static Dictionary<string, string[]> ValidateBrandInput(BrandUpsertDto input)
        {
            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            static void AddError(Dictionary<string, string[]> map, string key, string message)
            {
                if (map.TryGetValue(key, out var existing))
                {
                    map[key] = [.. existing, message];
                }
                else
                {
                    map[key] = [message];
                }
            }

            if (string.IsNullOrWhiteSpace(input.BrandName))
            {
                AddError(errors, nameof(input.BrandName), "BrandName is required.");
            }
            else if (input.BrandName.Trim().Length > 200)
            {
                AddError(errors, nameof(input.BrandName), "BrandName must be at most 200 characters.");
            }

            if (!string.IsNullOrWhiteSpace(input.LogoPath))
            {
                var logoPath = input.LogoPath.Trim();
                if (logoPath.Length > 300)
                {
                    AddError(errors, nameof(input.LogoPath), "LogoPath must be at most 300 characters.");
                }

                if (!Regex.IsMatch(logoPath, "^/brand-logos/[a-zA-Z0-9_-]+\\.(png|jpg|jpeg|webp)$", RegexOptions.IgnoreCase))
                {
                    AddError(errors, nameof(input.LogoPath), "LogoPath must point to an uploaded image under /brand-logos.");
                }
            }

            if (!string.IsNullOrWhiteSpace(input.Description) && input.Description.Trim().Length > 1000)
            {
                AddError(errors, nameof(input.Description), "Description must be at most 1000 characters.");
            }

            foreach (var source in input.EvidenceSources ?? [])
            {
                if (string.IsNullOrWhiteSpace(source.SourceTitle) || source.SourceTitle.Trim().Length > 250)
                {
                    AddError(errors, nameof(input.EvidenceSources), "Each evidence source title is required and must be at most 250 characters.");
                }

                if (string.IsNullOrWhiteSpace(source.SourceUrl) || source.SourceUrl.Trim().Length > 1000)
                {
                    AddError(errors, nameof(input.EvidenceSources), "Each evidence source URL is required and must be at most 1000 characters.");
                }
                else if (!Uri.TryCreate(source.SourceUrl.Trim(), UriKind.Absolute, out var uri) ||
                         (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    AddError(errors, nameof(input.EvidenceSources), "Evidence source URLs must be absolute http/https links.");
                }
            }

            foreach (var item in input.CriteriaItems ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Category) || item.Category.Trim().Length > 80)
                {
                    AddError(errors, nameof(input.CriteriaItems), "Each criterion category is required and must be at most 80 characters.");
                }

                if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 200)
                {
                    AddError(errors, nameof(input.CriteriaItems), "Each criterion name is required and must be at most 200 characters.");
                }

                if (item.NumericValue.HasValue && (item.NumericValue.Value < 0m || item.NumericValue.Value > 100m))
                {
                    AddError(errors, nameof(input.CriteriaItems), "Criterion numeric values must be in range 0 to 100.");
                }

                if (item.Weight.HasValue && (item.Weight.Value < 0.1m || item.Weight.Value > 10m))
                {
                    AddError(errors, nameof(input.CriteriaItems), "Criterion weights must be in range 0.1 to 10.");
                }
            }

            foreach (var certification in input.Certifications ?? [])
            {
                if (string.IsNullOrWhiteSpace(certification.Name) || certification.Name.Trim().Length > 120)
                {
                    AddError(errors, nameof(input.Certifications), "Each certification name is required and must be at most 120 characters.");
                }
            }

            return errors;
        }

        static void AddEvidenceSources(ClothingBrand target, BrandUpsertDto input)
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

        static void AddCriteriaItems(ClothingBrand target, BrandUpsertDto input)
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
                    Weight = criterion.Weight ?? 1m,
                    Notes = criterion.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            BrandScoreCalculator.NormalizeCriteria(target);
        }

        static void AddCertifications(ClothingBrand target, BrandUpsertDto input)
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

        static void DeleteLogoFile(WebApplication app, string? logoPath, ILogoFileOperations fileOperations)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return;
            }

            if (!Regex.IsMatch(logoPath, @"^/brand-logos/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$", RegexOptions.IgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(logoPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var logosDirectory = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "brand-logos");
            var fullPath = Path.Combine(logosDirectory, fileName);
            if (fileOperations.FileExists(fullPath))
            {
                try
                {
                    fileOperations.DeleteFile(fullPath);
                }
                catch (IOException ex)
                {
                    app.Logger.LogWarning(ex, "Failed to delete logo file: {FilePath}", fullPath);
                }
                catch (Exception ex)
                {
                    app.Logger.LogWarning(ex, "Unexpected error deleting logo file: {FilePath}", fullPath);
                }
            }
        }
    }
}

public interface IDbCommitter
{
    Task CommitAsync(AppDbContext db, CancellationToken cancellationToken = default);
}

public sealed class DefaultDbCommitter : IDbCommitter
{
    public async Task CommitAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface ILogoFileOperations
{
    void EnsureDirectory(string path);
    Stream CreateWriteStream(string fullPath);
    bool FileExists(string fullPath);
    void DeleteFile(string fullPath);
}

public sealed class DefaultLogoFileOperations : ILogoFileOperations
{
    public void EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public Stream CreateWriteStream(string fullPath)
    {
        return File.Create(fullPath);
    }

    public bool FileExists(string fullPath)
    {
        return File.Exists(fullPath);
    }

    public void DeleteFile(string fullPath)
    {
        File.Delete(fullPath);
    }
}
