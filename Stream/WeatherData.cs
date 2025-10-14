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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public partial class WeatherData : IDisposable, IWeatherDataConsumer {
        private readonly IInfluxDbExporterOptions options;
        private readonly IWeatherDataMediator weatherDataMediator;

        public WeatherData(IInfluxDbExporterOptions options, IWeatherDataMediator weatherDataMediator) {
            this.options = options;
            this.weatherDataMediator = weatherDataMediator;
            this.weatherDataMediator.RegisterConsumer(this);

            this.weatherDataMediator.Connected += OnConnected;
            this.weatherDataMediator.Disconnected += OnDisconnected;
        }

        private async void SendWeatherData() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;
            if (!WeatherDataInfo.Connected) return;

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();
            double valueDouble;

            valueDouble = double.IsNaN(WeatherDataInfo.CloudCover) ? 0d : WeatherDataInfo.CloudCover;
            points.Add(PointData.Measurement("wx_cloud_cover")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.DewPoint) ? 0d : WeatherDataInfo.DewPoint;
            points.Add(PointData.Measurement("wx_dewpoint")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Humidity) ? 0d : WeatherDataInfo.Humidity;
            points.Add(PointData.Measurement("wx_humidity")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Pressure) ? 0d : WeatherDataInfo.Pressure;
            points.Add(PointData.Measurement("wx_pressure")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.RainRate) ? 0d : WeatherDataInfo.RainRate;
            points.Add(PointData.Measurement("wx_rain_rate")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyBrightness) ? 0d : WeatherDataInfo.SkyBrightness;
            points.Add(PointData.Measurement("wx_sky_brightness")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyQuality) ? 0d : WeatherDataInfo.SkyQuality;
            points.Add(PointData.Measurement("wx_sky_quality")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.SkyTemperature) ? 0d : WeatherDataInfo.SkyTemperature;
            points.Add(PointData.Measurement("wx_sky_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.StarFWHM) ? 0d : WeatherDataInfo.StarFWHM;
            points.Add(PointData.Measurement("wx_star_fwhm")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.Temperature) ? 0d : WeatherDataInfo.Temperature;
            points.Add(PointData.Measurement("wx_temperature")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindDirection) ? 0d : WeatherDataInfo.WindDirection;
            points.Add(PointData.Measurement("wx_wind_direction")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindGust) ? 0d : WeatherDataInfo.WindGust;
            points.Add(PointData.Measurement("wx_wind_gust")
                .Field("value", valueDouble)
                .Timestamp(timeStamp, WritePrecision.Ns));

            valueDouble = double.IsNaN(WeatherDataInfo.WindSpeed) ? 0d : WeatherDataInfo.WindSpeed;
            points.Add(PointData.Measurement("wx_wind_speed")
                .Field("value", valueDouble)
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
                fullOptions.AddDefaultTag("wx_driver_name", WeatherDataInfo.Name);
            }

            using var client = new InfluxDBClient(fullOptions);

            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
            } catch (Exception ex) {
                Logger.Error($"Failed to write weather points: {ex.Message}");
            }
        }

        private WeatherDataInfo WeatherDataInfo { get; set; }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            WeatherDataInfo = deviceInfo;
            SendWeatherData();
        }

        private async Task OnConnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "wx_connected")
                .Field("text", "Weather source connected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        private async Task OnDisconnected(object sender, EventArgs e) {
            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            points.Add(PointData
                .Measurement(options.MeasurementName)
                .Tag("name", "wx_disconnected")
                .Field("text", "Weather source disconnected")
                .Timestamp(timeStamp, WritePrecision.Ms));

            await Utilities.Utilities.SendPoints(options, points);
        }

        public void Dispose() {
            weatherDataMediator.Connected -= OnConnected;
            weatherDataMediator.Disconnected -= OnDisconnected;

            weatherDataMediator.RemoveConsumer(this);
            GC.SuppressFinalize(this);
        }
    }
}