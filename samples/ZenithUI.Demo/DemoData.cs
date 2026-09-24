using ZenithUI;

namespace ZenithUI.Demo;

/// <summary>A customer account, for the server-backed table.</summary>
public sealed record Customer(string Account, string Name, string Country, decimal LifetimeValue);

/// <summary>
/// Stands in for a customer API: pages and sorts on the "server", and takes long enough that the
/// loading state is visible.
/// </summary>
public sealed class CustomerService
{
    /// <summary>How long each request takes.</summary>
    public const int LatencyMilliseconds = 450;

    private static readonly string[] Countries =
        ["Portugal", "Ireland", "Poland", "Austria", "Denmark", "Italy", "Spain", "Belgium"];

    private static readonly string[] Words =
        ["Harbour", "Summit", "Juniper", "Beacon", "Meridian", "Copper", "Willow", "Granite", "Atlas", "Orchard"];

    private static readonly string[] Suffixes = ["Trading", "Holdings", "Supply", "Group", "Partners"];

    private readonly List<Customer> _all = [.. Enumerable.Range(1, 2_500).Select(i => new Customer(
        $"C-{10_000 + i}",
        $"{Words[i * 7 % Words.Length]} {Words[i * 3 % Words.Length]} {Suffixes[i % Suffixes.Length]}",
        Countries[i * 11 % Countries.Length],
        (i * 7_919 % 480_000) + 1_200m))];

    /// <summary>A ZenTable ItemsProvider: one sorted window of the customers.</summary>
    public async ValueTask<ZenTableResult<Customer>> QueryAsync(ZenTableRequest request)
    {
        await Task.Delay(LatencyMilliseconds, request.CancellationToken);

        IEnumerable<Customer> rows = request.SortName switch
        {
            "account" => Order(c => c.Account),
            "name" => Order(c => c.Name),
            "country" => Order(c => c.Country),
            "value" => Order(c => c.LifetimeValue),
            _ => _all,
        };

        var window = rows.Skip(request.StartIndex);

        if (request.Count is { } count)
        {
            window = window.Take(count);
        }

        return new ZenTableResult<Customer>([.. window], _all.Count);

        IEnumerable<Customer> Order<TKey>(Func<Customer, TKey> key) =>
            request.SortDescending ? _all.OrderByDescending(key) : _all.OrderBy(key);
    }
}

/// <summary>A supplier account, for the lookup demos. Modelled on a procurement system.</summary>
public sealed record Vendor(
    string Name,
    string LegalName,
    string Website,
    string Account,
    string Category,
    decimal MonthlyVolume,
    string Region);

/// <summary>
/// Stands in for a vendor directory API: searches, sorts and windows on the "server".
/// </summary>
public sealed class VendorService
{
    /// <summary>A short, fixed list for the in-memory lookup.</summary>
    public static readonly IReadOnlyList<Vendor> Featured =
    [
        new("AWS Cloud Infrastructure", "Amazon Web Services, Inc.", "aws.amazon.com", "ACT-9012", "Cloud services", 24_500m, "US-East (N. Virginia)"),
        new("Cloudflare CDN & DNS", "Cloudflare, Inc.", "cloudflare.com", "ACT-7104", "Security & edge", 8_200m, "Global (Anycast)"),
        new("Google Cloud Platform", "Alphabet Inc.", "cloud.google.com", "ACT-3419", "Infrastructure", 19_400m, "US-Central (Iowa)"),
        new("Datadog Observability", "Datadog, Inc.", "datadoghq.com", "ACT-5502", "Monitoring", 4_150m, "US-West (Oregon)"),
        new("Fastly Edge Cloud", "Fastly, Inc.", "fastly.com", "ACT-6620", "Security & edge", 3_900m, "Global (Anycast)"),
        new("Hetzner Online", "Hetzner Online GmbH", "hetzner.com", "ACT-1187", "Infrastructure", 1_150m, "EU-Central (Falkenstein)"),
        new("OVHcloud", "OVH Groupe SA", "ovhcloud.com", "ACT-2044", "Infrastructure", 2_300m, "EU-West (Roubaix)"),
        new("PagerDuty", "PagerDuty, Inc.", "pagerduty.com", "ACT-8830", "Monitoring", 780m, "US-West (Oregon)"),
        new("Snowflake Data Cloud", "Snowflake Inc.", "snowflake.com", "ACT-4471", "Data", 12_750m, "US-East (N. Virginia)"),
        new("Twilio Communications", "Twilio Inc.", "twilio.com", "ACT-3908", "Messaging", 2_640m, "US-West (Oregon)"),
    ];

    private static readonly string[] Prefixes =
        ["Nimbus", "Stratus", "Vertex", "Quantum", "Helix", "Apex", "Lumen", "Cobalt", "Nova", "Pulse", "Zenith", "Orbit"];

    private static readonly string[] Kinds =
        ["Cloud", "Networks", "Analytics", "Security", "Storage", "Systems", "Labs", "Logistics"];

    private static readonly string[] Categories =
        ["Cloud services", "Security & edge", "Infrastructure", "Monitoring", "Data", "Messaging"];

    private static readonly string[] Regions =
        ["US-East (N. Virginia)", "US-West (Oregon)", "EU-Central (Frankfurt)", "EU-West (Dublin)", "APAC (Singapore)", "Global (Anycast)"];

    private readonly List<Vendor> _all =
    [
        .. Featured,
        .. Enumerable.Range(1, 5_000).Select(i =>
        {
            var name = $"{Prefixes[i * 7 % Prefixes.Length]} {Kinds[i * 5 % Kinds.Length]} {i:0000}";
            return new Vendor(
                name,
                $"{name} Ltd.",
                $"{name.Split(' ')[0].ToLowerInvariant()}{i}.example",
                $"ACT-{20_000 + i * 13 % 70_000}",
                Categories[i * 3 % Categories.Length],
                (i * 7_919 % 60_000) + 250m,
                Regions[i * 11 % Regions.Length]);
        }),
    ];

    /// <summary>How many vendors the directory holds.</summary>
    public int Count => _all.Count;

    /// <summary>A ZenLookup ItemsProvider: the matching vendors, one sorted window at a time.</summary>
    public async ValueTask<ZenTableResult<Vendor>> SearchAsync(ZenLookupRequest request)
    {
        await Task.Delay(250, request.CancellationToken);

        var query = request.Query;

        IEnumerable<Vendor> matches = query.Length == 0
            ? _all
            : _all.Where(v =>
                v.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                v.LegalName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                v.Account.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                v.Region.Contains(query, StringComparison.OrdinalIgnoreCase));

        matches = request.SortName switch
        {
            "name" => Order(matches, v => v.Name),
            "account" => Order(matches, v => v.Account),
            "volume" => Order(matches, v => v.MonthlyVolume),
            _ => matches,
        };

        var list = matches.ToList();
        var window = list.Skip(request.StartIndex);

        if (request.Count is { } count)
        {
            window = window.Take(count);
        }

        return new ZenTableResult<Vendor>([.. window], list.Count);

        IEnumerable<Vendor> Order<TKey>(IEnumerable<Vendor> rows, Func<Vendor, TKey> key) =>
            request.SortDescending ? rows.OrderByDescending(key) : rows.OrderBy(key);
    }
}
