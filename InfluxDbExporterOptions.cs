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
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter {

    public partial class InfluxDbExporterOptions : BaseINPC, IInfluxDbExporterOptions {
        private readonly IProfileService profileService;
        private readonly PluginOptionsAccessor pluginOptionsAccessor;
        private readonly string hostname;
        private readonly Guid? guid;

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

        public async Task CheckAuth() {
            try {
                if (string.IsNullOrWhiteSpace(InfluxDbUrl) ||
                                       string.IsNullOrWhiteSpace(InfluxDbToken) ||
                                                          string.IsNullOrWhiteSpace(InfluxDbOrgId) ||
                                                                             string.IsNullOrWhiteSpace(InfluxDbBucket)) {
                    throw new Exception($"Insufficient configuration");
                }

                if (!CheckURLValid(InfluxDbUrl)) {
                    throw new Exception($"Invalid InfluxDB URL");
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
            } catch (Exception ex) {
                Logger.Error($"Failed to interact with {InfluxDbUrl}: {ex.Message}");
                AuthWorks = false;
                AuthFailureMessage = ex.Message;
            } finally {
                RaisePropertyChanged(nameof(AuthWorks));
                RaisePropertyChanged(nameof(AuthFailureMessage));
            }
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
                e.PropertyName.Equals(guid + "-" + nameof(InfluxDbBucket))) {
                Logger.Trace($"Property changed: {e.PropertyName}");
                await CheckAuth();
            }
        }

        private static bool CheckURLValid(string url) {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
        }

        internal void RemoveProfileHandler() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            profileService.ActiveProfile.PropertyChanged -= ProfileService_PropertyChanged;
        }
    }
}