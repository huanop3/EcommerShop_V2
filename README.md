# EcommerShop_V1 🛒

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-blue.svg)](https://blazor.net/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📋 Mô tả dự án

EcommerShop_V1 là một ứng dụng thương mại điện tử hiện đại được xây dựng bằng kiến trúc Microservices với .NET 8.0 và Blazor Server. Dự án cung cấp một nền tảng mua sắm trực tuyến hoàn chính với giao diện người dùng thân thiện và các tính năng quản lý toàn diện.

## 🏗️ Kiến trúc hệ thống

### Microservices Architecture
- **AppHost**: Orchestration và service discovery
- **GateWayService**: API Gateway và routing
- **MainEcommerceService**: Dịch vụ chính quản lý người dùng, đơn hàng, coupon
- **ProductService**: Dịch vụ quản lý sản phẩm và danh mục
- **BlazorWebApp**: Giao diện người dùng Blazor Server

### Technologies Stack
- **.NET 8.0**: Framework chính
- **Blazor Server**: Frontend framework
- **Entity Framework Core**: ORM
- **SignalR**: Real-time communication
- **Apache Kafka**: Message broker
- **JWT**: Authentication & Authorization
- **AutoMapper**: Object mapping
- **Redis**: Caching (optional)

## ✨ Tính năng chính

### 🛍️ Cho khách hàng
- **Đăng ký/Đăng nhập**: Xác thực người dùng an toàn
- **Duyệt sản phẩm**: Tìm kiếm, lọc, phân loại sản phẩm
- **Giỏ hàng**: Thêm, xóa, cập nhật sản phẩm
- **Wishlist**: Lưu sản phẩm yêu thích
- **Thanh toán**: Quy trình thanh toán đơn giản
- **Quản lý hồ sơ**: Cập nhật thông tin cá nhân
- **Địa chỉ giao hàng**: Quản lý nhiều địa chỉ
- **Lịch sử đơn hàng**: Theo dõi đơn hàng

### 👨‍💼 Cho quản trị viên
- **Dashboard**: Thống kê tổng quan
- **Quản lý sản phẩm**: CRUD sản phẩm, danh mục
- **Quản lý người dùng**: Quản lý tài khoản khách hàng
- **Quản lý đơn hàng**: Xử lý đơn hàng, cập nhật trạng thái
- **Quản lý coupon**: Tạo và quản lý mã giảm giá
- **Seller Profile**: Quản lý thông tin người bán

### 🔧 Tính năng kỹ thuật
- **Real-time notifications**: SignalR
- **Caching**: Redis integration
- **Message Queue**: Kafka integration
- **Responsive Design**: Mobile-friendly UI
- **Toast Notifications**: User feedback
- **Loading States**: Better UX

## 🚀 Cài đặt và chạy

### Yêu cầu hệ thống
- .NET 8.0 SDK
- SQL Server (hoặc SQL Server Express)
- Docker (optional - cho Kafka, Redis)
- Visual Studio 2022 hoặc VS Code

### Cài đặt

1. **Clone repository**
```bash
git clone https://github.com/huanop3/EcommerShop_V1.git
cd EcommerShop_V1
```

2. **Cấu hình cơ sở dữ liệu**
- Cập nhật connection string trong `appsettings.json` của các service
- Chạy migrations:
```bash
dotnet ef database update --project MainEcommerceService
dotnet ef database update --project ProductService
```

3. **Cài đặt dependencies**
```bash
dotnet restore
```

4. **Chạy Kafka (optional)**
```bash
docker-compose -f AppHost/kafka.yml up -d
```

5. **Chạy ứng dụng**
```bash
dotnet run --project AppHost
```

Hoặc chạy từng service riêng biệt:
```bash
# Terminal 1
dotnet run --project GateWayService

# Terminal 2  
dotnet run --project MainEcommerceService

# Terminal 3
dotnet run --project ProductService

# Terminal 4
dotnet run --project BlazorWebApp
```

## 📁 Cấu trúc dự án

```
EcommerShop_V1/
├── AppHost/                    # Orchestration service
├── BlazorWebApp/              # Frontend Blazor application
│   ├── Pages/                 # Blazor pages
│   ├── Components/            # Reusable components
│   ├── Services/              # Client services
│   ├── ViewModel/             # Data models
│   └── wwwroot/              # Static assets
├── GateWayService/            # API Gateway
├── MainEcommerceService/      # Main business logic service
│   ├── Controllers/           # API controllers
│   ├── Infrastructure/        # Data access layer
│   ├── Models/               # Entity models
│   ├── Hubs/                 # SignalR hubs
│   └── Kafka/                # Message handlers
└── ProductService/           # Product management service
    ├── Controllers/           # Product API controllers
    ├── Infrastructure/        # Product data access
    ├── Models/               # Product entities
    └── Kafka/                # Product events
```

## 🌟 Screenshots

### Trang chủ
![Homepage](screenshots/homepage.png)

### Sản phẩm
![Products](screenshots/products.png)

### Giỏ hàng
![Cart](screenshots/cart.png)

### Admin Dashboard
![Admin](screenshots/admin.png)

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📝 License

Dự án này được phân phối dưới License MIT. Xem file `LICENSE` để biết thêm chi tiết.

## 👥 Tác giả

- **Your Name** - [huanop3](https://github.com/huanop3)

## 📞 Liên hệ

- GitHub: [@huanop3](https://github.com/huanop3)
- Email: your.email@example.com

## 🙏 Cảm ơn

- [.NET Team](https://github.com/dotnet) - Framework tuyệt vời
- [Blazor Community](https://blazor.net/) - UI framework mạnh mẽ
- [Apache Kafka](https://kafka.apache.org/) - Message streaming platform

---

⭐ Nếu dự án này hữu ích, hãy cho một star nhé! ⭐
