var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddScoped<StockWiseAPI.Services.LlmService>();


// Add this CORS policy (important for iOS connection)
var corsPolicy = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseCors(corsPolicy); // enable the policy

app.UseAuthorization();
app.MapControllers();

app.Run();
