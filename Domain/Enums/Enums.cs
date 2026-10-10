using System.ComponentModel;

namespace Domain.Enums;

public enum ERoles
{
    MasterAdmin = 1,
    Admin,
    Standard
}

public static class RoleNames
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string Admin = "ADMIN";
    public const string Manager = "MANAGER";
    public const string Seller = "SELLER";
    public const string Standard = "STANDARD";
}


public static class BillTypes
{
    public const string Hourly = "HOURLY";
    public const string Daily = "DAILY";
    public const string Weekly = "WEEKLY";
    public const string Monthly = "MONTHLY";
    public const string Yearly = "YEARLY";
}

public static class PaymentStatuses
{
    public const string PAID = "PAID";
    public const string UNPAID = "UNPAID";
}

public static class BankTransactionTypes
{
    public const string Deposit = "DEPOSIT";
    public const string Withdraw = "WITHDRAW";

}

public enum ECustomerType
{
    Regular = 1,
    Corporate
}

[DefaultValue(ECodeGeneration.Auto)]
public enum ECodeGeneration
{
    Auto = 0,      // GUID-based (default, backward compatible)
    DailyCount = 1, // Sequential daily reset
    Company = 2,    // Company-wide sequential
    Branch = 3      // Branch-level sequential
}



public static class RecurringChargeTriggerTypes
{
    public const string Auto = "AUTO";
    public const string Manual = "MANUAL";
}

public static class RecurringChargeRunStatuses
{
    public const string InProgress = "IN_PROGRESS";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
}

/// <summary>
/// How a RecurringChargeEntry was created: INITIAL (at booking time) or RUN (from a manual/auto recurring-charge run).
/// </summary>
public static class RecurringChargeSources
{
    // Legacy values (before the charge ledger)
    public const string Initial = "INITIAL";
    public const string Run = "RUN";

    /// <summary>First posting of a charge on its natural date.</summary>
    public const string Posted = "POSTED";
    /// <summary>Difference posted later because booking/delivery data changed (dated the day it is posted).</summary>
    public const string Correction = "CORRECTION";
}

/// <summary>What a charge-ledger entry (RecurringChargeEntry) charges for.</summary>
public static class ChargeTypes
{
    /// <summary>One billing month: stock in store at the start of the month × rate.</summary>
    public const string RentCycle = "RENT_CYCLE";
    public const string BookingLabour = "BOOKING_LABOUR";
    public const string DeliveryLabour = "DELIVERY_LABOUR";
    public const string DeliveryAdjustment = "DELIVERY_ADJUSTMENT";
    /// <summary>Entries created before the ledger existed; kept for history, not counted.</summary>
    public const string Legacy = "LEGACY";
}