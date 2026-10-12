using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Feedbacks;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("feedbacks")]
[Authorize]
public class FeedbacksController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbacksController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<FeedbackDto>>> GetAll(CancellationToken cancellationToken)
    {
        var feedbacks = await _feedbackService.GetAllAsync(cancellationToken);
        return Ok(feedbacks);
    }

    [HttpPost]
    public async Task<ActionResult<FeedbackDto>> Submit(CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.SubmitAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, feedback);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FeedbackDto>> UpdateStatus(Guid id, UpdateFeedbackStatusRequest request, CancellationToken cancellationToken)
    {
        var feedback = await _feedbackService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(feedback);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
