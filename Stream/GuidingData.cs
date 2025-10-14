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
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class GuidingData : IDisposable, IGuiderConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IGuiderMediator guiderMediator;

        public GuidingData(IInfluxDbExporterOptions options, IGuiderMediator guiderMediator) {
            this.options = options;
            this.guiderMediator = guiderMediator;

            this.guiderMediator.RegisterConsumer(this);

            this.guiderMediator.GuideEvent += OnGuideEvent;
            this.guiderMediator.GuidingStarted += OnGuidingStarted;
            this.guiderMediator.GuidingStopped += OnGuidingStopped;
            this.guiderMediator.AfterDither += OnAfterDither;
        }

        private async void SendGuideData(IGuideStep guideStep) {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!GuiderInfo.Connected) { return; }

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Dec.Arcseconds) ? 0d : GuiderInfo.RMSError.Dec.Arcseconds;
            points.Add(PointData.Measurement("guider_rms_dec_arcsec")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Dec.Pixel) ? 0d : GuiderInfo.RMSError.Dec.Pixel;
            points.Add(PointData.Measurement("guider_rms_dec_pixel")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.RA.Arcseconds) ? 0d : GuiderInfo.RMSError.RA.Arcseconds;
            points.Add(PointData.Measurement("guider_rms_ra_arcsec")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.RA.Pixel) ? 0d : GuiderInfo.RMSError.RA.Pixel;
            points.Add(PointData.Measurement("guider_rms_ra_pixel")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Total.Arcseconds) ? 0d : GuiderInfo.RMSError.Total.Arcseconds;
            points.Add(PointData.Measurement("guider_rms_arcsec")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Total.Pixel) ? 0d : GuiderInfo.RMSError.Total.Pixel;
            points.Add(PointData.Measurement("guider_rms_pixel")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            double rmsPeakRA = 0d;
            double rmsPeakDec = 0d;
            double rmsPeakTotal = 0d;

            if (!double.IsNaN(GuiderInfo.RMSError.PeakRA.Arcseconds) && !double.IsNaN(GuiderInfo.RMSError.PeakDec.Arcseconds)) {
                rmsPeakRA = GuiderInfo.RMSError.PeakRA.Arcseconds;
                rmsPeakDec = GuiderInfo.RMSError.PeakDec.Arcseconds;
                rmsPeakTotal = Math.Sqrt(Math.Pow(rmsPeakRA, 2) + Math.Pow(rmsPeakDec, 2));
            }

            points.Add(PointData.Measurement("guider_rms_peak_ra_arcsec")
                .Field("value", rmsPeakRA)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_rms_peak_dec_arcsec")
                .Field("value", rmsPeakDec)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_rms_peak_arcsec")
                .Field("value", rmsPeakTotal)
                .Timestamp(timeStamp, WritePrecision.Ns));

            rmsPeakRA = 0d;
            rmsPeakDec = 0d;
            rmsPeakTotal = 0d;

            if (!double.IsNaN(GuiderInfo.RMSError.PeakRA.Pixel) && !double.IsNaN(GuiderInfo.RMSError.PeakDec.Pixel)) {
                rmsPeakRA = GuiderInfo.RMSError.PeakRA.Pixel;
                rmsPeakDec = GuiderInfo.RMSError.PeakDec.Pixel;
                rmsPeakTotal = Math.Sqrt(Math.Pow(rmsPeakRA, 2) + Math.Pow(rmsPeakDec, 2));
            }

            points.Add(PointData.Measurement("guider_rms_peak_ra_pixel")
                .Field("value", rmsPeakRA)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_rms_peak_dec_pixel")
                .Field("value", rmsPeakDec)
            .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_rms_peak_pixel")
                .Field("value", rmsPeakTotal)
                .Timestamp(timeStamp, WritePrecision.Ns));

            // Guide step details
            points.Add(PointData.Measurement("guider_ra_distance")
                .Field("value", guideStep.RADistanceRaw)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_ra_duration")
                .Field("value", guideStep.RADuration)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_dec_distance")
                .Field("value", guideStep.RADuration)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("guider_dec_duration")
                .Field("value", guideStep.DECDuration)
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
                fullOptions.AddDefaultTag("guider_name", GuiderInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write guider points: {ex.Message}");
            }
        }

        public void OnGuideEvent(object sender, IGuideStep guideStep) {
            SendGuideData(guideStep);
        }

        private async Task OnAfterDither(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "guider_dither")
                .Field("title", "Dither")
                .Field("text", "Dither")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnGuidingStarted(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "guider_guiding_started")
                .Field("title", "Guiding started")
                .Field("text", "Guiding started")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnGuidingStopped(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "guider_guiding_stopped")
                .Field("title", "Guiding stopped")
                .Field("text", "Guiding stopped")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private GuiderInfo GuiderInfo { get; set; }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            GuiderInfo = deviceInfo;
        }

        public void Dispose() {
            guiderMediator.GuideEvent -= OnGuideEvent;
            guiderMediator.GuidingStarted -= OnGuidingStarted;
            guiderMediator.GuidingStopped -= OnGuidingStopped;
            guiderMediator.AfterDither -= OnAfterDither;

            guiderMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}