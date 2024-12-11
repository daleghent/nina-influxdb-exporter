#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using DaleGhent.NINA.InfluxDbExporter.Utilities;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class SwitchData : ISwitchConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly ISwitchMediator switchMediator;

        public SwitchData(IInfluxDbExporterOptions options, ISwitchMediator switchMediator) {
            this.options = options;
            this.switchMediator = switchMediator;
            this.switchMediator.RegisterConsumer(this);
        }

        private async void SendSwitchData() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!SwitchInfo.Connected) return;
            if (SwitchInfo.ReadonlySwitches == null) return;
            if (SwitchInfo.ReadonlySwitches.Count < 1) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            foreach (var roSwitch in SwitchInfo.ReadonlySwitches) {
                if (!double.IsNaN(roSwitch.Value)) {
                    points.Add(PointData.Measurement($"switch_ro_sw{roSwitch.Id}")
                        .Tag("name", roSwitch.Name)
                        .Field("value", roSwitch.Value)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }
            }

            if (points.Count > 0) {
                // Send the points
                var fullOptions = new InfluxDBClientOptions(options.InfluxDbUrl) {
                    Token = options.InfluxDbToken,
                    Bucket = options.InfluxDbBucket,
                    Org = options.InfluxDbOrgId,
                };

                if (options.TagProfileName) {
                    fullOptions.AddDefaultTag("profile_name", options.ProfileName);
                }

                if (options.TagHostname) {
                    fullOptions.AddDefaultTag("host_name", options.Hostname);
                }

                if (options.TagEquipmentName) {
                    fullOptions.AddDefaultTag("switch_name", SwitchInfo.Name);
                }

                using var client = new InfluxDBClient(fullOptions);

                try {
                    var writeApi = client.GetWriteApiAsync();
                    await writeApi.WritePointsAsync(points);
                } catch (Exception ex) {
                    Logger.Error($"Failed to write switch points: {ex.Message}");
                }
            }
        }

        private SwitchInfo SwitchInfo { get; set; }

        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            SwitchInfo = deviceInfo;
            SendSwitchData();
        }

        public void Dispose() {
            switchMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}