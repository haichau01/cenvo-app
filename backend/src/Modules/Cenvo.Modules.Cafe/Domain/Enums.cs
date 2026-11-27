namespace Cenvo.Modules.Cafe.Domain;

public enum OrderType { DineIn = 1, TakeAway = 2 }

public enum OrderStatus { Open = 1, Paid = 2, Cancelled = 3 }

/// <summary>New = not sent to the kitchen → Sent → Preparing → Done (ready) → Served (brought to the table).</summary>
public enum KitchenStatus { New = 0, Sent = 1, Preparing = 2, Done = 3, Served = 4 }

public enum PaymentMethod { Cash = 1, Transfer = 2, Card = 3 }

public enum ShiftStatus { Open = 1, Closed = 2 }
