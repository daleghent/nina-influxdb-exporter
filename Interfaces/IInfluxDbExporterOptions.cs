#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using System.ComponentModel;

namespace DaleGhent.NINA.InfluxDbExporter.Interfaces {

    public interface IInfluxDbExporterOptions : INotifyPropertyChanged {
        string InfluxDbUrl { get; set; }
        string InfluxDbBucket { get; set; }
        string InfluxDbOrgId { get; set; }
        string InfluxDbToken { get; set; }
        bool TagFullImagePath { get; set; }
        bool TagHostname { get; set; }
        bool TagProfileName { get; set; }
        bool TagEquipmentName { get; set; }
        string Hostname { get; }
        string ProfileName { get; }
    }
}