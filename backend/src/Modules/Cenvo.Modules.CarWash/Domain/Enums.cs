namespace Cenvo.Modules.CarWash.Domain;

/// <summary>Cars only (no motorbikes). Prices are set per vehicle type.</summary>
public enum VehicleType { Sedan = 1, SevenSeater = 2, Pickup = 3, Suv = 4 }

/// <summary>Waiting → Washing → Done (ready for pickup) → Delivered. Cancelled at any point with a reason.</summary>
public enum WashStatus { Waiting = 1, Washing = 2, Done = 3, Delivered = 4, Cancelled = 5 }

/// <summary>Commission per service: a percentage of the price, or a fixed amount per vehicle.</summary>
public enum CommissionType { Percent = 1, FixedAmount = 2 }

public enum MembershipStatus { Active = 1, Cancelled = 2 }
