namespace Cenvo.Modules.Cafe.Domain.Calculations;

/// <summary>Which kitchen status changes are allowed. Pure function for unit testing.</summary>
public static class KitchenFlow
{
    /// <summary>
    /// Forward by any number of steps (Sent → Done is fine); back by exactly one step to undo a mistaken tap;
    /// same status is a harmless no-op (two screens tapping at once). Never into or out of New: only "send" does that.
    /// </summary>
    public static bool CanMove(KitchenStatus from, KitchenStatus to) =>
        from != KitchenStatus.New && to != KitchenStatus.New && to >= from - 1;
}
