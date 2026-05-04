#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Enums;
using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using DaleGhent.NINA.InfluxDbExporter.Utilities;
using InfluxDB.Client;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter {

    public partial class InfluxDbExporterOptions : BaseINPC, IInfluxDbExporterOptions {
        private readonly IProfileService profileService;
        private readonly PluginOptionsAccessor pluginOptionsAccessor;
        private readonly string hostname;
        private readonly Guid? guid;
        private readonly DateTime dateTime;

        public InfluxDbExporterOptions(IProfileService profileService) {
            this.profileService = profileService;
            profileService.ProfileChanged += ProfileService_ProfileChanged;
            profileService.ActiveProfile.PluginSettings.PropertyChanged += ProfileService_PropertyChanged;

            guid = PluginOptionsAccessor.GetAssemblyGuid(typeof(InfluxDbExporter));
            if (guid == null) {
                throw new Exception($"GUID was not found in assembly metadata");
            }

            this.pluginOptionsAccessor = new PluginOptionsAccessor(this.profileService, guid.Value);

            hostname = Environment.MachineName;
            dateTime = DateTime.Now;
        }

        public string InfluxDbUrl {
            get => pluginOptionsAccessor.GetValueString(nameof(InfluxDbUrl), string.Empty);
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbUrl), value);
                RaisePropertyChanged();
            }
        }

        public string InfluxDbBucket {
            get => pluginOptionsAccessor.GetValueString(nameof(InfluxDbBucket), string.Empty);
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbBucket), value);
                RaisePropertyChanged();
            }
        }

        public string InfluxDbOrgId {
            get => pluginOptionsAccessor.GetValueString(nameof(InfluxDbOrgId), string.Empty);
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbOrgId), value);
                RaisePropertyChanged();
            }
        }

        public string InfluxDbToken {
            get => Security.Decrypt(pluginOptionsAccessor.GetValueString(nameof(InfluxDbToken), string.Empty));
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbToken), Security.Encrypt(value));
                RaisePropertyChanged();
            }
        }

        public InfluxDbVersion InfluxDbVersion {
            get {
                var stored = pluginOptionsAccessor.GetValueString(nameof(InfluxDbVersion), nameof(InfluxDbVersion.V2));
                return Enum.TryParse<InfluxDbVersion>(stored, out var result) ? result : InfluxDbVersion.V2;
            }
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbVersion), value.ToString());
                RaisePropertyChanged();
            }
        }

        public string InfluxDbDatabase {
            get => pluginOptionsAccessor.GetValueString(nameof(InfluxDbDatabase), string.Empty);
            set {
                pluginOptionsAccessor.SetValueString(nameof(InfluxDbDatabase), value);
                RaisePropertyChanged();
            }
        }

        public bool TagImageFileName {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagImageFileName), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagImageFileName), value);
                RaisePropertyChanged();
            }
        }

        public bool TagFullImagePath {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagFullImagePath), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagFullImagePath), value);
                RaisePropertyChanged();
            }
        }

        public bool TagHostname {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagHostname), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagHostname), value);
                RaisePropertyChanged();
            }
        }

        public bool TagProfileName {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagProfileName), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagProfileName), value);
                RaisePropertyChanged();
            }
        }

        public bool TagEquipmentName {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagEquipmentName), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagEquipmentName), value);
                RaisePropertyChanged();
            }
        }

        public string Hostname => hostname;
        public string ProfileName => profileService.ActiveProfile?.Name;

        public bool AuthWorks { get; private set; }
        public string AuthFailureMessage { get; private set; }

        public string MeasurementName {
            get => pluginOptionsAccessor.GetValueString(nameof(MeasurementName), "events");
            set {
                pluginOptionsAccessor.SetValueString(nameof(MeasurementName), value);
                RaisePropertyChanged();
            }
        }

        public async Task CheckAuth() {
            try {
                if (string.IsNullOrWhiteSpace(InfluxDbUrl)) {
                    throw new Exception("InfluxDB URL is required");
                }

                if (!CheckURLValid(InfluxDbUrl)) {
                    throw new Exception("Invalid InfluxDB URL");
                }

                switch (InfluxDbVersion) {
                    case InfluxDbVersion.V1:
                        await CheckAuthV1();
                        break;

                    case InfluxDbVersion.V2:
                        await CheckAuthV2();
                        break;

                    case InfluxDbVersion.V3:
                        await CheckAuthV3();
                        break;
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to interact with {InfluxDbUrl}: {ex.Message}");
                AuthWorks = false;
                AuthFailureMessage = ex.Message;
            } finally {
                RaisePropertyChanged(nameof(AuthWorks));
                RaisePropertyChanged(nameof(AuthFailureMessage));
            }
        }

        private async Task CheckAuthV1() {
            if (string.IsNullOrWhiteSpace(InfluxDbDatabase)) {
                throw new Exception("Database name is required for InfluxDB 1.x");
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var pingUrl = InfluxDbUrl.TrimEnd('/') + "/ping";
            var response = await httpClient.GetAsync(pingUrl);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent) {
                throw new Exception($"Ping failed with HTTP {(int)response.StatusCode}");
            }

            AuthWorks = true;
            AuthFailureMessage = "Connection successful (InfluxDB 1.x / no-auth mode)";
        }

        private async Task CheckAuthV2() {
            if (string.IsNullOrWhiteSpace(InfluxDbToken) ||
                string.IsNullOrWhiteSpace(InfluxDbOrgId) ||
                string.IsNullOrWhiteSpace(InfluxDbBucket)) {
                throw new Exception("Token, Org ID, and Bucket are required for InfluxDB 2.x");
            }

            var options = new InfluxDBClientOptions(InfluxDbUrl) {
                Token = InfluxDbToken,
                Bucket = InfluxDbBucket,
                Org = InfluxDbOrgId,
            };

            using var client = new InfluxDBClient(options);

            if (!await client.PingAsync()) {
                throw new Exception("Failed to complete protocol ping. Wrong address or host is down?");
            }

            var bucketApi = client.GetBucketsApi();
            _ = await bucketApi.FindBucketByNameAsync(InfluxDbBucket) ?? throw new Exception($"Failed to access bucket {InfluxDbBucket}");

            var version = await client.VersionAsync();

            AuthWorks = true;
            AuthFailureMessage = $"Authentication was successful. InfluxDB server {version}";
        }

        private async Task CheckAuthV3() {
            if (string.IsNullOrWhiteSpace(InfluxDbDatabase)) {
                throw new Exception("Database name is required for InfluxDB 3.x");
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            if (!string.IsNullOrWhiteSpace(InfluxDbToken)) {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Token", InfluxDbToken);
            }

            var pingUrl = InfluxDbUrl.TrimEnd('/') + "/ping";
            var response = await httpClient.GetAsync(pingUrl);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent) {
                throw new Exception($"Ping failed with HTTP {(int)response.StatusCode}");
            }

            AuthWorks = true;
            AuthFailureMessage = string.IsNullOrWhiteSpace(InfluxDbToken)
                ? "Connection successful (InfluxDB 3.x / no-auth)"
                : "Connection successful (InfluxDB 3.x / token auth)";
        }

        public void SetInfluxDbToken(SecureString s) {
            InfluxDbToken = SecureStringToString(s);
        }

        private static string SecureStringToString(SecureString value) {
            IntPtr valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaiseAllPropertiesChanged();
        }

        private async void ProfileService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals(guid + "-" + nameof(InfluxDbUrl)) ||
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbToken)) ||
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbOrgId)) ||
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbBucket)) ||
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbVersion)) ||
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbDatabase))) {
                Logger.Trace($"Property changed: {e.PropertyName}");
                await CheckAuth();
            }
        }

        private static bool CheckURLValid(string url) {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        internal void RemoveProfileHandler() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            profileService.ActiveProfile.PropertyChanged -= ProfileService_PropertyChanged;
        }

        public string TokenDate => dateTime.ToString("d");
        public string TokenTime => dateTime.ToString("T");
        public string TokenDateTime => dateTime.ToString("G");
        public string TokenDateUtc => dateTime.ToUniversalTime().ToString("d");
        public string TokenTimeUtc => dateTime.ToUniversalTime().ToString("T");
        public string TokenDateTimeUtc => dateTime.ToUniversalTime().ToString("G");
        public string TokenUnixEpoch => Utilities.Utilities.UnixEpoch(dateTime).ToString();
    }
}