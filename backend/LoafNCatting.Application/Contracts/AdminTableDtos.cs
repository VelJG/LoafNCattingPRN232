namespace LoafNCatting.Application.Contracts;

public sealed class AdminTableDto
{
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Area { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminTableUpsertRequest
{
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Area { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminTableOptionsDto
{
    public IReadOnlyList<string> Statuses { get; set; } = [];
}
