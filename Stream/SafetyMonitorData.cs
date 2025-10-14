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

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class SafetyMonitorData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;

        public SafetyMonitorData(IInfluxDbExporterOptions options, ISafetyMonitorMediator safetyMonitorMediator) {
            this.options = options;
            this.safetyMonitorMediator = safetyMonitorMediator;

            this.safetyMonitorMediator.IsSafeChanged += OnIsSafeChanged;
        }

        private async void OnIsSafeChanged(object sender, IsSafeEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "safety_safe_state")
                .Field("title", "Safety state changed")
                .Field("text", $"Safe state changed to {e.IsSafe}")
                .Field("safety_issafe", e.IsSafe)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            safetyMonitorMediator.IsSafeChanged -= OnIsSafeChanged;

            GC.SuppressFinalize(this);
        }
    }
}