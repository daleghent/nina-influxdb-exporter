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
using InfluxDB.Client.Core;
using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class GuidingData {
        private readonly IInfluxDbExporterOptions options;
        private readonly IGuiderMediator guiderMediator;

        public GuidingData(IInfluxDbExporterOptions options, IGuiderMediator guiderMediator) {
            this.options = options;
            this.guiderMediator = guiderMediator;

            this.guiderMediator.GuideEvent += GuideData;
        }

        private void GuideData(object sender, IGuideEvent args) {
            var guiderInfo = guiderMediator.GetInfo();

            if (guiderInfo.Connected) {
                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbUserName, options.InfluxDbUserPassword, options.InfluxDbDbName, string.Empty);
                using var writeApi = client.GetWriteApi();

                var timeNow = DateTime.UtcNow;

                if (!double.IsNaN(guiderInfo.RMSError.Dec.Arcseconds)) {
                    var guiderErrDecArcs = new GuiderErrDecArcsec { Value = guiderInfo.RMSError.Dec.Arcseconds, Time = timeNow };
                    writeApi.WriteMeasurement(guiderErrDecArcs, WritePrecision.Ns);
                }

                if (!double.IsNaN(guiderInfo.RMSError.Dec.Pixel)) {
                    var guiderErrDecPixels = new GuiderErrDecPixels { Value = guiderInfo.RMSError.Dec.Pixel, Time = timeNow };
                    writeApi.WriteMeasurement(guiderErrDecPixels, WritePrecision.Ns);
                }

                if (!double.IsNaN(guiderInfo.RMSError.RA.Arcseconds)) {
                    var guiderErrRAArcs = new GuiderErrRAArcsec { Value = guiderInfo.RMSError.RA.Arcseconds, Time = timeNow };
                    writeApi.WriteMeasurement(guiderErrRAArcs, WritePrecision.Ns);
                }

                if (!double.IsNaN(guiderInfo.RMSError.RA.Pixel)) {
                    var guiderErrRAPixels = new GuiderErrRAPixels { Value = guiderInfo.RMSError.RA.Pixel, Time = timeNow };
                    writeApi.WriteMeasurement(guiderErrRAPixels, WritePrecision.Ns);
                }
            }
        }

        [Measurement("guiderErrDecArcsec")]
        private class GuiderErrDecArcsec {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("guiderErrDecPixels")]
        private class GuiderErrDecPixels {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("guiderErrRAArcsec")]
        private class GuiderErrRAArcsec {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("guiderErrRAPixels")]
        private class GuiderErrRAPixels {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        public void Unregister() {
            guiderMediator.GuideEvent -= GuideData;
        }
    }
}