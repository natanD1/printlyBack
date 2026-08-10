namespace PrintFlow.Application.DTOs.FilamentTypes;

public record FilamentTypeDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
