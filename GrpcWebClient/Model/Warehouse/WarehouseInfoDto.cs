namespace GrpcWebClient.Model.Warehouse;

public class WarehouseInfoDto
{
    public string WarehouseName { get; set; }
    public int TotalVolume { get; set; }
    public int ResidualVolume { get; set; }
}