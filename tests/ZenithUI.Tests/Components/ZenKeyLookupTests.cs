using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenKeyLookup: the lookup bound to a key of the record. The popup, keyboard and commit behaviour
/// are ZenLookup's and are tested there; these pin the mapping between the key and the record.
/// </summary>
public class ZenKeyLookupTests : BunitContext
{
    public ZenKeyLookupTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // A class without value equality, as an entity or DTO usually is: matching has to go by key.
    private sealed class Vendor(Guid id, string name, string account)
    {
        public Guid Id { get; } = id;

        public string Name { get; } = name;

        public string Account { get; } = account;
    }

    private static readonly Vendor[] Vendors =
    [
        new(Guid.Parse("0b0e5a4e-0000-0000-0000-000000000001"), "AWS", "ACT-9012"),
        new(Guid.Parse("0b0e5a4e-0000-0000-0000-000000000002"), "Cloudflare", "ACT-7104"),
        new(Guid.Parse("0b0e5a4e-0000-0000-0000-000000000003"), "Google Cloud", "ACT-3419"),
    ];

    private static RenderFragment Columns => builder =>
    {
        builder.OpenComponent<ZenColumn<Vendor>>(0);
        builder.AddComponentParameter(1, nameof(ZenColumn<Vendor>.Title), "Vendor");
        builder.AddComponentParameter(2, nameof(ZenColumn<Vendor>.Field), (Func<Vendor, object?>)(v => v.Name));
        builder.CloseComponent();
    };

    private Guid _id;
    private Guid? _nullableId;

    private IRenderedComponent<ZenKeyLookup<Vendor, Guid>> Lookup(
        Action<ComponentParameterCollectionBuilder<ZenKeyLookup<Vendor, Guid>>>? extra = null) =>
        Render<ZenKeyLookup<Vendor, Guid>>(p =>
        {
            p.Add(x => x.Items, Vendors)
                .Add(x => x.Columns, Columns)
                .Add(x => x.ValueField, v => v.Id)
                .Add(x => x.ItemText, v => v.Name)
                .Add(x => x.Label, "Vendor")
                .Add(x => x.Value, _id)
                .Add(x => x.ValueChanged, v => _id = v);

            extra?.Invoke(p);
        });

    private IRenderedComponent<ZenKeyLookup<Vendor, Guid?>> NullableLookup() =>
        Render<ZenKeyLookup<Vendor, Guid?>>(p => p
            .Add(x => x.Items, Vendors)
            .Add(x => x.Columns, Columns)
            .Add(x => x.ValueField, v => v.Id)
            .Add(x => x.ItemText, v => v.Name)
            .Add(x => x.Value, _nullableId)
            .Add(x => x.ValueChanged, v => _nullableId = v));

