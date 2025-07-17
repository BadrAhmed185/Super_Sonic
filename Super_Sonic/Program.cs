namespace Super_Sonic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Register Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Smart port handling: only override if running inside Docker
            //if (builder.Environment.IsProduction())
            //{
            //    var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
            //    builder.WebHost.UseUrls($"http://*:{port}");
            //}
            var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
            builder.WebHost.UseUrls($"http://*:{port}");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            // Always enable Swagger for testing (optional: restrict to development)
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
