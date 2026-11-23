using LubeLogMCP.Helper;
using LubeLogMCP.MCP;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LubeLoggerApiClient>();

var mcpBuilder = builder.Services.AddMcpServer()
    .WithHttpTransport() // With streamable HTTP
    .WithToolsFromAssembly(); // Add all classes marked with [McpServerToolType]

// Delete tools are opt-in: without the flag they don't appear in tools/list at all.
if (string.Equals(builder.Configuration["LUBELOG_ENABLE_DELETE"], "true", StringComparison.OrdinalIgnoreCase))
{
    mcpBuilder.WithTools<LubeLoggerDeleteMCP>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// No HTTPS redirection, HSTS or CORS: this is a server-to-server endpoint and TLS terminates at the reverse proxy.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapMcp("/api/mcp");

app.Run();
