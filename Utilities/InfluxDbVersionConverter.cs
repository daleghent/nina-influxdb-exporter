#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Enums;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    /// <summary>
    /// Returns Visibility.Visible when the bound InfluxDbVersion matches any version listed in
    /// ConverterParameter (comma-separated, e.g. "V1,V3").  Collapses otherwise.
    /// </summary>
    public class InfluxDbVersionConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not InfluxDbVersion current || parameter is not string param) {
                return Visibility.Collapsed;
            }

            var allowed = param.Split(',').Select(s => s.Trim());
            foreach (var name in allowed) {
                if (Enum.TryParse<InfluxDbVersion>(name, out var v) && v == current) {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
