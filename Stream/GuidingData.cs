#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using DaleGhent.NINA.InfluxDbExporter.Utilities;
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
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            var guiderInfo = guiderMediator.GetInfo();

            if (guiderInfo.Connected) {
                var timeStamp = DateTime.UtcNow;
                var points = new List<PointData>();
                double valueDouble;

                valueDouble = double.IsNaN(guiderInfo.RMSError.Dec.Arcseconds) ?
                    -1d : guiderInfo.RMSError.Dec.Arcseconds;
                points.Add(PointData.Measurement("guiderErrDecArcsec")
                    .Field("value", valueDouble)
                    .Timestamp(timeStamp, WritePrecision.Ns));

                valueDouble = double.IsNaN(guiderInfo.RMSError.Dec.Pixel) ?
                    -1d : guiderInfo.RMSError.Dec.Pixel;
                points.Add(PointData.Measurement("guiderErrDecPixel")
                    .Field("value", valueDouble)
                    .Timestamp(timeStamp, WritePrecision.Ns));

                valueDouble = double.IsNaN(guiderInfo.RMSError.RA.Arcseconds) ?
                    -1d : guiderInfo.RMSError.RA.Arcseconds;
                points.Add(PointData.Measurement("guiderErrRAArcsec")
                    .Field("value", valueDouble)
                    .Timestamp(timeStamp, WritePrecision.Ns));

                valueDouble = double.IsNaN(guiderInfo.RMSError.RA.Pixel) ?
                    -1d : guiderInfo.RMSError.RA.Pixel;
                points.Add(PointData.Measurement("guiderErrRAPixel")
                    .Field("value", valueDouble)
                    .Timestamp(timeStamp, WritePrecision.Ns));

                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
                using var writeApi = client.GetWriteApi();
                writeApi.EventHandler += WriteEventHandler.WriteEvent;
                writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
                writeApi.Flush();
                writeApi.Dispose();
            }
        }

        public void Unregister() {
            guiderMediator.GuideEvent -= GuideData;
        }
    }
}