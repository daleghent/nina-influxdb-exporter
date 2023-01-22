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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Settings = DaleGhent.NINA.InfluxDbExporter.Properties.Settings;

namespace DaleGhent.NINA.InfluxDbExporter {

    [Export(typeof(IPluginManifest))]
    public class InfluxDbExporter : PluginBase {

        [ImportingConstructor]
        public InfluxDbExporter(IProfileService profileService,
                                IImageSaveMediator imageSaveMediator,
                                ICameraMediator cameraMediator,
                                IFocuserMediator focuserMediator,
                                IGuiderMediator guiderMediator,
                                ITelescopeMediator telescopeMediator,
                                IWeatherDataMediator weatherDataMediator) {
            if (Settings.Default.UpgradeSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            InfluxDbExporterOptions ??= new(profileService);
            ImageMetadata ??= new(imageSaveMediator, InfluxDbExporterOptions);
            EquipmentData ??= new(profileService, InfluxDbExporterOptions, cameraMediator, focuserMediator, guiderMediator, telescopeMediator, weatherDataMediator);
        }

        public override Task Teardown() {
            InfluxDbExporterOptions.RemoveProfileHandler();
            ImageMetadata.Unregister();
            EquipmentData.Dispose();

            return base.Teardown();
        }

        public InfluxDbExporterOptions InfluxDbExporterOptions { get; private set; }
        public ImageMetadata ImageMetadata { get; private set; }
        public EquipmentData EquipmentData { get; private set; }
    }
}