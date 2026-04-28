using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Parkease.Test;

[TestFixture]
public class BookingServiceTests
{
    private Mock<IBookingRepository> _repoMock;
    private Mock<ISlotServiceClient> _slotClientMock;
    private Mock<IPaymentServiceClient> _paymentClientMock;
    private Mock<IAuthServiceClient> _authClientMock;
    private Mock<IBookingEventPublisher> _publisherMock;
    private Mock<ILogger<BookingService.Infrastructure.Services.BookingService>> _loggerMock;
    private BookingService.Infrastructure.Services.BookingService _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IBookingRepository>();
        _slotClientMock = new Mock<ISlotServiceClient>();
        _paymentClientMock = new Mock<IPaymentServiceClient>();
        _authClientMock = new Mock<IAuthServiceClient>();
        _publisherMock = new Mock<IBookingEventPublisher>();
        _loggerMock = new Mock<ILogger<BookingService.Infrastructure.Services.BookingService>>();

        _service = new BookingService.Infrastructure.Services.BookingService(
            _repoMock.Object, 
            _slotClientMock.Object, 
            _paymentClientMock.Object, 
            _authClientMock.Object, 
            _publisherMock.Object, 
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateBookingAsync_ShouldReturnResponse_WhenSlotIsAvailable()
    {
        // Arrange
        var slotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateBookingRequest { SlotId = slotId, UserId = userId, Amount = 100.0m };
        
        _slotClientMock.Setup(x => x.ReserveSlotAsync(slotId, It.IsAny<Guid>())).ReturnsAsync(true);
        _paymentClientMock.Setup(x => x.CreateOrderAsync(It.IsAny<CreateRazorpayOrderRequest>()))
            .ReturnsAsync(new RazorpayOrderResponse("order_1", "INR", 10000, "created", "key_1"));

        // Act
        var result = await _service.CreateBookingAsync(request);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.RazorpayOrderId, Is.EqualTo("order_1"));
        _repoMock.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Once);
    }

    [Test]
    public void CreateBookingAsync_ShouldThrow_WhenSlotCannotBeReserved()
    {
        // Arrange
        _slotClientMock.Setup(x => x.ReserveSlotAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
        var request = new CreateBookingRequest { SlotId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 50.0m };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateBookingAsync(request));
    }

    [Test]
    public async Task ConfirmBookingAsync_ShouldUpdateStatusAndPublishEvent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var booking = new BookingEntity { CorrelationId = correlationId, Status = BookingStatus.Pending, UserId = Guid.NewGuid(), SlotId = Guid.NewGuid() };
        
        _repoMock.Setup(x => x.GetByCorrelationIdAsync(correlationId)).ReturnsAsync(booking);
        _authClientMock.Setup(x => x.GetUserDetailsAsync(booking.UserId)).ReturnsAsync(new UserDetailsResponse("test@test.com", "Test User", "1234567890"));

        // Act
        await _service.ConfirmBookingAsync(correlationId, "pay_123");

        // Assert
        Assert.That(booking.Status, Is.EqualTo(BookingStatus.Confirmed));
        _publisherMock.Verify(x => x.PublishBookingConfirmedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        _slotClientMock.Verify(x => x.ConfirmSlotAsync(booking.SlotId, correlationId), Times.Once);
    }

    [Test]
    public async Task FailBookingAsync_ShouldReleaseSlot()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var booking = new BookingEntity { CorrelationId = correlationId, Status = BookingStatus.Pending, SlotId = Guid.NewGuid() };
        _repoMock.Setup(x => x.GetByCorrelationIdAsync(correlationId)).ReturnsAsync(booking);

        // Act
        await _service.FailBookingAsync(correlationId, "Payment Failed");

        // Assert
        Assert.That(booking.Status, Is.EqualTo(BookingStatus.Failed));
        _slotClientMock.Verify(x => x.ReleaseSlotAsync(booking.SlotId, correlationId), Times.Once);
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnList()
    {
        var userId = Guid.NewGuid();
        var bookings = new List<BookingEntity> { new BookingEntity { UserId = userId } };
        _repoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(bookings);

        var result = await _service.GetByUserIdAsync(userId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }
}
