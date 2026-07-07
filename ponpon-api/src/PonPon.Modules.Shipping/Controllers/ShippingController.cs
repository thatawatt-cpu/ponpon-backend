using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Shipping.Application.Features.Shipping.CancelShippingBooking;
using PonPon.Modules.Shipping.Application.Features.Shipping.CheckShippingRates;
using PonPon.Modules.Shipping.Application.Features.Shipping.CreateShippingBooking;
using PonPon.Modules.Shipping.Application.Features.Shipping.GetShippingBooking;

namespace PonPon.Modules.Shipping.Controllers;

[ApiController]
[Route("api/shipping")]
[Authorize]
public sealed class ShippingController : ControllerBase
{
    /// <summary>
    /// ตรวจราคาค่าขนส่งจากทุก courier — ใช้ตอนลูกค้าเลือก shipping option
    /// </summary>
    [HttpPost("rates")]
    public async Task<ActionResult<IReadOnlyList<ShippingRateResponse>>> CheckRates(
        [FromBody] CheckShippingRatesRequest request,
        [FromServices] CheckShippingRatesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new CheckShippingRatesQuery(
            request.ToName, request.ToPhone, request.ToEmail,
            request.ToAddress, request.ToDistrict, request.ToState,
            request.ToProvince, request.ToPostcode,
            request.ParcelName,
            request.WeightKg, request.WidthCm, request.LengthCm, request.HeightCm);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

    /// <summary>
    /// สร้าง booking / จองขนส่ง — admin เท่านั้น หลังจากแพ็กสินค้าแล้ว
    /// </summary>
    [HttpPost("bookings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateShippingBookingResponse>> CreateBooking(
        [FromBody] CreateShippingBookingRequest request,
        [FromServices] CreateShippingBookingHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateShippingBookingCommand(
            request.ToName, request.ToPhone, request.ToEmail,
            request.ToAddress, request.ToDistrict, request.ToState,
            request.ToProvince, request.ToPostcode,
            request.ParcelName,
            request.WeightKg, request.WidthCm, request.LengthCm, request.HeightCm,
            request.CourierCode, request.ServiceCode,
            request.Remark, request.Cod,
            request.OrderId, request.OrderNumber);

        return Ok(await handler.HandleAsync(command, cancellationToken));
    }

    /// <summary>
    /// ดูสถานะ booking + URL label สำหรับปริ้น
    /// </summary>
    [HttpGet("bookings/{trackingCode}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GetShippingBookingResponse>> GetBooking(
        string trackingCode,
        [FromServices] GetShippingBookingHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetShippingBookingQuery(trackingCode), cancellationToken));
    }

    /// <summary>
    /// ยกเลิก booking — admin เท่านั้น
    /// </summary>
    [HttpDelete("bookings/{trackingCode}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelBooking(
        string trackingCode,
        [FromServices] CancelShippingBookingHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new CancelShippingBookingCommand(trackingCode), cancellationToken);
        return NoContent();
    }
}
