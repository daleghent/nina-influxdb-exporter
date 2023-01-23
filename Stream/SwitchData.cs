#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class SwitchData : ISwitchConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly ISwitchMediator switchMediator;

        public SwitchData(IInfluxDbExporterOptions options, ISwitchMediator switchMediator) {
            this.options = options;
            this.switchMediator = switchMediator;
            this.switchMediator.RegisterConsumer(this);
        }

        private void SendSwitchData() {
            if (!SwitchInfo.Connected || SwitchInfo.ReadonlySwitches.Count == 0) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            foreach (var roSwitch in SwitchInfo.ReadonlySwitches) {
                if (!double.IsNaN(roSwitch.Value)) {
                    points.Add(PointData.Measurement($"switchROSwitch{roSwitch.Id}")
                        .Tag("name", roSwitch.Name)
                        .Field("value", roSwitch.Value)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }
            }

            if (points.Count > 0) {
                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbUserName, options.InfluxDbUserPassword, options.InfluxDbDbName, string.Empty);
                using var writeApi = client.GetWriteApi();
                writeApi.WritePoints(points);
                writeApi.Flush();
                writeApi.Dispose();
            }
        }

        private SwitchInfo SwitchInfo { get; set; }

        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            SwitchInfo = deviceInfo;
            SendSwitchData();
        }

        public void Dispose() {
            switchMediator.RegisterConsumer(this);
        }
    }
}