#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class SafetyMonitorData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;

        private const string safePeriodTag = "safety_safe_period";
        private const string unsafePeriodTag = "safety_unsafe_period";

        public SafetyMonitorData(IInfluxDbExporterOptions options, ISafetyMonitorMediator safetyMonitorMediator) {
            this.options = options;
            this.safetyMonitorMediator = safetyMonitorMediator;

            this.safetyMonitorMediator.Connected += OnConnected;
            this.safetyMonitorMediator.Disconnected += OnDisconnected;

            this.safetyMonitorMediator.IsSafeChanged += OnIsSafeChanged;
        }

        private bool IsSafe { get; set; } = false;

        private DateTimeOffset BeganSafe { get; set; } = DateTimeOffset.MinValue;

        private DateTimeOffset BeganUnsafe { get; set; } = DateTimeOffset.MinValue;

        private async void OnIsSafeChanged(object sender, IsSafeEventArgs e) {
            var timeStamp = DateTimeOffset.UtcNow;
            var points = new List<PointData>();

            IsSafe = e.IsSafe;
            var safeStateText = e.IsSafe ? "SAFE" : "UNSAFE";

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "safety_safe_state")
                .Field("title", "Safety state changed")
                .Field("text", $"Safety state changed to {safeStateText}")
                .Field("safety_issafe", e.IsSafe)
                .Timestamp(timeStamp, WritePrecision.Ms));

            // Just transitioned to SAFE
            if (IsSafe) {
                // record the beginning of this new SAFE period
                BeganSafe = timeStamp;

                // event: the beignning and end of the preceeding UNSAFE period
                points.Add(PointData
                    .Measurement(options.MeasurementName)
                    .Tag("name", unsafePeriodTag)
                    .Field("text", "Unafe period")
                    .Field("timeEnd", timeStamp.ToUnixTimeMilliseconds())
                    .Timestamp(BeganUnsafe, WritePrecision.Ms));
            }

            // Just transitioned to UNSAFE
            if (!IsSafe) {
                // record the beginning of this new UNSAFE period
                BeganUnsafe = timeStamp;

                // event: the beignning and end of the preceeding SAFE period
                points.Add(PointData
                    .Measurement(options.MeasurementName)
                    .Tag("name", safePeriodTag)
                    .Field("text", "Safe period")
                    .Field("timeEnd", timeStamp.ToUnixTimeMilliseconds())
                    .Timestamp(BeganSafe, WritePrecision.Ms));
            }

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnConnected(object sender, EventArgs e) {
            var timeStamp = DateTimeOffset.UtcNow;
            var points = new List<PointData>();

            IsSafe = safetyMonitorMediator.GetInfo().IsSafe;

            if (IsSafe) {
                BeganSafe = timeStamp;
            } else {
                BeganUnsafe = timeStamp;
            }

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "safety_connected")
                .Field("text", "Safety Monitor connected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDisconnected(object sender, EventArgs e) {
            var timeStamp = DateTimeOffset.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "safety_disconnected")
                .Field("text", "Safety Monitor disconnected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            if (IsSafe) {
                // event: the beignning and end of the existing SAFE period
                points.Add(PointData
                    .Measurement(options.MeasurementName)
                    .Tag("name", "safety_safe_period")
                    .Field("text", "Safe period")
                    .Field("timeEnd", timeStamp.ToUnixTimeMilliseconds())
                    .Timestamp(BeganSafe, WritePrecision.Ms));
            }

            if (!IsSafe) {
                // event: the beignning and end of the existing UNSAFE period
                points.Add(PointData
                    .Measurement(options.MeasurementName)
                    .Tag("name", "safety_unsafe_period")
                    .Field("text", "Unafe period")
                    .Field("timeEnd", timeStamp.ToUnixTimeMilliseconds())
                    .Timestamp(BeganUnsafe, WritePrecision.Ms));
            }

            await Utilities.Utilities.SendPoints(options, points);

            BeganSafe = DateTimeOffset.MinValue;
            BeganUnsafe = DateTimeOffset.MinValue;
            IsSafe = false;
        }

        public void Dispose() {
            safetyMonitorMediator.Connected -= OnConnected;
            safetyMonitorMediator.Disconnected -= OnDisconnected;

            safetyMonitorMediator.IsSafeChanged -= OnIsSafeChanged;

            GC.SuppressFinalize(this);
        }
    }
}