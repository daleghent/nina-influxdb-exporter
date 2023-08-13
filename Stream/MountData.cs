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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class MountData : ITelescopeConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly ITelescopeMediator telescopeMediator;

        public MountData(IInfluxDbExporterOptions options, ITelescopeMediator telescopeMediator) {
            this.options = options;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
        }

        public void SendMountInfo() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!TelescopeInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(TelescopeInfo.Altitude) ? 0d : TelescopeInfo.Altitude;
            points.Add(PointData.Measurement("mount_altitude")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(TelescopeInfo.Azimuth) ? 0d : TelescopeInfo.Azimuth;
            points.Add(PointData.Measurement("mount_azimuth")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
            writeApi.Flush();
            writeApi.Dispose();
        }

        private TelescopeInfo TelescopeInfo { get; set; }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
            SendMountInfo();
        }

        public void Dispose() {
            telescopeMediator.RemoveConsumer(this);
        }
    }
}