    private static void Open<TValue>(IRenderedComponent<ZenKeyLookup<Vendor, TValue>> cut) =>
        cut.Find("input[role=combobox]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

    [Fact]
    public void TheFieldShowsTheRecordTheKeyNames()
    {
        _id = Vendors[1].Id;

        Lookup().Find("input").GetAttribute("value").ShouldBe("Cloudflare");
    }

    [Fact]
    public void AnEmptyGuid_ShowsNothing_AndOffersNoClear()
    {
        var cut = Lookup();

        cut.Find("input").GetAttribute("value").ShouldBe(string.Empty);
        cut.FindAll("button[aria-label='Clear selection']").ShouldBeEmpty();
    }

    [Fact]
    public void Picking_WritesTheKey()
    {
        var cut = Lookup();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[2].Click();

        _id.ShouldBe(Vendors[2].Id);
        cut.Find("input").GetAttribute("value").ShouldBe("Google Cloud");
    }

    [Fact]
    public void TheRowForTheKey_IsSelected_WithoutValueEqualityOnTheRecord()
    {
        _id = Vendors[1].Id;
        var cut = Lookup();
        Open(cut);

        var rows = cut.FindAll("[role=grid] tbody tr");
        rows[1].GetAttribute("aria-selected").ShouldBe("true");
        rows[0].GetAttribute("aria-selected").ShouldBe("false");

        // And the cursor starts on it.
        var active = cut.Find("input").GetAttribute("aria-activedescendant");
        cut.Find($"#{active}").TextContent.Trim().ShouldBe("Cloudflare");
    }

    [Fact]
    public void Clearing_WritesEmptyGuid()
    {
        _id = Vendors[0].Id;
        var cut = Lookup();

        cut.Find("button[aria-label='Clear selection']").Click();

        _id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Clearing_ANullableKey_WritesNull()
    {
        _nullableId = Vendors[0].Id;
        var cut = NullableLookup();

        cut.Find("button[aria-label='Clear selection']").Click();

        _nullableId.ShouldBeNull();
    }

    [Fact]
    public void ConfirmMode_WritesTheKeyOnlyOnConfirm()
    {
        var cut = Lookup(p => p.Add(x => x.Commit, ZenLookupCommit.Confirm));
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[1].Click();
        _id.ShouldBe(Guid.Empty);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Confirm").Click();
        _id.ShouldBe(Vendors[1].Id);
    }

    [Fact]
    public void AKeyChangedFromOutside_UpdatesTheField()
    {
        var cut = Lookup();

        cut.Render(p => p.Add(x => x.Value, Vendors[2].Id));

        cut.Find("input").GetAttribute("value").ShouldBe("Google Cloud");
    }

    [Fact]
    public void ItemResolver_FetchesARecordNoPageHasLoaded()
    {
        // A form opened on an existing row, over a server-side provider: the key is known, the
        // record is not.
        var resolved = new List<Guid>();
        _id = Vendors[2].Id;

        var cut = Render<ZenKeyLookup<Vendor, Guid>>(p => p
            .Add(x => x.Columns, Columns)
            .Add(x => x.ValueField, v => v.Id)
            .Add(x => x.ItemText, v => v.Name)
            .Add(x => x.Value, _id)
            .Add(x => x.ItemsProvider, _ => ValueTask.FromResult(new ZenTableResult<Vendor>([], 0)))
            .Add(x => x.ItemResolver, (id, _) =>
            {
                resolved.Add(id);
                return ValueTask.FromResult<Vendor?>(Vendors.Single(v => v.Id == id));
            }));

        cut.WaitForAssertion(() => cut.Find("input").GetAttribute("value").ShouldBe("Google Cloud"));
        resolved.ShouldBe([Vendors[2].Id]);
    }

    [Fact]
    public void APick_DoesNotCallTheResolver()
    {
        var calls = 0;

        var cut = Lookup(p => p.Add(x => x.ItemResolver, (_, _) =>
        {
            calls++;
            return ValueTask.FromResult<Vendor?>(null);
        }));
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[0].Click();
        cut.Render(p => p.Add(x => x.Value, _id));

        calls.ShouldBe(0);
    }

    [Fact]
    public void WithoutValueField_ItSaysSo()
    {
        var ex = Should.Throw<InvalidOperationException>(() =>
            Render<ZenKeyLookup<Vendor, Guid>>(p => p
                .Add(x => x.Items, Vendors)
                .Add(x => x.Columns, Columns)));

        ex.Message.ShouldContain("ValueField");
    }

    // ---- In a form ----------------------------------------------------------------------------

    private sealed class Order
    {
        [Required(ErrorMessage = "Pick a vendor.")]
        public Guid? VendorId { get; set; }
    }

    [Fact]
    public void BindsAFormModelsKey_AndValidatesIt()
    {
        var order = new Order();

        var cut = Render<EditForm>(p => p
            .Add(x => x.Model, order)
            .Add(x => x.ChildContent, _ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                builder.OpenComponent<ZenKeyLookup<Vendor, Guid?>>(1);
                builder.AddComponentParameter(2, "Items", Vendors);
                builder.AddComponentParameter(3, "Columns", Columns);
                builder.AddComponentParameter(4, "ValueField", (Func<Vendor, Guid?>)(v => v.Id));
                builder.AddComponentParameter(5, "Label", "Vendor");
                builder.AddComponentParameter(6, "Value", order.VendorId);
                builder.AddComponentParameter(7, "ValueChanged",
                    EventCallback.Factory.Create<Guid?>(this, v => order.VendorId = v));
                builder.AddComponentParameter(8, "ValueExpression",
                    (Expression<Func<Guid?>>)(() => order.VendorId));
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();
        cut.Find("[role=alert]").TextContent.ShouldContain("Pick a vendor.");

        cut.Find("input[role=combobox]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.FindAll("[role=grid] tbody tr")[1].Click();

        order.VendorId.ShouldBe(Vendors[1].Id);
        cut.FindAll("[role=alert]").ShouldBeEmpty();
    }
}
