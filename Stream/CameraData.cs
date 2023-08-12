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
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!CameraInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(CameraInfo.Temperature) ? -1d : CameraInfo.Temperature;
            points.Add(PointData.Measurement("camera_sensor_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(CameraInfo.CoolerPower) ? -1d : CameraInfo.CoolerPower;
            points.Add(PointData.Measurement("camera_cooler_power")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            var valueInt = (CameraInfo.Battery < 0) ? -1 : CameraInfo.Battery;
            points.Add(PointData.Measurement("camera_battery_level")
                .Field("value", valueInt)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
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