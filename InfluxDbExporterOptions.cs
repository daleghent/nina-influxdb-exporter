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
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace DaleGhent.NINA.InfluxDbExporter {

    public class InfluxDbExporterOptions : BaseINPC, IInfluxDbExporterOptions {
        private readonly IProfileService profileService;
        private readonly IPluginOptionsAccessor pluginOptionsAccessor;
        private readonly string hostname;

        public InfluxDbExporterOptions(IProfileService profileService) {
            this.profileService = profileService;
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            var guid = PluginOptionsAccessor.GetAssemblyGuid(typeof(InfluxDbExporter));
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

        public bool TagFullImagePath {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagFullImagePath), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagFullImagePath), value);
                RaisePropertyChanged();
            }
        }

        public bool TagHostname {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagHostname), true);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagHostname), value);
                RaisePropertyChanged();
            }
        }

        public bool TagProfileName {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagProfileName), true);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagProfileName), value);
                RaisePropertyChanged();
            }
        }

        public bool TagEquipmentName {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(TagEquipmentName), true);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(TagEquipmentName), value);
                RaisePropertyChanged();
            }
        }

        public string Hostname => hostname;
        public string ProfileName => profileService.ActiveProfile?.Name;

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

        internal void RemoveProfileHandler() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
        }
    }
}