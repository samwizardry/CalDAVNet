using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class PropertyUpdateBuilderTests
{
    [Fact]
    public void Build_InitializesPropertyUpdateRootWithSetAndRemove()
    {
        var builder = new PropertyUpdateBuilder();
        var doc = builder.Build();

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.PropertyUpdate);
        doc.Root.Element(XNames.Set).Should().NotBeNull();
        doc.Root.Element(XNames.Remove).Should().NotBeNull();
    }

    [Fact]
    public void AddDisplayName_AddsToSetProps()
    {
        var doc = new PropertyUpdateBuilder()
            .AddDisplayName("New Calendar Name")
            .Build();

        var setProp = doc.Root!.Element(XNames.Set)!.Element(XNames.Prop)!;
        setProp.Element(XNames.DisplayName)!.Value.Should().Be("New Calendar Name");
    }

    [Fact]
    public void RemoveDisplayName_AddsToRemoveProps()
    {
        var doc = new PropertyUpdateBuilder()
            .RemoveDisplayName()
            .Build();

        var removeProp = doc.Root!.Element(XNames.Remove)!.Element(XNames.Prop)!;
        removeProp.Element(XNames.DisplayName).Should().NotBeNull();
    }

    [Fact]
    public void AddDescription_And_RemoveDescription()
    {
        var doc = new PropertyUpdateBuilder()
            .AddDescription("Updated description")
            .RemoveDescription()
            .Build();

        var setProp = doc.Root!.Element(XNames.Set)!.Element(XNames.Prop)!;
        setProp.Element(XNames.CalendarDescription)!.Value.Should().Be("Updated description");

        var removeProp = doc.Root!.Element(XNames.Remove)!.Element(XNames.Prop)!;
        removeProp.Element(XNames.CalendarDescription).Should().NotBeNull();
    }

    [Fact]
    public void AddColor_And_RemoveColor()
    {
        var doc = new PropertyUpdateBuilder()
            .AddColor("#ff5500")
            .RemoveColor()
            .Build();

        var setProp = doc.Root!.Element(XNames.Set)!.Element(XNames.Prop)!;
        setProp.Element(XNames.CalendarColor)!.Value.Should().Be("#ff5500");

        var removeProp = doc.Root!.Element(XNames.Remove)!.Element(XNames.Prop)!;
        removeProp.Element(XNames.CalendarColor).Should().NotBeNull();
    }

    [Fact]
    public void AddProp_And_RemoveProp_CustomElements()
    {
        var customSet = new XElement(XName.Get("custom-set", "http://example.com/ns"), "value");
        var customRemove = new XElement(XName.Get("custom-remove", "http://example.com/ns"));

        var doc = new PropertyUpdateBuilder()
            .AddProp(customSet)
            .RemoveProp(customRemove)
            .Build();

        var setProp = doc.Root!.Element(XNames.Set)!.Element(XNames.Prop)!;
        setProp.Element(customSet.Name)!.Value.Should().Be("value");

        var removeProp = doc.Root!.Element(XNames.Remove)!.Element(XNames.Prop)!;
        removeProp.Element(customRemove.Name).Should().NotBeNull();
    }
}
