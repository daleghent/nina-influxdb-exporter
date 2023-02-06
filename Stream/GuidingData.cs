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
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

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
                var timeStamp = DateTime.UtcNow;
                var points = new List<PointData>();

                if (!double.IsNaN(guiderInfo.RMSError.Dec.Arcseconds)) {
                    points.Add(PointData.Measurement("guiderErrDecArcsec")
                        .Field("value", guiderInfo.RMSError.Dec.Arcseconds)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }

                if (!double.IsNaN(guiderInfo.RMSError.Dec.Pixel)) {
                    points.Add(PointData.Measurement("guiderErrDecPixel")
                        .Field("value", guiderInfo.RMSError.Dec.Pixel)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }

                if (!double.IsNaN(guiderInfo.RMSError.RA.Arcseconds)) {
                    points.Add(PointData.Measurement("guiderErrRAArcsec")
                        .Field("value", guiderInfo.RMSError.RA.Arcseconds)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }

                if (!double.IsNaN(guiderInfo.RMSError.RA.Pixel)) {
                    points.Add(PointData.Measurement("guiderErrRAPixel")
                        .Field("value", guiderInfo.RMSError.RA.Pixel)
                        .Timestamp(timeStamp, WritePrecision.Ns));
                }

                if (points.Count > 0) {
                    using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
                    using var writeApi = client.GetWriteApi();
                    writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
                    writeApi.Flush();
                    writeApi.Dispose();
                }
            }
        }

        public void Unregister() {
            guiderMediator.GuideEvent -= GuideData;
        }
    }
}