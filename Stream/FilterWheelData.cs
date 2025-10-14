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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class FilterWheelData : IDisposable, IFilterWheelConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IFilterWheelMediator filterWheelMediator;

        private readonly TimeSpan updateInterval = TimeSpan.FromSeconds(60);

        public FilterWheelData(IInfluxDbExporterOptions options, IFilterWheelMediator filterWheelMediator) {
            this.options = options;
            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.filterWheelMediator.Connected += OnConnected;
            this.filterWheelMediator.Disconnected += OnDisconnected;

            this.filterWheelMediator.FilterChanged += OnFilterChanged;
        }

        private async void SendFilterWheelInfo() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!FilterWheelInfo.Connected) return;

            if (DateTime.Now - LastUpdate < updateInterval) return;
            if (FilterWheelInfo.IsMoving) return;

            if (await SendPoints()) {
                LastUpdate = DateTime.Now;
            }
        }

        private async Task<bool> SendPoints() {
            var success = false;
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData.Measurement("fwheel_filter")
                .Field("value", FilterWheelInfo.SelectedFilter.Name)
                .Timestamp(timeStamp, WritePrecision.S));

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
                fullOptions.AddDefaultTag("focuser_name", FilterWheelInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
                success = true;
            } catch (Exception ex) {
                Logger.Error($"Failed to write filter wheel points: {ex.Message}");
            }

            return success;
        }

        private FilterWheelInfo FilterWheelInfo { get; set; }
        private DateTime LastUpdate { get; set; } = DateTime.MinValue;

        public void UpdateDeviceInfo(FilterWheelInfo info) {
            FilterWheelInfo = info;
            SendFilterWheelInfo();
        }

        private async Task OnConnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "fwheel_connected")
                .Field("title", "Filter Wheel connected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDisconnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "fwheel_disconnected")
                .Field("title", "Filter Wheel disconnected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnFilterChanged(object sender, FilterChangedEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "filter_change")
                .Field("title", "Filter changed")
                .Field("text", $"Filter changed from {e.From.Name} to {e.To.Name}")
                .Field("filter_from", e.From.Name)
                .Field("filter_to", e.To.Name)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);

            Logger.Info($"Filter changed from {e.From.Name} to {e.To.Name}");
        }

        public void Dispose() {
            filterWheelMediator.Connected -= OnConnected;
            filterWheelMediator.Disconnected -= OnDisconnected;

            filterWheelMediator.FilterChanged -= OnFilterChanged;

            filterWheelMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}