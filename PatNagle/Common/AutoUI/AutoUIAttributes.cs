using System;

namespace PatNagle.Common.AutoUI;

/// <summary>Marks a property to be rendered in the auto-generated settings UI.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyMemberAttribute : Attribute
{
}

/// <summary>Slider bounds for a numeric property. Args mirror the old TiqUtils.Wpf order.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SliderLimitsAttribute : Attribute
{
    public SliderLimitsAttribute(double min, double max, double toolTipPrecision, double tickFrequency, double largeChange)
    {
        Min = min;
        Max = max;
        ToolTipPrecision = toolTipPrecision;
        TickFrequency = tickFrequency;
        LargeChange = largeChange;
    }

    public double Min { get; }
    public double Max { get; }
    public double ToolTipPrecision { get; }
    public double TickFrequency { get; }
    public double LargeChange { get; }
}

/// <summary>Groups properties under a labelled section in the settings UI.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyGroupAttribute : Attribute
{
    public PropertyGroupAttribute(string groupName) => GroupName = groupName;

    public string GroupName { get; }
}

/// <summary>Explicit display order within a group (lower first).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyOrderAttribute : Attribute
{
    public PropertyOrderAttribute(int order) => Order = order;

    public int Order { get; }
}
