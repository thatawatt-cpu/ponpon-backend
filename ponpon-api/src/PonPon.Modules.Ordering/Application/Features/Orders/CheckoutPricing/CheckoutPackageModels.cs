namespace PonPon.Modules.Ordering.Application.Features.Orders.CheckoutPricing;

public sealed record CheckoutShippingPackage(
    string BoxCode,
    decimal WidthCm,
    decimal LengthCm,
    decimal HeightCm,
    decimal WeightKg,
    int ItemCount);

internal sealed record ShippingBox(
    string Code,
    decimal WidthCm,
    decimal LengthCm,
    decimal HeightCm,
    decimal? MaxWeightKg = null)
{
    public decimal Volume => WidthCm * LengthCm * HeightCm;
    public decimal LongestSide => Math.Max(WidthCm, Math.Max(LengthCm, HeightCm));
    public decimal MiddleSide
    {
        get
        {
            var sides = new[] { WidthCm, LengthCm, HeightCm }.OrderBy(x => x).ToArray();
            return sides[1];
        }
    }
    public decimal ShortestSide => Math.Min(WidthCm, Math.Min(LengthCm, HeightCm));
}

internal sealed record PackableItem(
    decimal WidthCm,
    decimal LengthCm,
    decimal HeightCm,
    decimal WeightGrams)
{
    public decimal Volume => WidthCm * LengthCm * HeightCm;
    public decimal WeightKg => WeightGrams / 1000m;
    public decimal LongestSide => Math.Max(WidthCm, Math.Max(LengthCm, HeightCm));
    public decimal MiddleSide
    {
        get
        {
            var sides = new[] { WidthCm, LengthCm, HeightCm }.OrderBy(x => x).ToArray();
            return sides[1];
        }
    }
    public decimal ShortestSide => Math.Min(WidthCm, Math.Min(LengthCm, HeightCm));
}

internal sealed class CheckoutPackageDraft
{
    private const decimal VolumeBuffer = 1.15m;
    private readonly List<PackableItem> _items = [];

    public CheckoutPackageDraft(ShippingBox box)
    {
        Box = box;
    }

    public ShippingBox Box { get; private set; }
    public decimal UsedVolume => _items.Sum(x => x.Volume);
    public decimal WeightKg => _items.Sum(x => x.WeightKg);
    public int ItemCount => _items.Count;

    public bool TryAdd(PackableItem item, IReadOnlyCollection<ShippingBox> boxes)
    {
        var candidateItems = _items.Append(item).ToArray();
        var box = CheckoutPackagePacker.FindSmallestBox(candidateItems, boxes);
        if (box is null)
            return false;

        Box = box;
        _items.Add(item);
        return true;
    }

    public void AddFirst(PackableItem item)
    {
        _items.Add(item);
    }

    public CheckoutShippingPackage ToResponse()
        => new(Box.Code, Box.WidthCm, Box.LengthCm, Box.HeightCm, Math.Round(WeightKg, 3), ItemCount);

    public static bool FitsByVolume(IEnumerable<PackableItem> items, ShippingBox box)
        => items.Sum(x => x.Volume) * VolumeBuffer <= box.Volume
            && (!box.MaxWeightKg.HasValue || items.Sum(x => x.WeightKg) <= box.MaxWeightKg.Value);
}

internal static class CheckoutPackagePacker
{
    public static readonly IReadOnlyCollection<ShippingBox> DefaultBoxes =
    [
        new("OO", 14m, 9.75m, 6m),
        new("AA", 13m, 17m, 7m),
        new("1", 15m, 15m, 15m),
        new("C", 20m, 30m, 11m),
        new("2", 20m, 30m, 19m),
        new("3", 31m, 36m, 26m),
        new("4", 32m, 48m, 30m),
        new("H", 41m, 45m, 35m),
        new("I", 45m, 55m, 40m),
        new("UMBRELLA", 20m, 100m, 20m)
    ];

    public static IReadOnlyCollection<CheckoutShippingPackage>? Pack(IReadOnlyCollection<PackableItem> items)
    {
        var packages = new List<CheckoutPackageDraft>();
        foreach (var item in items.OrderByDescending(x => x.Volume))
        {
            if (FindSmallestBox([item], DefaultBoxes) is null)
                return null;

            var added = false;
            foreach (var package in packages.OrderBy(x => x.Box.Volume))
            {
                if (!package.TryAdd(item, DefaultBoxes))
                    continue;

                added = true;
                break;
            }

            if (added)
                continue;

            var box = FindSmallestBox([item], DefaultBoxes);
            if (box is null)
                return null;

            var newPackage = new CheckoutPackageDraft(box);
            newPackage.AddFirst(item);
            packages.Add(newPackage);
        }

        return packages.Select(x => x.ToResponse()).ToArray();
    }

    public static ShippingBox? FindSmallestBox(
        IReadOnlyCollection<PackableItem> items,
        IReadOnlyCollection<ShippingBox> boxes)
    {
        if (items.Count == 0)
            return null;

        var maxLongest = items.Max(x => x.LongestSide);
        var maxMiddle = items.Max(x => x.MiddleSide);
        var maxShortest = items.Max(x => x.ShortestSide);

        return boxes
            .Where(box =>
                box.LongestSide >= maxLongest
                && box.MiddleSide >= maxMiddle
                && box.ShortestSide >= maxShortest
                && CheckoutPackageDraft.FitsByVolume(items, box))
            .OrderBy(x => x.Volume)
            .FirstOrDefault();
    }
}
