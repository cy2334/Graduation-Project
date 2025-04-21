namespace GrpcTestService.Models;

public class UserWarehouseAssociationTable
{
    public int id { get; set; }
    public int userId { get; set; }
    public int warehouseId { get; set; }    
    public AccessPermission status { get; set; }
}

public enum AccessPermission
{
    ReadOnly = 0,
    ReadAndWrite = 1
}