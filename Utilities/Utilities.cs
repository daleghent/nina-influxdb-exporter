#region "copyright"

/*
    Copyright 2023 Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Enums;
using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using InfluxDB.Client.Writes;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    public class Utilities {

        public static bool ConfigCheck(IInfluxDbExporterOptions options) {
            if (options == null) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbUrl)) { return false; }

            switch (options.InfluxDbVersion) {
                case InfluxDbVersion.V1:
                    if (string.IsNullOrEmpty(options.InfluxDbDatabase)) { return false; }
                    break;

                case InfluxDbVersion.V2:
                    if (!options.AuthWorks) { return false; }
                    if (string.IsNullOrEmpty(options.InfluxDbBucket)) { return false; }
                    if (string.IsNullOrEmpty(options.InfluxDbToken)) { return false; }
                    if (string.IsNullOrEmpty(options.InfluxDbOrgId)) { return false; }
                    break;

                case InfluxDbVersion.V3:
                    if (string.IsNullOrEmpty(options.InfluxDbDatabase)) { return false; }
                    break;
            }

            return true;
        }

        internal static long UnixEpoch(DateTime dateTime) {
            return (long)dateTime.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        public static async Task<bool> SendPoints(IInfluxDbExporterOptions options, List<PointData> points) {
            if (!ConfigCheck(options)) { return false; }
            if (points == null) { return false; }
            if (points.Count == 0) { return false; }

            return options.InfluxDbVersion switch {
                InfluxDbVersion.V1 => await SendPointsV1(options, points),
                InfluxDbVersion.V2 => await SendPointsV2(options, points),
                InfluxDbVersion.V3 => await SendPointsV3(options, points),
                _ => false,
            };
        }

        private static async Task<bool> SendPointsV1(IInfluxDbExporterOptions options, List<PointData> points) {
            try {
                var lines = new StringBuilder();
                foreach (var point in points) {
                    var lp = point.ToLineProtocol();
                    if (!string.IsNullOrEmpty(lp)) {
                        lines.AppendLine(lp);
                    }
                }

                var writeUrl = $"{options.InfluxDbUrl.TrimEnd('/')}/write?db={Uri.EscapeDataString(options.InfluxDbDatabase)}&precision=ns";

                using var httpClient = new HttpClient();
                var content = new StringContent(lines.ToString(), Encoding.UTF8, "text/plain");
                var response = await httpClient.PostAsync(writeUrl, content);

                if (!response.IsSuccessStatusCode) {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
                }

                return true;
            } catch (Exception ex) {
                Logger.Error($"Failed to send points to InfluxDB 1.x: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> SendPointsV2(IInfluxDbExporterOptions options, List<PointData> points) {
            try {
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
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointsAsync(points);
                return true;
            } catch (Exception ex) {
                Logger.Error($"Failed to send points to InfluxDB 2.x: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> SendPointsV3(IInfluxDbExporterOptions options, List<PointData> points) {
            try {
                var clientOptions = new InfluxDB3.Client.Config.ClientConfig {
                    Host = options.InfluxDbUrl,
                    Database = options.InfluxDbDatabase,
                    Token = string.IsNullOrWhiteSpace(options.InfluxDbToken) ? null : options.InfluxDbToken,
                };

                using var client = new InfluxDB3.Client.InfluxDBClient(clientOptions);

                // Build default tags to prepend to line protocol if tagging options are set.
                // InfluxDB3.Client accepts line-protocol strings; we serialise via the v2 SDK's ToLineProtocol()
                // and optionally inject default tags as additional tag set entries.
                var defaultTags = new List<(string key, string value)>();
                if (options.TagProfileName && !string.IsNullOrEmpty(options.ProfileName)) {
                    defaultTags.Add(("profile_name", options.ProfileName));
                }
                if (options.TagHostname && !string.IsNullOrEmpty(options.Hostname)) {
                    defaultTags.Add(("host_name", options.Hostname));
                }

                var records = points
                    .Select(p => {
                        var lp = p.ToLineProtocol();
                        if (string.IsNullOrEmpty(lp) || defaultTags.Count == 0) {
                            return lp;
                        }
                        // Insert default tags after the measurement name
                        // Line protocol: <measurement>[,<tag>=<val>...] <fields> [timestamp]
                        var commaOrSpace = lp.IndexOf(' ');
                        var firstComma = lp.IndexOf(',');
                        int insertAt = (firstComma >= 0 && firstComma < commaOrSpace) ? firstComma : commaOrSpace;
                        if (insertAt < 0) { return lp; }
                        var tagStr = string.Join(",", defaultTags.Select(t => $"{EscapeTag(t.key)}={EscapeTag(t.value)}"));
                        return lp.Insert(insertAt, "," + tagStr);
                    })
                    .Where(lp => !string.IsNullOrEmpty(lp))
                    .ToArray();

                await client.WriteRecordsAsync(records);
                return true;
            } catch (Exception ex) {
                Logger.Error($"Failed to send points to InfluxDB 3.x: {ex.Message}");
                return false;
            }
        }

        private static string EscapeTag(string value) {
            return value.Replace(",", @"\,").Replace("=", @"\=").Replace(" ", @"\ ");
        }
    }
}