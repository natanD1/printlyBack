namespace Printly.Application.DTOs.Showcases;

public record SetShowcasePublishedRequest(bool IsPublished);

public record ShowcaseProductIdsRequest(List<Guid> ProductIds);

public record SetShowcaseItemVisibilityRequest(bool IsVisible);

public record SetShowcaseBlockedRequest(bool IsBlocked);
