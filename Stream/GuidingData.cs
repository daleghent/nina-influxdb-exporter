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
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class GuidingData : IGuiderConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IGuiderMediator guiderMediator;

        public GuidingData(IInfluxDbExporterOptions options, IGuiderMediator guiderMediator) {
            this.options = options;
            this.guiderMediator = guiderMediator;
            this.guiderMediator.RegisterConsumer(this);
        }

        private void SendGuideData() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!GuiderInfo.Connected) { return; }

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Dec.Arcseconds) ?
                -1d : GuiderInfo.RMSError.Dec.Arcseconds;
            points.Add(PointData.Measurement("guider_err_dec_arcsec")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.Dec.Pixel) ?
                -1d : GuiderInfo.RMSError.Dec.Pixel;
            points.Add(PointData.Measurement("guider_err_dec_pixel")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.RA.Arcseconds) ?
                -1d : GuiderInfo.RMSError.RA.Arcseconds;
            points.Add(PointData.Measurement("guider_err_ra_arcsec")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(GuiderInfo.RMSError.RA.Pixel) ?
                -1d : GuiderInfo.RMSError.RA.Pixel;
            points.Add(PointData.Measurement("guider_err_ra_pixel")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
            writeApi.Flush();
            writeApi.Dispose();
        }

        private GuiderInfo GuiderInfo { get; set; }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            GuiderInfo = deviceInfo;
            SendGuideData();
        }

        public void Dispose() {
            guiderMediator.RemoveConsumer(this);
        }
    }
}