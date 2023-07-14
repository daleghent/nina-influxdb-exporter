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
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;

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
            double valueDouble;

            valueDouble = double.IsNaN(WeatherDataInfo.CloudCover) ?
                -1d : WeatherDataInfo.CloudCover;
            points.Add(PointData.Measurement("wxCloudCover")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.DewPoint) ?
                -1d : WeatherDataInfo.DewPoint;
            points.Add(PointData.Measurement("wxDewPoint")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Humidity) ?
                -1d : WeatherDataInfo.Humidity;
            points.Add(PointData.Measurement("wxHumidity")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Pressure) ?
                -1d : WeatherDataInfo.Pressure;
            points.Add(PointData.Measurement("wxPressure")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.RainRate) ?
                -1d : WeatherDataInfo.RainRate;
            points.Add(PointData.Measurement("wxRainRate")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyBrightness) ?
                -1d : WeatherDataInfo.SkyBrightness;
            points.Add(PointData.Measurement("wxSkyBrightness")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyQuality) ?
                -1d : WeatherDataInfo.SkyQuality;
            points.Add(PointData.Measurement("wxSkyQuality")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyTemperature) ?
                -1d : WeatherDataInfo.SkyTemperature;
            points.Add(PointData.Measurement("wxSkyTemperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.StarFWHM) ?
                -1d : WeatherDataInfo.StarFWHM;
            points.Add(PointData.Measurement("wxStarFWHM")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Temperature) ?
                -1d : WeatherDataInfo.Temperature;
            points.Add(PointData.Measurement("wxTemperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindDirection) ?
                -1d : WeatherDataInfo.WindDirection;
            points.Add(PointData.Measurement("wxWindDirection")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindGust) ?
                -1d : WeatherDataInfo.WindGust;
            points.Add(PointData.Measurement("wxWindGust")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindSpeed) ?
                -1d : WeatherDataInfo.WindSpeed;
            points.Add(PointData.Measurement("wxWindSpeed")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
            writeApi.Flush();
            writeApi.Dispose();
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