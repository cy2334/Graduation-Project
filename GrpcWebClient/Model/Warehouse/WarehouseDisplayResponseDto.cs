namespace GrpcWebClient.Model.Warehouse;

public class WarehouseDisplayResponseDto
{
    public List<WarehouseInfoDto> WarehouseInfos { get; set; }
    public int PageSize { get; set; }
    public int PageIndex { get; set; }
    public int Total { get; set; }
}