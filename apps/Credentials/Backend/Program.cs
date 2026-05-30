using Backend.Modules;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ModuleHost.RegisterAll(builder);

WebApplication app = builder.Build();
ModuleHost.ConfigureAll(app);
app.Run();

public partial class Program;
