namespace Cenvo.Modules.CarWash.Domain;

/// <summary>When a branch reminds its customers, and what the messages say. One row per branch; defaults until saved.</summary>
public class ReminderSettings : BaseEntity, IBranchScoped
{
    public const int MaxDays = 365;
    public const int MaxLowUses = 10;

    public const string DefaultReturnTemplate =
        "{chinhanh}: Chào {ten}, xe {bienso} đã {songay} ngày chưa rửa. Ghé {chinhanh} để xe luôn sạch đẹp nhé! LH {sdtchinhanh}";
    public const string DefaultExpiringTemplate =
        "{chinhanh}: Chào {ten}, gói {goi} của bạn còn {conlai} lượt, hết hạn ngày {hethan}. Ghé dùng kẻo phí nhé! LH {sdtchinhanh}";
    public const string DefaultLowUsesTemplate =
        "{chinhanh}: Chào {ten}, gói {goi} của bạn còn {conlai} lượt. Gia hạn gói tại quầy để tiếp tục ưu đãi nhé! LH {sdtchinhanh}";

    public Guid BranchId { get; set; }
    /// <summary>Remind a customer this many days after their latest wash.</summary>
    public int ReturnAfterDays { get; set; } = 14;
    /// <summary>Remind when a package with uses left expires within this many days.</summary>
    public int ExpiringWithinDays { get; set; } = 7;
    /// <summary>Remind when a package has this many uses left or fewer.</summary>
    public int LowUsesThreshold { get; set; } = 1;
    public string ReturnTemplate { get; set; } = DefaultReturnTemplate;
    public string ExpiringTemplate { get; set; } = DefaultExpiringTemplate;
    public string LowUsesTemplate { get; set; } = DefaultLowUsesTemplate;

    public void Update(int returnAfterDays, int expiringWithinDays, int lowUsesThreshold,
        string returnTemplate, string expiringTemplate, string lowUsesTemplate)
    {
        if (returnAfterDays is < 1 or > MaxDays || expiringWithinDays is < 1 or > MaxDays)
            throw new BusinessException(CarWashErrors.InvalidReminderDays);
        if (lowUsesThreshold is < 0 or > MaxLowUses) throw new BusinessException(CarWashErrors.InvalidLowUsesThreshold);

        ReturnAfterDays = returnAfterDays;
        ExpiringWithinDays = expiringWithinDays;
        LowUsesThreshold = lowUsesThreshold;
        ReturnTemplate = CheckTemplate(returnTemplate);
        ExpiringTemplate = CheckTemplate(expiringTemplate);
        LowUsesTemplate = CheckTemplate(lowUsesTemplate);
    }

    private static string CheckTemplate(string template)
    {
        var text = template?.Trim() ?? "";
        if (text == "") throw new BusinessException(CarWashErrors.ReminderTemplateRequired);
        if (text.Length > ReminderTemplate.MaxLength) throw new BusinessException(CarWashErrors.ReminderTemplateTooLong);
        var unknown = ReminderTemplate.UnknownPlaceholders(text);
        if (unknown.Count > 0) throw new BusinessException(CarWashErrors.UnknownPlaceholders(unknown));
        return text;
    }
}
