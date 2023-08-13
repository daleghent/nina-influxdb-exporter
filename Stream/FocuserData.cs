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
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!FocuserInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            double valueDouble = double.IsNaN(FocuserInfo.Temperature) ? 0d : FocuserInfo.Temperature;
            points.Add(PointData.Measurement("focuser_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            var valueInt = (FocuserInfo.Position < 0) ? 0 : FocuserInfo.Position;
            points.Add(PointData.Measurement("focuser_position")
                .Field("value", valueInt)
                .Timestamp(timeStamp, WritePrecision.Ns));

            // Send the points
            var fullOptions = new InfluxDBClientOptions(options.InfluxDbUrl) {
                Token = options.InfluxDbToken,
            };

            if (options.TagProfileName) {
                fullOptions.AddDefaultTag("profile_name", options.ProfileName);
            }

            if (options.TagHostname) {
                fullOptions.AddDefaultTag("host_name", options.Hostname);
            }

            if (options.TagEquipmentName) {
                fullOptions.AddDefaultTag("focuser_name", FocuserInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
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