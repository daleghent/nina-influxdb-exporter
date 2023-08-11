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
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class RotatorData : IRotatorConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IRotatorMediator rotatorMediator;

        public RotatorData(IInfluxDbExporterOptions options, IRotatorMediator rotatorMediator) {
            this.options = options;
            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);
        }

        private void SendRotatorInfo() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!RotatorInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            float valueFloat;

            valueFloat = float.IsNaN(RotatorInfo.MechanicalPosition) ? -1f : RotatorInfo.MechanicalPosition;
            points.Add(PointData.Measurement("rotatorMechanicalPosition")
                .Field("value", valueFloat)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueFloat = float.IsNaN(RotatorInfo.Position) ? -1f : RotatorInfo.Position;
            points.Add(PointData.Measurement("rotatorPosition")
                .Field("value", valueFloat)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
            writeApi.Flush();
            writeApi.Dispose();
        }

        private RotatorInfo RotatorInfo { get; set; }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            RotatorInfo = deviceInfo;
            SendRotatorInfo();
        }

        public void Dispose() {
            rotatorMediator.RemoveConsumer(this);
        }
    }
}