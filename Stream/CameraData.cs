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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class CameraData : ICameraConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly ICameraMediator cameraMediator;

        public CameraData(IInfluxDbExporterOptions options, ICameraMediator cameraMediator) {
            this.options = options;
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
        }

        private void SendCameraInfo() {
            if (!CameraInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            if (!double.IsNaN(CameraInfo.Temperature)) {
                points.Add(PointData.Measurement("cameraSensorTemp")
                    .Field("value", CameraInfo.Temperature)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(CameraInfo.CoolerPower)) {
                points.Add(PointData.Measurement("cameraCoolerPower")
                    .Field("value", CameraInfo.CoolerPower)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (CameraInfo.Battery > -1) {
                points.Add(PointData.Measurement("cameraBatteryLevel")
                    .Field("value", CameraInfo.Battery)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
            writeApi.Flush();
            writeApi.Dispose();
        }

        private CameraInfo CameraInfo { get; set; }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
            SendCameraInfo();
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }
    }
}