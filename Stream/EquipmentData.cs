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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class EquipmentData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IWeatherDataMediator weatherDataMediator;
        private readonly System.Timers.Timer timer;

        public EquipmentData(IProfileService profileService,
                             IInfluxDbExporterOptions options,
                             ICameraMediator cameraMediator,
                             IFocuserMediator focuserMediator,
                             IGuiderMediator guiderMediator,
                             ITelescopeMediator telescopeMediator,
                             IWeatherDataMediator weatherDataMediator) {
            this.profileService = profileService;
            this.options = options;
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.guiderMediator = guiderMediator;
            this.telescopeMediator = telescopeMediator;
            this.weatherDataMediator = weatherDataMediator;

            timer = new System.Timers.Timer(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval));
            timer.Elapsed += new System.Timers.ElapsedEventHandler(DeviceStats);
            timer.AutoReset = true;
            timer.Start();
        }

        private void DeviceStats(object source, System.Timers.ElapsedEventArgs e) {
            using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbUserName, options.InfluxDbUserPassword, options.InfluxDbDbName, string.Empty);
            using var writeApi = client.GetWriteApi();

            var timeNow = DateTime.UtcNow;

            // CameraInfo
            var cameraInfo = cameraMediator.GetInfo();

            if (cameraInfo.Connected) {
                if (!double.IsNaN(cameraInfo.Temperature)) {
                    var cameraSensorTemerature = new CamerSensorTemperature { Value = cameraInfo.Temperature, Time = timeNow };
                    writeApi.WriteMeasurement(cameraSensorTemerature, WritePrecision.Ns);
                }

                if (!double.IsNaN(cameraInfo.CoolerPower)) {
                    var cameraCoolerPower = new CamerCoolerPower { Value = cameraInfo.CoolerPower, Time = timeNow };
                    writeApi.WriteMeasurement(cameraCoolerPower, WritePrecision.Ns);
                }

                if (cameraInfo.Battery > -1) {
                    var cameraBatteryLevel = new CamerBatteryLevel { Value = cameraInfo.Battery, Time = timeNow };
                    writeApi.WriteMeasurement(cameraBatteryLevel, WritePrecision.Ns);
                }
            }

            // Focuser
            var focuserInfo = focuserMediator.GetInfo();

            if (focuserInfo.Connected) {
                if (!double.IsNaN(focuserInfo.Temperature)) {
                    var focuserTemperature = new FocuserTemperature { Value = focuserInfo.Temperature, Time = timeNow };
                    writeApi.WriteMeasurement(focuserTemperature, WritePrecision.Ns);
                }

                if (focuserInfo.Position > -1) {
                    var focuserPosition = new FocuserPosition { Value = focuserInfo.Position, Time = timeNow };
                    writeApi.WriteMeasurement(focuserPosition, WritePrecision.Ns);
                }
            }

            // Guider
            var guiderInfo = guiderMediator.GetInfo();

            if (guiderInfo.Connected) {
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

            // Mount
            var mountInfo = telescopeMediator.GetInfo();

            if (mountInfo.Connected) {
                if (!double.IsNaN(mountInfo.Altitude)) {
                    var mountAltitude = new MountAltitude { Value = mountInfo.Altitude, Time = timeNow };
                    writeApi.WriteMeasurement(mountAltitude, WritePrecision.Ns);
                }

                if (!double.IsNaN(mountInfo.Azimuth)) {
                    var mountAzimuth = new MountAzimuth { Value = mountInfo.Azimuth, Time = timeNow };
                    writeApi.WriteMeasurement(mountAzimuth, WritePrecision.Ns);
                }
            }

            // Weather
            var weatherInfo = weatherDataMediator.GetInfo();

            if (weatherInfo.Connected) {
                if (!double.IsNaN(weatherInfo.CloudCover)) {
                    var weatherCloudCover = new WeatherCloudCover { Value = weatherInfo.CloudCover, Time = timeNow };
                    writeApi.WriteMeasurement(weatherCloudCover, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.DewPoint)) {
                    var weatherDewPoint = new WeatherDewPoint { Value = weatherInfo.DewPoint, Time = timeNow };
                    writeApi.WriteMeasurement(weatherDewPoint, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.Humidity)) {
                    var weatherHumidity = new WeatherHumidity { Value = weatherInfo.Humidity, Time = timeNow };
                    writeApi.WriteMeasurement(weatherHumidity, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.Pressure)) {
                    var weatherPressure = new WeatherPressure { Value = weatherInfo.Humidity, Time = timeNow };
                    writeApi.WriteMeasurement(weatherPressure, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.RainRate)) {
                    var weatherRainRate = new WeatherRainRate { Value = weatherInfo.RainRate, Time = timeNow };
                    writeApi.WriteMeasurement(weatherRainRate, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.SkyBrightness)) {
                    var weatherSkyBrightness = new WeatherSkyBrightness { Value = weatherInfo.SkyBrightness, Time = timeNow };
                    writeApi.WriteMeasurement(weatherSkyBrightness, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.SkyQuality)) {
                    var weatherSkyQuality = new WeatherSkyQuality { Value = weatherInfo.SkyQuality, Time = timeNow };
                    writeApi.WriteMeasurement(weatherSkyQuality, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.SkyTemperature)) {
                    var weatherSkyTemperature = new WeatherSkyTemperature { Value = weatherInfo.SkyTemperature, Time = timeNow };
                    writeApi.WriteMeasurement(weatherSkyTemperature, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.StarFWHM)) {
                    var weatherStarFWHM = new WeatherStarFWHM { Value = weatherInfo.StarFWHM, Time = timeNow };
                    writeApi.WriteMeasurement(weatherStarFWHM, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.Temperature)) {
                    var weatherTemperature = new WeatherTemperature { Value = weatherInfo.Temperature, Time = timeNow };
                    writeApi.WriteMeasurement(weatherTemperature, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.WindDirection)) {
                    var weatherWindDirection = new WeatherWindDirection { Value = weatherInfo.WindDirection, Time = timeNow };
                    writeApi.WriteMeasurement(weatherWindDirection, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.WindDirection)) {
                    var weatherWindDirection = new WeatherWindDirection { Value = weatherInfo.WindDirection, Time = timeNow };
                    writeApi.WriteMeasurement(weatherWindDirection, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.WindGust)) {
                    var weatherWindGust = new WeatherWindGust { Value = weatherInfo.WindGust, Time = timeNow };
                    writeApi.WriteMeasurement(weatherWindGust, WritePrecision.Ns);
                }

                if (!double.IsNaN(weatherInfo.WindSpeed)) {
                    var weatherWindSpeed = new WeatherWindSpeed { Value = weatherInfo.WindSpeed, Time = timeNow };
                    writeApi.WriteMeasurement(weatherWindSpeed, WritePrecision.Ns);
                }
            }
        }

        [Measurement("cameraSensorTemp")]
        private class CamerSensorTemperature {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("cameraCoolerPower")]
        private class CamerCoolerPower {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("cameraBatteryLevel")]
        private class CamerBatteryLevel {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("focuserTemperature")]
        private class FocuserTemperature {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("focuserPostition")]
        private class FocuserPosition {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
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

        [Measurement("mountAltitude")]
        private class MountAltitude {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("mountAzimuth")]
        private class MountAzimuth {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxCloudCover")]
        private class WeatherCloudCover {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxDewPoint")]
        private class WeatherDewPoint {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxHumidity")]
        private class WeatherHumidity {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxPressure")]
        private class WeatherPressure {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxRainRate")]
        private class WeatherRainRate {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxSkyBrightness")]
        private class WeatherSkyBrightness {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxSkyQuality")]
        private class WeatherSkyQuality {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxSkyTemperature")]
        private class WeatherSkyTemperature {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxStarFWHM")]
        private class WeatherStarFWHM {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxTemperature")]
        private class WeatherTemperature {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxWindDirection")]
        private class WeatherWindDirection {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxWindGust")]
        private class WeatherWindGust {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("wxWindSpeed")]
        private class WeatherWindSpeed {
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        public void Dispose() {
            timer.Stop();
            timer.Dispose();
        }
    }
}