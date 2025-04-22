namespace GrpcTestService.Models;

public class VIPAndWarehouseRelations
{
    public int Id { get; set; }
    public int VIPId { get; set; }
    public int WarehouseId { get; set; }
    public int OccupiesVolume { get; set; }
}