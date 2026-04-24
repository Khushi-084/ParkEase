namespace Slot.Domain.Enums;

/// <summary>
/// Categories of parking slots.
/// FIXED: Added EV to match the case study requirement for electric vehicle charging spots.
/// Previously only had Car/Bike/Truck, which caused a 400 error when creating an EV slot.
/// </summary>
public enum SlotType
{
    Car,
    Bike,
    Truck,
    EV      // FIXED: electric vehicle charging slot
}