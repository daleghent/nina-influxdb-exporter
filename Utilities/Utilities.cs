#region "copyright"

/*
    Copyright 2023 Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    public class Utilities {

        public static bool ConfigCheck(IInfluxDbExporterOptions options) {
            if (options == null) { return false; }
            if (!options.AuthWorks) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbUrl)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbBucket)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbToken)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbOrgId)) { return false; }

            return true;
        }

        public static async Task<bool> SendPoints(IInfluxDbExporterOptions options, List<InfluxDB.Client.Writes.PointData> points) {
            if (!ConfigCheck(options)) { return false; }
            if (points == null) { return false; }
            if (points.Count == 0) { return false; }

            // Send the points
            var fullOptions = new InfluxDB.Client.InfluxDBClientOptions(options.InfluxDbUrl) {
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

            using var client = new InfluxDB.Client.InfluxDBClient(fullOptions);
            try {
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
                return true;
            } catch {
                return false;
            }
        }
    }
}