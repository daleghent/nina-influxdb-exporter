#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class MiscData : IDisposable {
        private readonly IInfluxDbExporterOptions options;
        private readonly IProfileService profileService;

        private PeriodicTimer timer;

        public MiscData(IInfluxDbExporterOptions options, IProfileService profileService) {
            this.options = options;
            this.profileService = profileService;
        }

        public async void Run(CancellationToken ct) {
            try {
                timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

                MiscItems();

                while (await timer.WaitForNextTickAsync(ct)) {
                    MiscItems();
                }
            } catch (OperationCanceledException) {
                // do nothing
            }
        }

        public void Dispose() {
            timer?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async void MiscItems() {
            if (!Utilities.Utilities.ConfigCheck(this.options)) return;

            var observerInfo = new ObserverInfo() {
                Latitude = profileService.ActiveProfile.AstrometrySettings.Latitude,
                Longitude = profileService.ActiveProfile.AstrometrySettings.Longitude,
                Elevation = profileService.ActiveProfile.AstrometrySettings.Elevation
            };

            var timeStamp = DateTime.UtcNow;
            var points = new List<PointData>();

            var sunAltitude = AstroUtil.GetSunAltitude(timeStamp, observerInfo);
            var moonAltitude = AstroUtil.GetMoonAltitude(timeStamp, observerInfo);

            points.Add(PointData.Measurement("astro_sun_altitude")
                .Field("value", sunAltitude)
                .Timestamp(timeStamp, WritePrecision.Ns));

            points.Add(PointData.Measurement("astro_moon_altitude")
                .Field("value", moonAltitude)
                .Timestamp(timeStamp, WritePrecision.Ns));

            await Utilities.Utilities.SendPoints(options, points);
        }
    }
}