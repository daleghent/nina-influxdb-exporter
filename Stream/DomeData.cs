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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class DomeData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly IDomeMediator domeMediator;

        public DomeData(IInfluxDbExporterOptions options, IDomeMediator domeMediator) {
            this.options = options;
            this.domeMediator = domeMediator;

            this.domeMediator.Opened += OnDomeShutterOpen;
            this.domeMediator.Closed += OnDomeShutterClose;
            this.domeMediator.Homed += OnDomeHomed;
            this.domeMediator.Parked += OnDomeParked;
            this.domeMediator.Slewed += OnDomeSlewed;
        }

        private async Task OnDomeHomed(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "dome_shutter_homed")
                .Field("text", "Dome homed")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDomeParked(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "dome_shutter_parked")
                .Field("text", "Dome parked")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDomeShutterOpen(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "dome_shutter_open")
                .Field("text", "Dome shutter opened")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDomeShutterClose(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "dome_shutter_close")
                .Field("text", "Dome shutter closed")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDomeSlewed(object sender, DomeEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.EventMetric)
                .Tag("name", "dome_slewed")
                .Field("text", $"Dome slewed. From: {e.From:F2}°; To: {e.To:F2}°")
                .Field("dome_slewed_from", e.From)
                .Field("dome_slewed_to", e.To)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            domeMediator.Opened -= OnDomeShutterOpen;
            domeMediator.Closed -= OnDomeShutterClose;
            domeMediator.Homed -= OnDomeHomed;
            domeMediator.Parked -= OnDomeParked;
            domeMediator.Slewed -= OnDomeSlewed;

            GC.SuppressFinalize(this);
        }
    }
}