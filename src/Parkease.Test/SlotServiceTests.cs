using Moq;
using NUnit.Framework;
using Slot.Application.Interfaces;
using Slot.Domain.Entities;
using Slot.Domain.Enums;
using Slot.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Parkease.Test;

[TestFixture]
public class SlotServiceTests
{
    private Mock<ISlotRepository> _repoMock;
    private SlotService _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<ISlotRepository>();
        _service = new SlotService(_repoMock.Object);
    }

    // ── GetFirstAvailableAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetFirstAvailableAsync_ShouldReturnSlot_WhenAvailable()
    {
        var slot = new SlotEntity { SlotId = Guid.NewGuid(), SlotNumber = "A1", Status = SlotStatus.Available, Type = SlotType.Car };
        _repoMock.Setup(x => x.GetFirstAvailableAsync(It.IsAny<SlotType?>())).ReturnsAsync(slot);

        var result = await _service.GetFirstAvailableAsync("Car");

        Assert.That(result.SlotNumber, Is.EqualTo("A1"));
    }

    [Test]
    public void GetFirstAvailableAsync_ShouldThrow_WhenNoSlotAvailable()
    {
        _repoMock.Setup(x => x.GetFirstAvailableAsync(It.IsAny<SlotType?>())).ReturnsAsync((SlotEntity)null!);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetFirstAvailableAsync("Car"));
    }

    // ── ReserveSlotAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task ReserveSlotAsync_ShouldChangeStatus_ToReserved()
    {
        var slotId = Guid.NewGuid();
        var slot = new SlotEntity { SlotId = slotId, Status = SlotStatus.Available, SlotNumber = "B2" };
        _repoMock.Setup(x => x.GetByIdAsync(slotId)).ReturnsAsync(slot);

        await _service.ReserveSlotAsync(slotId);

        Assert.That(slot.Status, Is.EqualTo(SlotStatus.Reserved));
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void ReserveSlotAsync_ShouldThrow_WhenSlotIsNotAvailable()
    {
        var slotId = Guid.NewGuid();
        var slot = new SlotEntity { SlotId = slotId, Status = SlotStatus.Occupied };
        _repoMock.Setup(x => x.GetByIdAsync(slotId)).ReturnsAsync(slot);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReserveSlotAsync(slotId));
    }

    // ── ConfirmSlotAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task ConfirmSlotAsync_ShouldChangeStatus_ToOccupied()
    {
        var slotId = Guid.NewGuid();
        var slot = new SlotEntity { SlotId = slotId, Status = SlotStatus.Reserved };
        _repoMock.Setup(x => x.GetByIdAsync(slotId)).ReturnsAsync(slot);

        await _service.ConfirmSlotAsync(slotId);

        Assert.That(slot.Status, Is.EqualTo(SlotStatus.Occupied));
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // ── ReleaseSlotAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task ReleaseSlotAsync_ShouldChangeStatus_ToAvailable()
    {
        var slotId = Guid.NewGuid();
        var slot = new SlotEntity { SlotId = slotId, Status = SlotStatus.Reserved };
        _repoMock.Setup(x => x.GetByIdAsync(slotId)).ReturnsAsync(slot);

        await _service.ReleaseSlotAsync(slotId);

        Assert.That(slot.Status, Is.EqualTo(SlotStatus.Available));
    }

    [Test]
    public async Task ReleaseSlotAsync_ShouldBeIdempotent_WhenAlreadyAvailable()
    {
        var slotId = Guid.NewGuid();
        var slot = new SlotEntity { SlotId = slotId, Status = SlotStatus.Available };
        _repoMock.Setup(x => x.GetByIdAsync(slotId)).ReturnsAsync(slot);

        // Should NOT throw, should NOT call SaveChangesAsync
        await _service.ReleaseSlotAsync(slotId);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    // ── UpdatePricesByLotAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdatePricesByLotAsync_ShouldUpdateAllSlotsInLot()
    {
        var lotId = Guid.NewGuid();
        var slots = new List<SlotEntity>
        {
            new SlotEntity { LotId = lotId, PricePerHour = 10m },
            new SlotEntity { LotId = lotId, PricePerHour = 10m }
        };
        _repoMock.Setup(x => x.GetByLotIdAsync(lotId)).ReturnsAsync(slots);

        await _service.UpdatePricesByLotAsync(lotId, 25.0m);

        Assert.That(slots.All(s => s.PricePerHour == 25.0m), Is.True);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
