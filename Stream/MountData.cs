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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class MountData : IDisposable, ITelescopeConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly ITelescopeMediator telescopeMediator;

        public MountData(IInfluxDbExporterOptions options, ITelescopeMediator telescopeMediator) {
            this.options = options;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            this.telescopeMediator.Parked += OnMountParked;
            this.telescopeMediator.Unparked += OnMountUnparked;
            this.telescopeMediator.Homed += OnMountHomed;
            this.telescopeMediator.Slewed += OnMountSlewed;
        }

        public async void SendMountInfo() {
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
                fullOptions.AddDefaultTag("mount_name", TelescopeInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write mount points: {ex.Message}");
            }
        }

        private TelescopeInfo TelescopeInfo { get; set; }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
            SendMountInfo();
        }

        private async Task OnMountParked(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "mount_parked")
                .Field("text", $"Mount has parked")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnMountUnparked(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "mount_unparked")
                .Field("text", $"Mount has unparked")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnMountHomed(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "mount_homed")
                .Field("text", $"Mount has homed")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnMountSlewed(object sender, MountSlewedEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "mount_slewed")
                .Field("text", $"Mount slewed. From RA: {e.From.RAString}, Dec: {e.From.DecString}; To RA: {e.To.RAString}, Dec: {e.To.DecString}")
                .Field("mount_slew_from_ra", e.From.RAString)
                .Field("mount_slew_from_dec", e.From.DecString)
                .Field("mount_slew_to_ra", e.From.RAString)
                .Field("mount_slew_to_dec", e.From.DecString)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            telescopeMediator.Parked -= OnMountParked;
            telescopeMediator.Unparked -= OnMountUnparked;
            telescopeMediator.Homed -= OnMountHomed;
            telescopeMediator.Slewed -= OnMountSlewed;

            telescopeMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}