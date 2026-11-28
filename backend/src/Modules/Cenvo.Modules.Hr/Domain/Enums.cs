namespace Cenvo.Modules.Hr.Domain;

/// <summary>Where the person mainly works (reports and schedules group by it).</summary>
public enum Department { Cafe = 1, CarWash = 2, General = 3 }

public enum ContractType { Seasonal = 1, PartTime = 2, FullTime = 3 }

/// <summary>Advance = money paid early (deducted from pay); Bonus adds to pay; Penalty deducts.</summary>
public enum AdjustmentKind { Advance = 1, Bonus = 2, Penalty = 3 }

public enum AdjustmentStatus { Active = 1, Cancelled = 2 }
