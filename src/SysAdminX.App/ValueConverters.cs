// -----------------------------------------------------------------------
// <copyright file="ValueConverters.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace SysAdminX.App.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Execute(object value) => value is bool b ? !b : value;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }
}

public class CapitalizeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns <c>true</c> when the bound value is non-null, <c>false</c> otherwise.
/// Useful for binding <c>IsEnabled</c> on a button whose command needs a
/// non-null selection (e.g. <c>IsEnabled="{Binding SelectedItem, Converter={...}}"</c>).
///
/// Pass <c>ConverterParameter=Invert</c> to invert the result (true when null).
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var hasValue = value != null;
        return invert ? !hasValue : hasValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns <c>true</c> when the bound string is non-empty, <c>false</c> otherwise.
/// Designed for binding <c>InfoBar.IsOpen</c> to an error-message string so
/// the bar auto-shows when there's a message and auto-hides when the message
/// is cleared.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrEmpty(s);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
