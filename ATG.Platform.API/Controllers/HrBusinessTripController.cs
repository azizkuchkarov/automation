using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/hr/business-trips")]
[Authorize]
public class HrBusinessTripController(IHrBusinessTripRequestService service) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await service.GetMyRequestsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(CancellationToken ct)
    {
        var result = await service.GetApprovalQueueAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("order-queue")]
    public async Task<IActionResult> GetOrderQueue(CancellationToken ct)
    {
        var result = await service.GetOrderQueueAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("certificate-queue")]
    public async Task<IActionResult> GetCertificateQueue(CancellationToken ct)
    {
        var result = await service.GetCertificateQueueAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("colleagues")]
    public async Task<IActionResult> GetColleagues(CancellationToken ct)
    {
        var result = await service.GetDepartmentColleaguesAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHrBusinessTripRequestRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHrBusinessTripRequestRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await service.SubmitAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/hr-review")]
    public async Task<IActionResult> HrReview(Guid id, [FromBody] HrBusinessTripApprovalRequest request, CancellationToken ct)
    {
        var result = await service.HrReviewAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] HrBusinessTripApprovalRequest request, CancellationToken ct)
    {
        var result = await service.ApproveAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/signing-package")]
    public async Task<IActionResult> GetSigningPackage(Guid id, CancellationToken ct)
    {
        var result = await service.GetSigningPackageAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/order-signing-package")]
    public async Task<IActionResult> GetOrderSigningPackage(Guid id, CancellationToken ct)
    {
        var result = await service.GetOrderSigningPackageAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/sign-order")]
    public async Task<IActionResult> SignOrder(Guid id, [FromBody] HrBusinessTripApprovalRequest request, CancellationToken ct)
    {
        var result = await service.SignOrderWithEimzoAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] HrBusinessTripApprovalRequest request, CancellationToken ct)
    {
        var result = await service.RejectAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/download/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadPdfAsync(id, GetUserId()!.Value, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    [HttpGet("{id:guid}/download/signed-pdf")]
    public async Task<IActionResult> DownloadSignedPdf(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadSignedPdfAsync(id, GetUserId()!.Value, GetIp(), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    [HttpPost("{id:guid}/issue-order")]
    public async Task<IActionResult> IssueOrder(Guid id, CancellationToken ct)
    {
        var result = await service.IssueOrderAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("issue-order")]
    public async Task<IActionResult> IssueOrders([FromBody] IssueHrBusinessTripOrderRequest request, CancellationToken ct)
    {
        var result = await service.IssueOrdersAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/download/order-pdf")]
    [HttpGet("{id:guid}/download/order-docx")]
    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadOrderDocxAsync(id, GetUserId()!.Value, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    /// <summary>Public QR target — returns the order PDF without authentication.</summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}/public/order-pdf")]
    public async Task<IActionResult> DownloadOrderPdfPublic(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadOrderPdfPublicAsync(id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    [HttpGet("{id:guid}/download/order-signed-pdf")]
    public async Task<IActionResult> DownloadOrderSignedPdf(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadSignedPdfAsync(id, GetUserId()!.Value, GetIp(), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    [HttpPost("{id:guid}/generate-certificates")]
    public async Task<IActionResult> GenerateCertificates(Guid id, CancellationToken ct)
    {
        var result = await service.GenerateCertificatesAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/deliver-certificates")]
    public async Task<IActionResult> DeliverCertificates(Guid id, CancellationToken ct)
    {
        var result = await service.DeliverCertificatesAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/travelers/{travelerId:guid}/download/certificate")]
    public async Task<IActionResult> DownloadCertificate(Guid id, Guid travelerId, CancellationToken ct)
    {
        var result = await service.DownloadCertificateAsync(id, travelerId, GetUserId()!.Value, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
