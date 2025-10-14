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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class FocuserData : IFocuserConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IFocuserMediator focuserMediator;

        public FocuserData(IInfluxDbExporterOptions options, IFocuserMediator focuserMediator) {
            this.options = options;
            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.focuserMediator.Connected += OnConnected;
            this.focuserMediator.Disconnected += OnDisconnected;
        }

        private async void SendFocuserInfo() {
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
                fullOptions.AddDefaultTag("focuser_name", FocuserInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write focuser points: {ex.Message}");
            }
        }

        private FocuserInfo FocuserInfo { get; set; }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            FocuserInfo = deviceInfo;
            SendFocuserInfo();
        }

        public async void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            double afpos = double.IsNaN(info.Position) ? 0d : info.Position;
            double aftemp = double.IsNaN(info.Temperature) ? 0d : info.Temperature;

            points.Add(PointData.Measurement(options.MeasurementName)
                .Tag("name", "autofocus")
                .Field("title", "Autofocus completed")
                .Field("text", $"Autofocus on filter {info.Filter}, Postion: {afpos}, Temperature: {aftemp}")
                .Field("autofocus_position", Convert.ToInt32(afpos))
                .Field("autofocus_temperature", aftemp)
                .Field("autofocus_filter", info.Filter)
                .Timestamp(timeStamp, WritePrecision.Ms));

            // Send the points
            var fullOptions = new InfluxDBClientOptions(options.InfluxDbUrl) {
                Token = options.InfluxDbToken,
                Bucket = options.InfluxDbBucket,
                Org = options.InfluxDbOrgId,
            };

            if (string.IsNullOrEmpty(info.Filter)) {
                info.Filter = "Unknown";
            }
            fullOptions.AddDefaultTag("afop_filter", info.Filter);

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

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write focuser points: {ex.Message}");
            }
        }

        public void UpdateUserFocused(FocuserInfo info) {
        }

        private async Task OnConnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "focuser_connected")
                .Field("text", "Focuser connected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDisconnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "focuser_disconnected")
                .Field("text", "Focuser disconnected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            focuserMediator.Connected -= OnConnected;
            focuserMediator.Disconnected -= OnDisconnected;

            focuserMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}