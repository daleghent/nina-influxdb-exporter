#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace DaleGhent.NINA.InfluxDbExporter.Resources {

    [Export(typeof(ResourceDictionary))]
    public partial class OptionsDataTemplates : ResourceDictionary {

        public OptionsDataTemplates() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            var procStartInfo = new ProcessStartInfo() {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true,
            };

            _ = Process.Start(procStartInfo);
            e.Handled = true;
        }

        private void PasswordBox_InfluxDbUserPassword_Loaded(object sender, RoutedEventArgs e) {
            if (sender is PasswordBox elem) {
                if (elem.DataContext is InfluxDbExporterOptions vm) {
                    elem.Password = vm.InfluxDbUserPassword;
                }
            }
        }

        private void PasswordBox_InfluxDbUserPassword_PasswordChanged(object sender, RoutedEventArgs e) {
            if (sender is PasswordBox elem) {
                if (elem.DataContext is InfluxDbExporterOptions vm) {
                    vm.SetInfluxDbUserPassword(elem.SecurePassword);
                }
            }
        }
    }
}