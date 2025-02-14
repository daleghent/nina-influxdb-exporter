#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Stream;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Settings = DaleGhent.NINA.InfluxDbExporter.Properties.Settings;

namespace DaleGhent.NINA.InfluxDbExporter {

    [Export(typeof(IPluginManifest))]
    public class InfluxDbExporter : PluginBase {
        public readonly CancellationTokenSource cts;

        [ImportingConstructor]
        public InfluxDbExporter(IProfileService profileService,
                                IImageSaveMediator imageSaveMediator,
                                ICameraMediator cameraMediator,
                                IFocuserMediator focuserMediator,
                                IGuiderMediator guiderMediator,
                                IRotatorMediator rotatorMediator,
                                ISwitchMediator switchMediator,
                                ITelescopeMediator telescopeMediator,
                                IWeatherDataMediator weatherDataMediator) {
            if (Settings.Default.UpgradeSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            cts = new();

            InfluxDbExporterOptions ??= new(profileService);

            CameraData ??= new(InfluxDbExporterOptions, cameraMediator);
            FocuserData ??= new(InfluxDbExporterOptions, focuserMediator);
            GuidingData ??= new(InfluxDbExporterOptions, guiderMediator);
            MountData ??= new(InfluxDbExporterOptions, telescopeMediator);
            RotatorData ??= new(InfluxDbExporterOptions, rotatorMediator);
            SwitchData ??= new(InfluxDbExporterOptions, switchMediator);
            WeatherData ??= new(InfluxDbExporterOptions, weatherDataMediator);
            ImageMetadata ??= new(InfluxDbExporterOptions, imageSaveMediator);
            MiscData ??= new(InfluxDbExporterOptions, profileService);
        }

        public override async Task Initialize() {
            try {
                await InfluxDbExporterOptions.CheckAuth();
            } catch (Exception ex) {
                Logger.Debug($"Failed to check auth on startup: {ex.Message}");
            }

            _ = Task.Run(() => MiscData.Run(cts.Token));
        }

        public override Task Teardown() {
            cts.Cancel();

            InfluxDbExporterOptions.RemoveProfileHandler();
            CameraData.Dispose();
            FocuserData.Dispose();
            MountData.Dispose();
            RotatorData.Dispose();
            SwitchData.Dispose();
            WeatherData.Dispose();
            GuidingData.Dispose();
            MiscData.Dispose();
            ImageMetadata.Unregister();

            cts.Dispose();

            return base.Teardown();
        }

        public CameraData CameraData { get; set; }
        public FocuserData FocuserData { get; set; }
        public GuidingData GuidingData { get; set; }
        public MountData MountData { get; set; }
        public RotatorData RotatorData { get; set; }
        public SwitchData SwitchData { get; set; }
        public WeatherData WeatherData { get; set; }
        public MiscData MiscData { get; set; }
        public InfluxDbExporterOptions InfluxDbExporterOptions { get; private set; }
        public ImageMetadata ImageMetadata { get; private set; }
    }
}