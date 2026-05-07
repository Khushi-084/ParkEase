using Moq;
using NUnit.Framework;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;
using Ticket.Domain.Entities;
using Ticket.Domain.Enums;
using Ticket.Infrastructure.Services;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Parkease.Test;

[TestFixture]
public class TicketServiceTests
{
    private Mock<ITicketRepository> _ticketRepoMock;
    private Mock<ISlotServiceClient> _slotClientMock;
    private Mock<IPaymentServiceClient> _paymentClientMock;
    private TicketService _ticketService;

    [SetUp]
    public void SetUp()
    {
        _ticketRepoMock = new Mock<ITicketRepository>();
        _slotClientMock = new Mock<ISlotServiceClient>();
        _paymentClientMock = new Mock<IPaymentServiceClient>();
        _ticketService = new TicketService(_ticketRepoMock.Object, _slotClientMock.Object, _paymentClientMock.Object);
    }

    // ── Entry Flow Tests ──────────────────────────────────────────────────────

    [Test]
    public async Task CreateTicketAsync_ShouldCreateTicket_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateTicketRequest { VehicleNumber = "MH12AB1234", SlotType = "Car" };
        var slot = new SlotDTO { SlotId = Guid.NewGuid(), SlotNumber = "A-101", Type = "Car", PricePerHour = 20.0m };
        
        _slotClientMock.Setup(x => x.GetAvailableSlotAsync("Car"))
            .ReturnsAsync(slot);
        
        _ticketRepoMock.Setup(x => x.AddAsync(It.IsAny<TicketEntity>()))
            .ReturnsAsync((TicketEntity t) => t);

        // Act
        var result = await _ticketService.CreateTicketAsync(request);

