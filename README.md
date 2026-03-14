# EverFirst
website 
# InventSystem - Complete Inventory Management System

## Features

### Admin Panel
- **Dashboard**: 4 types of charts (Line, Pie, Bar, Doughnut) showing revenue, orders, categories, and sales data
- **Product Management**: Add, edit, delete products with stock tracking
- **Category Management**: Organize products by categories
- **Order Management**: View and update order status
- **Customer Management**: View registered customers
- **Transaction Management**: Track all financial transactions
- **Stock Alerts**: Get notified of low stock items
- **CSV Export**: Export products, orders, and transactions

### Customer Features
- **Product Browsing**: Search, filter, and sort products
- **Shopping Cart**: Add/remove items, update quantities
- **Checkout**: Place orders with shipping details
- **Order Tracking**: View order history and status
- **Profile Management**: Update personal information

### Security
- JWT-based authentication
- Role-based access control (Admin/Customer)
- Password hashing with BCrypt
- Secure cookie storage

## Setup Instructions

1. **Prerequisites**
   - Visual Studio 2022 or later
   - .NET 8.0 SDK
   - SQL Server LocalDB (comes with Visual Studio)

2. **Open Project**
   - Open `InventSystem.sln` in Visual Studio
   - Restore NuGet packages (automatic)

3. **Database Setup**
   - Open Package Manager Console
   - Run: `Add-Migration InitialCreate`
   - Run: `Update-Database`

4. **Run Application**
   - Press F5 or click Run
   - Application will open in your browser

5. **Default Admin Credentials**
   - Email: admin@inventsystem.com
   - Password: Admin@123

## Technology Stack

- ASP.NET Core 8.0 MVC
- Entity Framework Core 8.0
- SQL Server LocalDB
- Bootstrap 5.3
- Chart.js 4.4
- JWT Authentication
- Font Awesome 6.4

## Project Structure

```
InventSystem/
├── Controllers/      # MVC Controllers
├── Models/          # Data models and ViewModels
├── Views/           # Razor views
├── Data/            # DbContext and migrations
├── Services/        # Business logic services
├── Middleware/      # Custom middleware
└── wwwroot/         # Static files
```

## License

MIT License

