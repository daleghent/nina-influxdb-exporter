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
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!WeatherDataInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(WeatherDataInfo.CloudCover) ?
                -1d : WeatherDataInfo.CloudCover;
            points.Add(PointData.Measurement("wx_cloud_cover")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.DewPoint) ?
                -1d : WeatherDataInfo.DewPoint;
            points.Add(PointData.Measurement("wx_dewpoint")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Humidity) ?
                -1d : WeatherDataInfo.Humidity;
            points.Add(PointData.Measurement("wx_humidity")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Pressure) ?
                -1d : WeatherDataInfo.Pressure;
            points.Add(PointData.Measurement("wx_pressure")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.RainRate) ?
                -1d : WeatherDataInfo.RainRate;
            points.Add(PointData.Measurement("wx_rain_rate")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyBrightness) ?
                -1d : WeatherDataInfo.SkyBrightness;
            points.Add(PointData.Measurement("wx_sky_brightness")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyQuality) ?
                -1d : WeatherDataInfo.SkyQuality;
            points.Add(PointData.Measurement("wx_sky_quality")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyTemperature) ?
                -1d : WeatherDataInfo.SkyTemperature;
            points.Add(PointData.Measurement("wx_sky_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.StarFWHM) ?
                -1d : WeatherDataInfo.StarFWHM;
            points.Add(PointData.Measurement("wx_star_fwhm")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Temperature) ?
                -1d : WeatherDataInfo.Temperature;
            points.Add(PointData.Measurement("wx_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindDirection) ?
                -1d : WeatherDataInfo.WindDirection;
            points.Add(PointData.Measurement("wx_wind_direction")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindGust) ?
                -1d : WeatherDataInfo.WindGust;
            points.Add(PointData.Measurement("wx_wind_gust")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindSpeed) ?
                -1d : WeatherDataInfo.WindSpeed;
            points.Add(PointData.Measurement("wx_wind_speed")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
            using var writeApi = client.GetWriteApi();
            writeApi.EventHandler += WriteEventHandler.WriteEvent;
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