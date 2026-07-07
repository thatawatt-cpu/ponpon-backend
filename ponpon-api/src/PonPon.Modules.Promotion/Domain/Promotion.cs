namespace PonPon.Modules.Promotion.Domain;

public sealed class Promotion
{
    private Promotion()
    {
        Name = string.Empty;
        Type = string.Empty;
        DiscountType = string.Empty;
        Timezone = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid? CampaignId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Type { get; private set; }
    public string DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal MinimumSubtotal { get; private set; }
    public decimal? MaximumDiscount { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public string Timezone { get; private set; }
    public int Priority { get; private set; }
    public bool CanStackWithCoupon { get; private set; }
    public bool CanStackWithPromotions { get; private set; }
    public bool CanCombineWithFlashSale { get; private set; }
    public int? MaximumTotalUses { get; private set; }
    public int? MaximumUsesPerCustomer { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public List<PromotionScheduleRule> ScheduleRules { get; private set; } = [];
    public List<PromotionScope> Scopes { get; private set; } = [];
    public List<PromotionCustomerScope> CustomerScopes { get; private set; } = [];
    public List<PromotionCondition> Conditions { get; private set; } = [];

    public static Promotion Create(PromotionInput input, DateTime now)
    {
        var promotion = new Promotion { Id = Guid.NewGuid(), CreatedAtUtc = now };
        promotion.Update(input, now);
        return promotion;
    }

    public void Update(PromotionInput input, DateTime now)
    {
        CampaignId = input.CampaignId;
        Name = input.Name.Trim();
        Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        Type = input.Type.Trim().ToLowerInvariant();
        DiscountType = input.DiscountType.Trim().ToLowerInvariant();
        DiscountValue = input.DiscountValue;
        MinimumSubtotal = input.MinimumSubtotal;
        MaximumDiscount = input.MaximumDiscount;
        StartsAtUtc = input.StartsAtUtc;
        EndsAtUtc = input.EndsAtUtc;
        Timezone = string.IsNullOrWhiteSpace(input.Timezone) ? "Asia/Bangkok" : input.Timezone.Trim();
        Priority = input.Priority;
        CanStackWithCoupon = input.CanStackWithCoupon;
        CanStackWithPromotions = input.CanStackWithPromotions;
        CanCombineWithFlashSale = input.CanCombineWithFlashSale;
        MaximumTotalUses = input.MaximumTotalUses;
        MaximumUsesPerCustomer = input.MaximumUsesPerCustomer;
        IsActive = input.IsActive;
        UpdatedAtUtc = now;
        ScheduleRules.Clear();
        ScheduleRules.AddRange(input.ScheduleRules.Select(PromotionScheduleRule.Create));
        Scopes.Clear();
        Scopes.AddRange(input.Scopes.Select(PromotionScope.Create));
        CustomerScopes.Clear();
        CustomerScopes.AddRange(input.CustomerScopes.Select(PromotionCustomerScope.Create));
        Conditions.Clear();
        Conditions.AddRange(input.Conditions.Select(PromotionCondition.Create));
    }

    public void IncrementUsage() => UsedCount++;
    public void DecrementUsage() => UsedCount = Math.Max(0, UsedCount - 1);

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

public sealed record PromotionInput(
    string Name,
    string? Description,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    string? Timezone,
    int Priority,
    bool CanStackWithCoupon,
    bool CanStackWithPromotions,
    bool CanCombineWithFlashSale,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    bool IsActive,
    IReadOnlyCollection<PromotionScheduleRuleInput>? ScheduleRules = null,
    IReadOnlyCollection<PromotionScopeInput>? Scopes = null,
    IReadOnlyCollection<PromotionCustomerScopeInput>? CustomerScopes = null,
    IReadOnlyCollection<PromotionConditionInput>? Conditions = null,
    Guid? CampaignId = null)
{
    public IReadOnlyCollection<PromotionScheduleRuleInput> ScheduleRules { get; } = ScheduleRules ?? [];
    public IReadOnlyCollection<PromotionScopeInput> Scopes { get; } = Scopes ?? [];
    public IReadOnlyCollection<PromotionCustomerScopeInput> CustomerScopes { get; } = CustomerScopes ?? [];
    public IReadOnlyCollection<PromotionConditionInput> Conditions { get; } = Conditions ?? [];
}

public sealed class PromotionScheduleRule
{
    private PromotionScheduleRule()
    {
        Type = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public string Type { get; private set; }
    public int? DayOfWeek { get; private set; }
    public int? DayOfMonth { get; private set; }
    public TimeOnly? StartsAtLocalTime { get; private set; }
    public TimeOnly? EndsAtLocalTime { get; private set; }

    public static PromotionScheduleRule Create(PromotionScheduleRuleInput input) => new()
    {
        Id = Guid.NewGuid(),
        Type = input.Type.Trim().ToLowerInvariant(),
        DayOfWeek = input.DayOfWeek,
        DayOfMonth = input.DayOfMonth,
        StartsAtLocalTime = input.StartsAtLocalTime,
        EndsAtLocalTime = input.EndsAtLocalTime
    };
}

public sealed record PromotionScheduleRuleInput(
    string Type,
    int? DayOfWeek = null,
    int? DayOfMonth = null,
    TimeOnly? StartsAtLocalTime = null,
    TimeOnly? EndsAtLocalTime = null);

public sealed class PromotionScope
{
    private PromotionScope()
    {
        Type = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public string Type { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? Sku { get; private set; }
    public string? CategoryName { get; private set; }
    public bool IsExclude { get; private set; }

    public static PromotionScope Create(PromotionScopeInput input) => new()
    {
        Id = Guid.NewGuid(),
        Type = input.Type.Trim().ToLowerInvariant(),
        ProductId = input.ProductId,
        VariantId = input.VariantId,
        Sku = string.IsNullOrWhiteSpace(input.Sku) ? null : input.Sku.Trim().ToUpperInvariant(),
        CategoryName = string.IsNullOrWhiteSpace(input.CategoryName) ? null : input.CategoryName.Trim(),
        IsExclude = input.IsExclude
    };
}

public sealed record PromotionScopeInput(
    string Type,
    Guid? ProductId = null,
    Guid? VariantId = null,
    string? Sku = null,
    string? CategoryName = null,
    bool IsExclude = false);

public sealed class PromotionCustomerScope
{
    private PromotionCustomerScope()
    {
        Type = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public string Type { get; private set; }
    public Guid? CustomerId { get; private set; }

    public static PromotionCustomerScope Create(PromotionCustomerScopeInput input) => new()
    {
        Id = Guid.NewGuid(),
        Type = input.Type.Trim().ToLowerInvariant(),
        CustomerId = input.CustomerId
    };
}

public sealed record PromotionCustomerScopeInput(
    string Type,
    Guid? CustomerId = null);

public sealed class PromotionCondition
{
    private PromotionCondition()
    {
        Type = string.Empty;
        Value = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public string Type { get; private set; }
    public string Value { get; private set; }

    public static PromotionCondition Create(PromotionConditionInput input) => new()
    {
        Id = Guid.NewGuid(),
        Type = input.Type.Trim().ToLowerInvariant(),
        Value = input.Value.Trim().ToLowerInvariant()
    };
}

public sealed record PromotionConditionInput(string Type, string Value);
