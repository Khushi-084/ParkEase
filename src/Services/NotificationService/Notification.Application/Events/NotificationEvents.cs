namespace Notification.Application.Events;

// Base event properties
public abstract record NotificationEvent(Guid UserId, string Email);

// --- Driver Events ---
public record BookingConfirmedEvent(Guid UserId, string Email, Guid BookingId, string LotName) : NotificationEvent(UserId, Email);
public record CheckInCompletedEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);
public record ParkingExpiryReminderEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);
public record CheckoutCompletedEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);
public record PaymentCompletedEvent(Guid UserId, string Email, decimal Amount) : NotificationEvent(UserId, Email);
public record BookingCancelledEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);

// --- Manager Events ---
public record NewBookingForManagerEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);
public record VehicleCheckInEvent(Guid UserId, string Email, string VehicleNumber) : NotificationEvent(UserId, Email);
public record VehicleEarlyCheckoutEvent(Guid UserId, string Email, string VehicleNumber) : NotificationEvent(UserId, Email);
public record BookingExtendedEvent(Guid UserId, string Email, Guid BookingId) : NotificationEvent(UserId, Email);
public record ForceCheckoutEvent(Guid UserId, string Email, string VehicleNumber) : NotificationEvent(UserId, Email);
public record LotManagerApprovedEvent(Guid UserId, string Email, string ManagerName) : NotificationEvent(UserId, Email);

// --- Admin Events ---
public record AdminBroadcastEvent(Guid UserId, string Email, string Title, string Message) : NotificationEvent(UserId, Email);
