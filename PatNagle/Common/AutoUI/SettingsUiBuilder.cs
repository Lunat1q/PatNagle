using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PatNagle.Common.AutoUI;

/// <summary>
///     Reflection-driven settings window. Avalonia replacement for TiqUtils.Wpf's
///     CreateAutoUISettingsDialog: reads [PropertyMember]/[SliderLimits]/[PropertyGroup]
///     and builds sliders, checkboxes and enum pickers bound directly to the live object.
/// </summary>
public static class SettingsUiBuilder
{
    // ponytail: only scalar/bool/enum members are rendered. Nested [PropertyMember] classes
    // aren't used by AppSettings, so recursion is omitted; add it if a nested settings class appears.
    public static Window CreateAutoUISettingsDialog(this object settings)
    {
        var type = settings.GetType();

        var panel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };

        var members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<PropertyMemberAttribute>() != null && p.CanRead && p.CanWrite)
            .ToList();

        // Preserve grouped, declaration-ish order: default group first, then named groups.
        var groups = members
            .GroupBy(p => p.GetCustomAttribute<PropertyGroupAttribute>()?.GroupName ?? string.Empty)
            .OrderBy(g => g.Key == string.Empty ? 0 : 1)
            .ThenBy(g => g.Key);

        foreach (var group in groups)
        {
            if (group.Key != string.Empty)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = group.Key,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 8, 0, 2)
                });
            }

            var ordered = group
                .OrderBy(p => p.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? int.MaxValue)
                .ThenBy(p => p.MetadataToken);

            foreach (var prop in ordered)
            {
                var row = BuildRow(settings, prop);
                if (row != null)
                {
                    panel.Children.Add(row);
                }
            }
        }

        var closeButton = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var root = new DockPanel();
        DockPanel.SetDock(closeButton, Dock.Bottom);
        root.Children.Add(closeButton);
        root.Children.Add(new ScrollViewer { Content = panel });

        var window = new Window
        {
            Title = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? "Settings",
            Width = 380,
            SizeToContent = SizeToContent.Height,
            MaxHeight = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = root
        };
        closeButton.Click += (_, _) => window.Close();

        return window;
    }

    private static Control? BuildRow(object owner, PropertyInfo prop)
    {
        var label = new TextBlock
        {
            Text = GetLabel(prop),
            VerticalAlignment = VerticalAlignment.Center
        };

        var editor = BuildEditor(owner, prop);
        if (editor == null)
        {
            return null;
        }

        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(label);
        stack.Children.Add(editor);
        return stack;
    }

    private static Control? BuildEditor(object owner, PropertyInfo prop)
    {
        var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (t == typeof(bool))
        {
            var cb = new CheckBox { IsChecked = (bool)(prop.GetValue(owner) ?? false) };
            cb.IsCheckedChanged += (_, _) => prop.SetValue(owner, cb.IsChecked ?? false);
            return cb;
        }

        if (t.IsEnum)
        {
            var combo = new ComboBox
            {
                ItemsSource = Enum.GetValues(t),
                SelectedItem = prop.GetValue(owner),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem != null)
                {
                    prop.SetValue(owner, combo.SelectedItem);
                }
            };
            return combo;
        }

        if (t == typeof(int) || t == typeof(double) || t == typeof(float))
        {
            return BuildSlider(owner, prop, t);
        }

        // Unsupported type: skip rather than crash.
        return null;
    }

    private static Control BuildSlider(object owner, PropertyInfo prop, Type t)
    {
        var limits = prop.GetCustomAttribute<SliderLimitsAttribute>();
        var current = Convert.ToDouble(prop.GetValue(owner), CultureInfo.InvariantCulture);

        var slider = new Slider
        {
            Minimum = limits?.Min ?? 0,
            Maximum = limits?.Max ?? 100,
            TickFrequency = limits?.TickFrequency ?? 1,
            SmallChange = limits?.TickFrequency ?? 1,
            LargeChange = limits?.LargeChange ?? 1,
            IsSnapToTickEnabled = t == typeof(int),
            Value = current
        };

        var valueText = new TextBlock
        {
            Text = FormatValue(current, t),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 40,
            TextAlignment = TextAlignment.Right
        };

        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property != Slider.ValueProperty)
            {
                return;
            }

            var v = slider.Value;
            if (t == typeof(int))
            {
                prop.SetValue(owner, (int)Math.Round(v));
            }
            else if (t == typeof(float))
            {
                prop.SetValue(owner, (float)v);
            }
            else
            {
                prop.SetValue(owner, v);
            }

            valueText.Text = FormatValue(v, t);
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        Grid.SetColumn(slider, 0);
        Grid.SetColumn(valueText, 1);
        grid.Children.Add(slider);
        grid.Children.Add(valueText);
        return grid;
    }

    private static string FormatValue(double value, Type t)
        => t == typeof(int)
            ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string GetLabel(PropertyInfo prop)
    {
        var display = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        return !string.IsNullOrWhiteSpace(display) ? display! : Beautify(prop.Name);
    }

    private static string Beautify(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
