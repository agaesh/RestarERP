using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Interfaces;
using RestarProduct.Repositories;
using RestarProduct.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RestarProducts")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, RestarProduct.Services.ProductService>();
builder.Services.AddScoped<IRawMaterialRepository, RawMaterialRepository>();
builder.Services.AddScoped<IRawMaterialService, RestarProduct.Services.RawMaterialService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception,
            "Error occurred during {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected error",
            Detail = exception?.Message
        });
    });
});
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program;