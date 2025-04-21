namespace GrpcTestService.Models;

public class WarehouseDistanceAssociationTable
{
    int ID {get; set;}
    string WarehouseAName { get; set; }
    string WarehouseBName { get; set; }
    int Distance { get; set; }
}