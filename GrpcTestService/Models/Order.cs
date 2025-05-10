namespace GrpcTestService.Models;

public class Order
{
    public int Id { get; set; }
    public int VIPId { get; set; }
    public int WarehouseAId { get; set; }
    public int WarehouseBId { get; set; }
    public int Price { get; set; }
    public int DistanceId { get; set; }
    public string OrderDetail { get; set; }
    public DateTime OrderDate { get; set; }
}