namespace GrpcTestService.Models;

public class WarehouseDistanceAssociationTable
{
    public int Id {get; set;}
    public string WarehouseAName { get; set; }
    public string WarehouseBName { get; set; }
    public int Distance { get; set; }
}