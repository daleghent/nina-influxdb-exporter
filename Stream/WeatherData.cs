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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Printing;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class WeatherData : IWeatherDataConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IWeatherDataMediator weatherDataMediator;

        public WeatherData(IInfluxDbExporterOptions options, IWeatherDataMediator weatherDataMediator) {
            this.options = options;
            this.weatherDataMediator = weatherDataMediator;
            this.weatherDataMediator.RegisterConsumer(this);
        }

        private void SendWeatherData() {
            if (!WeatherDataInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            if (!double.IsNaN(WeatherDataInfo.CloudCover)) {
                points.Add(PointData.Measurement("wxCloudCover")
                    .Field("value", WeatherDataInfo.CloudCover)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.DewPoint)) {
                points.Add(PointData.Measurement("wxDewPoint")
                    .Field("value", WeatherDataInfo.DewPoint)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.Humidity)) {
                points.Add(PointData.Measurement("wxHumidity")
                    .Field("value", WeatherDataInfo.Humidity)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.Pressure)) {
                points.Add(PointData.Measurement("wxPressure")
                    .Field("value", WeatherDataInfo.Pressure)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.RainRate)) {
                points.Add(PointData.Measurement("wxRainRate")
                    .Field("value", WeatherDataInfo.RainRate)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.SkyBrightness)) {
                points.Add(PointData.Measurement("wxSkyBrightness")
                    .Field("value", WeatherDataInfo.SkyBrightness)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.SkyQuality)) {
                points.Add(PointData.Measurement("wxSkyQuality")
                    .Field("value", WeatherDataInfo.SkyQuality)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.SkyTemperature)) {
                points.Add(PointData.Measurement("wxSkyTemperature")
                    .Field("value", WeatherDataInfo.SkyTemperature)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.StarFWHM)) {
                points.Add(PointData.Measurement("wxStarFWHM")
                    .Field("value", WeatherDataInfo.StarFWHM)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.Temperature)) {
                points.Add(PointData.Measurement("wxTemperature")
                    .Field("value", WeatherDataInfo.Temperature)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.WindDirection)) {
                points.Add(PointData.Measurement("wxWindDirection")
                    .Field("value", WeatherDataInfo.WindDirection)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.WindGust)) {
                points.Add(PointData.Measurement("wxWindGust")
                    .Field("value", WeatherDataInfo.WindGust)
                    .Timestamp(timeStamp, WritePrecision.Ns));
            }

            if (!double.IsNaN(WeatherDataInfo.WindSpeed)) {
                points.Add(PointData.Measurement("wxWindSpeed")
                    .Field("value", WeatherDataInfo.WindSpeed)
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

        private WeatherDataInfo WeatherDataInfo { get; set; }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            WeatherDataInfo = deviceInfo;
            SendWeatherData();
        }

        public void Dispose() {
            weatherDataMediator.RemoveConsumer(this);
        }
    }
}