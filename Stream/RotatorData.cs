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
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class RotatorData : IDisposable, IRotatorConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IRotatorMediator rotatorMediator;

        public RotatorData(IInfluxDbExporterOptions options, IRotatorMediator rotatorMediator) {
            this.options = options;
            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);

            this.rotatorMediator.Moved += OnRotatorMoved;
        }

        private async void SendRotatorInfo() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!RotatorInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            float valueFloat;

            valueFloat = float.IsNaN(RotatorInfo.MechanicalPosition) ? 0f : RotatorInfo.MechanicalPosition;
            points.Add(PointData.Measurement("rotator_mechanical_angle")
                .Field("value", valueFloat)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueFloat = float.IsNaN(RotatorInfo.Position) ? 0f : RotatorInfo.Position;
            points.Add(PointData.Measurement("rotator_angle")
                .Field("value", valueFloat)
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
                fullOptions.AddDefaultTag("rotator_name", RotatorInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write focuser points: {ex.Message}");
            }
        }

        private RotatorInfo RotatorInfo { get; set; }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            RotatorInfo = deviceInfo;
            SendRotatorInfo();
        }

        private async Task OnRotatorMoved(object sender, RotatorEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "rotator_moved")
                .Field("title", "Rotator moved")
                .Field("text", $"Rotator moved. From: {e.From:F2}°; To: {e.To:F2}°")
                .Field("rotator_moved_from", e.From)
                .Field("rotator_moved_to", e.To)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            rotatorMediator.Moved -= OnRotatorMoved;

            rotatorMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}