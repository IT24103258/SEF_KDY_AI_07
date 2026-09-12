namespace FixFlow.Api.DTOs;

public class LocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class LocationCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class AssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
}

public class AssetCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Criticality { get; set; } = "Medium";
    public Guid LocationId { get; set; }
}

public class IssueCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultPriority { get; set; } = string.Empty;
}
