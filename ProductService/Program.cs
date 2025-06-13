using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductService.Hubs;
using ProductService.Models.dbProduct;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
var connectionString = builder.Configuration.GetConnectionString("ProductDbService");
builder.Services.AddDbContext<ProductDBContext>(options =>
    options
        .UseLazyLoadingProxies()
        .UseSqlServer(connectionString));
//Bật giao diện authentication 
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    // 🔥 Thêm hỗ trợ Authorization header tất cả api
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token vào ô bên dưới theo định dạng: Bearer {token}"
    });

    // 🔥 Định nghĩa yêu cầu sử dụng Authorization trên từng api
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
//Thêm middleware authentication
var privateKey = builder.Configuration["jwt:Secret-Key"];
var Issuer = builder.Configuration["jwt:Issuer"];
var Audience = builder.Configuration["jwt:Audience"];
// Thêm dịch vụ Authentication vào ứng dụng, sử dụng JWT Bearer làm phương thức xác thực
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // Thiết lập các tham số xác thực token
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        // Kiểm tra và xác nhận Issuer (nguồn phát hành token)
        ValidateIssuer = true,
        ValidIssuer = Issuer, // Biến `Issuer` chứa giá trị của Issuer hợp lệ
                              // Kiểm tra và xác nhận Audience (đối tượng nhận token)
        ValidateAudience = true,
        ValidAudience = Audience, // Biến `Audience` chứa giá trị của Audience hợp lệ
                                  // Kiểm tra và xác nhận khóa bí mật được sử dụng để ký token
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey)),
        // Sử dụng khóa bí mật (`privateKey`) để tạo SymmetricSecurityKey nhằm xác thực chữ ký của token
        // Giảm độ trễ (skew time) của token xuống 0, đảm bảo token hết hạn chính xác
        ClockSkew = TimeSpan.Zero,
        // Xác định claim chứa vai trò của user (để phân quyền)
        RoleClaimType = ClaimTypes.Role,
        // Xác định claim chứa tên của user
        NameClaimType = ClaimTypes.Name,
        // Kiểm tra thời gian hết hạn của token, không cho phép sử dụng token hết hạn
        ValidateLifetime = true
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});
//DI Service JWT
builder.Services.AddScoped<JwtAuthService>();

// Thêm dịch vụ Authorization để hỗ trợ phân quyền người dùng
builder.Services.AddAuthorization();
builder.Services.AddScoped<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<KafkaConsumerService>();
//DI Repository
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
//DI UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//DI Service
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProdService, ProdService>();

builder.Services.AddHttpClient();
// Lấy chuỗi kết nối Redis từ cấu hình
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");

// Thêm SignalR với Redis Backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options => {
        options.Configuration.ChannelPrefix = "ProductService"; // Tiền tố để phân biệt các kênh SignalR
    }).AddJsonProtocol();

// Cấu hình Redis Cache với cùng một connection string
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// Cấu hình ConnectionMultiplexer với cùng một connection string
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => {
    return ConnectionMultiplexer.Connect(redisConnectionString);
});
builder.Services.AddSingleton<RedisHelper>();

//Add middleware controller
builder.Services.AddControllers();

//bật cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder
            .WithOrigins("http://localhost:5093", "https://localhost:7257","https://localhost:7260","http://localhost:5282") // Thêm tất cả domain client của bạn
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Quan trọng cho SignalR
});
builder.Services.AddSignalR();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware xác thực phải đặt đúng thứ tự
app.UseAuthentication();
app.UseAuthorization();

// MapControllers phải đặt sau Authentication và Authorization
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.Run();

