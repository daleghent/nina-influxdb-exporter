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

    public partial class FlatDeviceData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly IFlatDeviceMediator flatDeviceMediator;

        public FlatDeviceData(IInfluxDbExporterOptions options, IFlatDeviceMediator flatDeviceMediator) {
            this.options = options;
            this.flatDeviceMediator = flatDeviceMediator;

            this.flatDeviceMediator.Connected += OnConnected;
            this.flatDeviceMediator.Disconnected += OnDisconnected;

            this.flatDeviceMediator.Opened += OnFlatCoverOpened;
            this.flatDeviceMediator.Closed += OnFlatCoverClosed;
            this.flatDeviceMediator.BrightnessChanged += OnBrightnessChanged;
            this.flatDeviceMediator.LightToggled += OnLightToggled;
        }

        private async Task OnConnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_connected")
                .Field("text", "Cover/Calibrator connected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDisconnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_disconnected")
                .Field("text", "Cover/Calibrator disconnected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnFlatCoverOpened(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_opened")
                .Field("text", "Cover opened")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnFlatCoverClosed(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_closed")
                .Field("text", "Cover closed")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnBrightnessChanged(object sender, FlatDeviceBrightnessChangedEventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_brightness")
                .Field("title", "Calibrator brightness changed")
                .Field("text", $"Calibrator brightness changed to {e.To}")
                .Field("calibrator_brightness_from", e.From)
                .Field("calibrator_brightness_to", e.To)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnLightToggled(object sender, EventArgs e) {
            var state = flatDeviceMediator.GetInfo().LocalizedLightOnState;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "calibrator_light_toggled")
                .Field("title", "Calibrator light toggled")
                .Field("text", $"Calibrator light: {state}")
                .Field("calibrator_light_state", state)
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            this.flatDeviceMediator.Connected -= OnConnected;
            this.flatDeviceMediator.Disconnected -= OnDisconnected;

            this.flatDeviceMediator.Opened -= OnFlatCoverOpened;
            this.flatDeviceMediator.Closed -= OnFlatCoverClosed;
            this.flatDeviceMediator.BrightnessChanged -= OnBrightnessChanged;
            this.flatDeviceMediator.LightToggled -= OnLightToggled;

            GC.SuppressFinalize(this);
        }
    }
}