        // Assert
        Assert.That(result.VehicleNumber, Is.EqualTo("MH12AB1234"));
        Assert.That(result.SlotNumber, Is.EqualTo("A-101"));
        _slotClientMock.Verify(x => x.MarkSlotOccupiedAsync(slot.SlotId), Times.Once);
    }

    [Test]
    public void CreateTicketAsync_ShouldThrowException_WhenVehicleNumberIsInvalid()
    {
        // Arrange
        var request = new CreateTicketRequest { VehicleNumber = "!!!", SlotType = "Car" };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _ticketService.CreateTicketAsync(request));
    }

    [Test]
    public async Task CreateTicketAsync_ShouldNormalizeVehicleNumber()
    {
        // Arrange
        var request = new CreateTicketRequest { VehicleNumber = " MH12AB1234 ", SlotType = "Car" };
        var slot = new SlotDTO { SlotId = Guid.NewGuid(), SlotNumber = "A-101", Type = "Car", PricePerHour = 20.0m };
        
        _slotClientMock.Setup(x => x.GetAvailableSlotAsync("Car")).ReturnsAsync(slot);
        _ticketRepoMock.Setup(x => x.AddAsync(It.IsAny<TicketEntity>())).ReturnsAsync((TicketEntity t) => t);

        // Act
        var result = await _ticketService.CreateTicketAsync(request);

        // Assert
        Assert.That(result.VehicleNumber, Is.EqualTo("MH12AB1234"));
    }

    // ── Exit Flow Tests ───────────────────────────────────────────────────────

    [Test]
    public async Task ExitTicketAsync_ShouldCalculateCorrectAmount_ForOneHour()
    {
        // Arrange
        var ticket = new TicketEntity 
        { 
            Id = Guid.NewGuid(), 
            DisplayId = "PK-ABC123", 
            EntryTime = DateTime.UtcNow.AddMinutes(-45), // 45 mins ago -> 1 hour billing
            SlotId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            VehicleNumber = "MH12AB1234",
            SlotNumber = "A-1"
        };

        var slot = new SlotDTO { SlotId = ticket.SlotId, SlotNumber = "A-1", Type = "Car", PricePerHour = 50.0m };

        _ticketRepoMock.Setup(x => x.GetByDisplayIdAsync("PK-ABC123")).ReturnsAsync(ticket);
        _slotClientMock.Setup(x => x.GetSlotByIdAsync(ticket.SlotId)).ReturnsAsync(slot);
        _paymentClientMock.Setup(x => x.CreatePaymentAsync(It.IsAny<InitiateTicketPaymentRequest>()))
            .ReturnsAsync(new PaymentInitResponse(Guid.NewGuid(), "Succeeded", "order_123"));
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<TicketEntity>())).ReturnsAsync((TicketEntity t) => t);

        // Act
        var result = await _ticketService.ExitTicketAsync("PK-ABC123", "Cash");

        // Assert
        Assert.That(result.Amount, Is.EqualTo(50.0m));
        Assert.That(result.DurationHours, Is.EqualTo(1));
    }

    [Test]
    public async Task ExitTicketAsync_ShouldCalculateCorrectAmount_ForTwoHours()
    {
        // Arrange
        var ticket = new TicketEntity 
        { 
            Id = Guid.NewGuid(), 
            EntryTime = DateTime.UtcNow.AddMinutes(-70), // 70 mins -> 2 hours billing
            SlotId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            VehicleNumber = "MH12AB1234",
            SlotNumber = "A-1"
        };
        var slot = new SlotDTO { SlotId = ticket.SlotId, SlotNumber = "A-1", Type = "Car", PricePerHour = 30.0m };

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _slotClientMock.Setup(x => x.GetSlotByIdAsync(ticket.SlotId)).ReturnsAsync(slot);
        _paymentClientMock.Setup(x => x.CreatePaymentAsync(It.IsAny<InitiateTicketPaymentRequest>()))
            .ReturnsAsync(new PaymentInitResponse(Guid.NewGuid(), "Succeeded", "order_123"));
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<TicketEntity>())).ReturnsAsync((TicketEntity t) => t);

        // Act
        var result = await _ticketService.ExitTicketAsync(ticket.Id.ToString(), "UPI");

        // Assert
        Assert.That(result.Amount, Is.EqualTo(60.0m));
        Assert.That(result.DurationHours, Is.EqualTo(2));
    }

    [Test]
    public void ExitTicketAsync_ShouldThrow_WhenTicketNotFound()
    {
        _ticketRepoMock.Setup(x => x.GetByDisplayIdAsync(It.IsAny<string>())).ReturnsAsync((TicketEntity)null!);
        _ticketRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TicketEntity)null!);
        Assert.ThrowsAsync<KeyNotFoundException>(() => _ticketService.ExitTicketAsync("NONEXISTENT", "Cash"));
    }

    [Test]
    public void ExitTicketAsync_ShouldThrow_WhenTicketAlreadyCompleted()
    {
        var ticket = new TicketEntity { Status = TicketStatus.Completed };
        _ticketRepoMock.Setup(x => x.GetByDisplayIdAsync("DONE")).ReturnsAsync(ticket);
        Assert.ThrowsAsync<InvalidOperationException>(() => _ticketService.ExitTicketAsync("DONE", "Cash"));
    }

    [Test]
    public async Task GetActiveCountByLotAsync_ShouldReturnZero_WhenNoSlotsFound()
    {
        var lotId = Guid.NewGuid();
        _slotClientMock.Setup(x => x.GetSlotIdsByLotAsync(lotId)).ReturnsAsync(new List<Guid>());
        
        var result = await _ticketService.GetActiveCountByLotAsync(lotId);
        
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ExitTicketAsync_ShouldReleaseSlot_OnSuccess()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new TicketEntity { Id = ticketId, SlotId = Guid.NewGuid(), EntryTime = DateTime.UtcNow.AddHours(-1), Status = TicketStatus.Active, VehicleNumber = "MH12AB1234", SlotNumber = "S1" };
        var slot = new SlotDTO { SlotId = ticket.SlotId, SlotNumber = "S1", Type = "Car", PricePerHour = 10.0m };
        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
        _slotClientMock.Setup(x => x.GetSlotByIdAsync(ticket.SlotId)).ReturnsAsync(slot);
        _paymentClientMock.Setup(x => x.CreatePaymentAsync(It.IsAny<InitiateTicketPaymentRequest>()))
            .ReturnsAsync(new PaymentInitResponse(Guid.NewGuid(), "Succeeded", ""));
        _ticketRepoMock.Setup(x => x.UpdateAsync(It.IsAny<TicketEntity>())).ReturnsAsync((TicketEntity t) => t);

        // Act
        await _ticketService.ExitTicketAsync(ticketId.ToString(), "Card");

        // Assert
        _slotClientMock.Verify(x => x.MarkSlotAvailableAsync(ticket.SlotId), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnResponse_WhenExists()
    {
        var id = Guid.NewGuid();
        var ticket = new TicketEntity { Id = id, VehicleNumber = "MH12AB1234", Status = TicketStatus.Active, SlotNumber = "A1", SlotId = Guid.NewGuid(), EntryTime = DateTime.UtcNow };
        _ticketRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(ticket);

        var result = await _ticketService.GetByIdAsync(id);

        Assert.That(result.VehicleNumber, Is.EqualTo("MH12AB1234"));
    }
}
