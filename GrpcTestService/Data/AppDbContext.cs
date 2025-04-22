using GrpcTestService.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    // 正确做法：添加接收 DbContextOptions 的构造函数
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) // 必须传递给基类构造函数
    {
    }
    // 表示数据库中的 Users 表
    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<UserWarehouseAssociationTable> UserWarehouseAssociationTables { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseDistanceAssociationTable> WarehouseDistanceAssociationTables { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<VIPInfomation> VIPInfomations { get; set; }
    public DbSet<VIPAndWarehouseRelations> VIPAndWarehouseRelations { get; set; }
    // 配置数据库连接
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 连接字符串
        string connectionString = "Server=CHINAMI-9AS5CS2;Database=demo1;User Id=sa;Password=123456;TrustServerCertificate=true;";
        optionsBuilder.UseSqlServer(connectionString);
    }
}