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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class FocuserData : IFocuserConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IFocuserMediator focuserMediator;

        public FocuserData(IInfluxDbExporterOptions options, IFocuserMediator focuserMediator) {
            this.options = options;
            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);
        }

        private void SendFocuserInfo() {
            if (!FocuserInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            if (!double.IsNaN(FocuserInfo.Temperature)) {
                points.Add(PointData.Measurement("focuserTemperature")
                    .Field("value", FocuserInfo.Temperature)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(FocuserInfo.Position)) {
                points.Add(PointData.Measurement("focuserPostition")
                    .Field("value", FocuserInfo.Position)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbUserName, options.InfluxDbUserPassword, options.InfluxDbDbName, string.Empty);
            using var writeApi = client.GetWriteApi();
            writeApi.WritePoints(points);
            writeApi.Flush();
            writeApi.Dispose();
        }

        private FocuserInfo FocuserInfo { get; set; }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            FocuserInfo = deviceInfo;
            SendFocuserInfo();
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            throw new NotImplementedException();
        }

        public void UpdateUserFocused(FocuserInfo info) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            focuserMediator.RemoveConsumer(this);
        }
    }
}