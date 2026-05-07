using Microsoft.EntityFrameworkCore;
using uis_bachelor_sustainability_webapp.Data;
using uis_bachelor_sustainability_webapp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/brands", async (AppDbContext db) =>
    await db.ClothingBrands
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToListAsync());

app.MapGet("/brands/{id:int}", async (int id, AppDbContext db) =>
{
    var brand = await db.ClothingBrands.FindAsync(id);
    return brand is null ? Results.NotFound() : Results.Ok(brand);
})
.WithName("GetBrandById");

app.MapPost("/brands", async (ClothingBrand input, AppDbContext db) =>
{
    var entity = new ClothingBrand
    {
        BrandName = input.BrandName,
        Category = input.Category,
        SustainabilityScore = input.SustainabilityScore,
        CreatedAtUtc = DateTime.UtcNow
    };

    db.ClothingBrands.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/brands/{entity.Id}", entity);
})
.WithName("CreateBrand");

app.Run();
