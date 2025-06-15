# DA_Ecommerce_MS - Distributed E-commerce Microservices Platform 🛒

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-blue.svg)](https://blazor.net/)
[![Microservices](https://img.shields.io/badge/Architecture-Microservices-orange.svg)](https://microservices.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📋 Tổng quan dự án

DA_Ecommerce_MS là một nền tảng thương mại điện tử phân tán được xây dựng với kiến trúc Microservices hiện đại, sử dụng .NET 8.0, Blazor Server, và các công nghệ cloud-native. Hệ thống hỗ trợ đa vai trò với dashboard riêng biệt cho Admin, Seller và Shipper.

## 🏗️ Kiến trúc Microservices

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   BlazorWebApp  │────│  GateWayService  │────│     Services     │
│   (Frontend)    │    │   (API Gateway)  │    │   (Backend)     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                 │
                                 ├── MainEcommerceService
                                 ├── ProductService
                                 └── AppHost (Orchestrator)
```

### Services Architecture

#### 🎯 **AppHost** - Service Orchestrator
- **Mục đích**: Service discovery và orchestration
- **Công nghệ**: .NET Aspire, Docker Compose
- **Chức năng**: 
  - Kafka container management
  - Service coordination
  - Development environment setup

#### 🌐 **GateWayService** - API Gateway
- **Port**: 5282
- **Mục đích**: Centralized routing và load balancing
- **Tính năng**:
  - Request routing
  - Authentication middleware
  - Rate limiting
  - CORS handling

#### 🏪 **MainEcommerceService** - Core Business Logic
- **Database**: SQL Server
- **Chức năng chính**:
  - User management & authentication (JWT)
  - Order processing & management
  - Address management
  - Coupon system
  - Seller profile management
  - Shipper management
  - Dashboard analytics
  - Real-time notifications (SignalR)

#### 📦 **ProductService** - Product Management
- **Database**: SQL Server (separate)
- **Chức năng chính**:
  - Product CRUD operations
  - Category management
  - Image upload (AWS S3)
  - Product search & filtering
  - Inventory management
  - Caching (Redis)

#### 🖥️ **BlazorWebApp** - Frontend Application
- **Framework**: Blazor Server
- **Features**:
  - Responsive UI với MudBlazor
  - Real-time updates
  - Multi-role dashboards
  - Shopping cart & wishlist
  - Order tracking

## 🔧 Tech Stack

### Backend Technologies
- **.NET 8.0** - Core framework
- **Entity Framework Core** - ORM với Lazy Loading
- **SignalR** - Real-time communication với Redis backplane
- **Apache Kafka** - Message streaming & event sourcing
- **Redis** - Caching & session storage
- **JWT Bearer** - Authentication & authorization
- **AutoMapper** - Object mapping
- **AWS S3** - File storage

### Frontend Technologies
- **Blazor Server** - UI framework
- **MudBlazor** - Material Design components
- **SignalR Client** - Real-time UI updates
- **Blazored.LocalStorage** - Client-side storage
- **JavaScript Interop** - Device info collection

### Infrastructure
- **SQL Server** - Primary database
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation

## 🎭 Multi-Role System

### 👨‍💼 Admin Dashboard (`/admin`)
- **User Management**: CRUD users, roles, permissions
- **Product Management**: Categories, products, inventory
- **Order Management**: Order processing, status updates
- **Seller Management**: Approve sellers, manage stores
- **Shipper Management**: Assign orders, track deliveries
- **Analytics**: Sales reports, user statistics
- **System Health**: Service monitoring, performance metrics

### 🛍️ Seller Dashboard (`/seller`)
- **Store Management**: Store profile, business info
- **Product Management**: Add/edit products, inventory
- **Order Processing**: View assigned orders, update status
- **Sales Analytics**: Revenue reports, product performance
- **Image Management**: AWS S3 integration for product photos

### 🚚 Shipper Dashboard (`/shipper`)
- **Assigned Orders**: View delivery assignments
- **Order Tracking**: Update delivery status
- **Route Optimization**: Delivery planning
- **Performance Metrics**: Success rate, earnings
- **Real-time Notifications**: New assignments, updates

### 🛒 Customer Features
- **Product Browsing**: Search, filter, categories
- **Shopping Cart**: Add/remove items, quantity management
- **Wishlist**: Save favorite products
- **Checkout Process**: Address selection, payment
- **Order Tracking**: Real-time status updates
- **Profile Management**: Personal info, addresses

## 📊 Advanced Features

### 🔄 Real-time System
- **SignalR Hubs**: Product updates, order notifications
- **Redis Backplane**: Multi-instance synchronization
- **Live Dashboard**: Real-time metrics and alerts

### 📨 Event-Driven Architecture
- **Kafka Integration**: Order events, product updates
- **Event Sourcing**: Audit trail, data consistency
- **Message Producers/Consumers**: Cross-service communication

### 🗄️ Data Management
- **Redis Caching**: Product listings, user sessions
- **Database Separation**: Independent service databases
- **Connection Pooling**: Optimized database connections

### 🔒 Security & Authentication
- **JWT Tokens**: Stateless authentication
- **Role-based Access**: Admin, Seller, Shipper, Customer
- **Device Tracking**: Login security, device fingerprinting
- **Refresh Tokens**: Secure token renewal

### ☁️ Cloud Integration
- **AWS S3**: Product image storage
- **CloudFront**: CDN for image delivery
- **Environment Configs**: Development/Production settings

## 🚀 Installation & Setup

### Prerequisites
```bash
- .NET 8.0 SDK
- SQL Server 2019+
- Docker Desktop
- AWS Account (for S3)
- Redis (optional, for caching)
```

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/DA_Ecommer_MS.git
cd DA_Ecommer_MS
```

### 2. Database Setup
```bash
# Update connection strings in appsettings.json files
# MainEcommerceService
dotnet ef database update --project MainEcommerceService

# ProductService  
dotnet ef database update --project ProductService
```

### 3. Configure Services

#### MainEcommerceService/appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MainEcommerceDB;Trusted_Connection=true;TrustServerCertificate=true;",
    "RedisConnection": "localhost:6379"
  },
  "jwt": {
    "Secret-Key": "your-secret-key-minimum-32-characters",
    "Issuer": "DA_Ecommerce_MS",
    "Audience": "DA_Ecommerce_Users"
  }
}
```

#### ProductService/appsettings.json
```json
{
  "ConnectionStrings": {
    "ProductDbService": "Server=.;Database=ProductDB;Trusted_Connection=true;TrustServerCertificate=true;",
    "RedisConnection": "localhost:6379"
  },
  "AWS": {
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "Region": "ap-southeast-1",
    "BucketName": "your-s3-bucket"
  }
}
```

### 4. Start Infrastructure
```bash
# Start Kafka & Redis (optional)
cd AppHost
docker-compose -f kafka.yml up -d
```

### 5. Run Services

#### Option A: Using AppHost (Recommended)
```bash
dotnet run --project AppHost
```

#### Option B: Individual Services
```bash
# Terminal 1 - Gateway
dotnet run --project GateWayService

# Terminal 2 - Main Service
dotnet run --project MainEcommerceService

# Terminal 3 - Product Service
dotnet run --project ProductService

# Terminal 4 - Frontend
dotnet run --project BlazorWebApp
```

### 6. Access Applications
- **Frontend**: http://localhost:5000
- **Admin Panel**: http://localhost:5000/admin
- **API Gateway**: http://localhost:5001
- **Main API**: http://localhost:5282/swagger
- **Product API**: http://localhost:5284/swagger

## 📁 Project Structure

```
DA_Ecommer_MS/
├── AppHost/                           # Service orchestration
│   ├── kafka.yml                     # Docker compose for Kafka
│   └── Program.cs                     # Service discovery
│
├── BlazorWebApp/                      # Frontend application
│   ├── Pages/
│   │   ├── Admin/                     # Admin dashboard
│   │   │   ├── Dashboard.razor
│   │   │   ├── DashboardShipper.razor
│   │   │   └── Dialogs/
│   │   │       └── ProductDialog.razor
│   │   ├── Components/                # Shared components
│   │   │   └── HeaderComponent.razor
│   │   └── Customer/                  # Customer pages
│   ├── Services/                      # HTTP client services
│   │   ├── LoginService.cs
│   │   ├── ProdService.cs
│   │   ├── OrderService.cs
│   │   ├── ProductImageService.cs
│   │   └── SignalRService.cs
│   ├── ViewModel/                     # Data transfer objects
│   │   ├── CartVM.cs
│   │   ├── WishlistVM.cs
│   │   └── UserVM.cs
│   └── wwwroot/js/
│       └── deviceInfo.js              # Device fingerprinting
│
├── GateWayService/                    # API Gateway
│   └── Program.cs                     # Routing configuration
│
├── MainEcommerceService/              # Core business service
│   ├── Controllers/                   # API endpoints
│   │   ├── UserLoginController.cs
│   │   ├── OrderController.cs
│   │   ├── DashboardController.cs
│   │   └── SellerProfileController.cs
│   ├── Infrastructure/
│   │   ├── Services/                  # Business logic
│   │   ├── Repositories/              # Data access
│   │   └── UnitOfWork/               # Transaction management
│   ├── Models/                        # Entity models
│   ├── Hubs/                         # SignalR hubs
│   ├── Kafka/                        # Event handlers
│   └── Migrations/                   # Database migrations
│
└── ProductService/                    # Product management service
    ├── Controllers/                   # Product APIs
    ├── Infrastructure/
    │   └── Services/
    │       ├── ProdService.cs         # Product business logic
    │       ├── S3Service.cs           # AWS integration
    │       └── ProductImageService.cs # Image management
    ├── Models/                        # Product entities
    ├── Hubs/                         # Product notifications
    └── Kafka/                        # Product events
```

## 🔌 API Endpoints

### Authentication APIs
```
POST /api/UserLogin/LoginUser          # User login
POST /api/UserLogin/RegisterUser       # User registration  
PUT  /api/UserLogin/Logout             # User logout
POST /api/UserLogin/RefreshToken       # Token refresh
```

### Product APIs
```
GET    /api/products                   # Get all products
GET    /api/products/{id}              # Get product by ID
POST   /api/products                   # Create product (Seller/Admin)
PUT    /api/products/{id}              # Update product (Seller/Admin)
DELETE /api/products/{id}              # Delete product (Admin)
POST   /api/products/upload-image      # Upload product image
```

### Order APIs
```
GET  /api/Order                        # Get orders (filtered by role)
POST /api/Order/CreateOrder            # Create new order
PUT  /api/Order/{id}/status            # Update order status
GET  /api/Order/{id}/items             # Get order items
```

## 🌟 Key Features Breakdown

### 🛒 E-commerce Core
- **Product Catalog**: Categories, search, filtering
- **Shopping Cart**: Persistent cart, quantity management
- **Wishlist**: Save favorites, move to cart
- **Checkout**: Address selection, order creation
- **Order Tracking**: Real-time status updates

### 📊 Business Intelligence
- **Sales Analytics**: Revenue tracking, product performance
- **User Analytics**: Registration trends, activity patterns
- **Order Analytics**: Processing times, completion rates
- **Inventory Management**: Stock levels, reorder alerts

### 🔄 Real-time Features
- **Live Notifications**: Order updates, system alerts
- **Dashboard Updates**: Real-time metrics refresh
- **Inventory Sync**: Live stock level updates
- **Order Status**: Real-time tracking updates

### 🔐 Security Features
- **JWT Authentication**: Secure stateless auth
- **Role-based Authorization**: Granular permissions
- **Device Fingerprinting**: Security tracking
- **Input Validation**: XSS and injection prevention
- **CORS Configuration**: Cross-origin security

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Team

- **Lead Developer** - Architecture & Backend Development
- **Frontend Developer** - Blazor UI/UX Implementation
- **DevOps Engineer** - Infrastructure & Deployment

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/DA_Ecommer_MS/issues)
- **Documentation**: [Wiki](https://github.com/yourusername/DA_Ecommer_MS/wiki)
- **Email**: support@example.com

## 🙏 Acknowledgments

- [.NET Team](https://github.com/dotnet) - Excellent framework
- [MudBlazor](https://mudblazor.com/) - Beautiful Blazor components
- [Apache Kafka](https://kafka.apache.org/) - Reliable event streaming
- [Redis](https://redis.io/) - Fast caching solution
- [AWS](https://aws.amazon.com/) - Cloud infrastructure

---

⭐ **Star this repo if you find it helpful!** ⭐

---

*Last updated: June 2025*
