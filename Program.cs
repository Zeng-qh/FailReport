using FailReport.Upload;

namespace FailReport
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // 注册文件上传服务
            builder.Services.AddScoped<IExcelUploadService, ExcelUploadService>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "CorsPolicy",
                                 builder =>
                                 builder.AllowAnyOrigin()
                                 .AllowAnyMethod()
                                 .AllowAnyHeader()
                                 );
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("CorsPolicy");  // 跨域

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseFileServer();
            //app.UseStaticFiles();
            //app.UseDefaultFiles(); 

            app.MapControllers();

            app.Run();
        }
    }
}