#nullable enable

#region "copyright"

/*
    Copyright (c) 2025 Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DaleGhent.NINA.InfluxDbExporter.Enums;
using DaleGhent.NINA.InfluxDbExporter.MetadataClient;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Instructions {

    [ExportMetadata("Name", "InfluxDB Metric")]
    [ExportMetadata("Description", "Send a measurement to InfluxDB")]
    [ExportMetadata("Icon", "InfluxDB_Logo_SVG")]
    [ExportMetadata("Category", "Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class InfluxDbMetric : SequenceItem, IValidatable {
        private readonly ICameraMediator cameraMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private readonly ISwitchMediator switchMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IWeatherDataMediator weatherDataMediator;

        private readonly IMetadata metadata;
        private IWindowService? windowService = null;

        [ImportingConstructor]
        public InfluxDbMetric(ICameraMediator cameraMediator,
                             IDomeMediator domeMediator,
                             IFilterWheelMediator filterWheelMediator,
                             IFlatDeviceMediator flatDeviceMediator,
                             IFocuserMediator focuserMediator,
                             IGuiderMediator guiderMediator,
                             IRotatorMediator rotatorMediator,
                             ISafetyMonitorMediator safetyMonitorMediator,
                             ISwitchMediator switchMediator,
                             ITelescopeMediator telescopeMediator,
                             IWeatherDataMediator weatherDataMediator) {
            this.cameraMediator = cameraMediator;
            this.domeMediator = domeMediator;
            this.guiderMediator = guiderMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.focuserMediator = focuserMediator;
            this.guiderMediator = guiderMediator;
            this.rotatorMediator = rotatorMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.switchMediator = switchMediator;
            this.telescopeMediator = telescopeMediator;
            this.weatherDataMediator = weatherDataMediator;

            metadata = new Metadata(cameraMediator,
                domeMediator, filterWheelMediator, flatDeviceMediator, focuserMediator,
                guiderMediator, rotatorMediator, safetyMonitorMediator, switchMediator,
                telescopeMediator, weatherDataMediator);

            MeasurementName = InfluxDbExporter.InfluxDbExporterOptions?.MeasurementName ?? "events";
        }

        private string measurementName = string.Empty;

        [JsonProperty]
        public string MeasurementName {
            get => measurementName;
            set {
                if (measurementName != value) {
                    measurementName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string measurementDescription = string.Empty;

        [JsonProperty]
        public string MeasurementDescription {
            get => measurementDescription;
            set {
                if (measurementDescription != value) {
                    measurementDescription = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<InfluxDbMetricEntity> measurementEntities = [];

        [JsonProperty]
        public ObservableCollection<InfluxDbMetricEntity> MeasurementEntities {
            get {
                if (measurementEntities.Count == 0) {
                    measurementEntities.Add(new InfluxDbMetricEntity());
                }

                return measurementEntities;
            }
            private set {
                ObservableCollection<InfluxDbMetricEntity> newTags = [];

                foreach (var tag in value) {
                    if (tag.Name.Equals(string.Empty, StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.Equals(string.Empty, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    newTags.Add(tag);
                }

                if (newTags.Count == 0) {
                    newTags.Add(new InfluxDbMetricEntity());
                }

                measurementEntities = newTags;
                RaisePropertyChanged();
            }
        }

        private InfluxDbMetricEntity? selectedEntity;

        public InfluxDbMetricEntity? SelectedEntity {
            get => selectedEntity;
            set {
                if (selectedEntity != value) {
                    selectedEntity = value;
                    // Nudge WPF to requery command can-execute state.
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var points = new List<PointData>();

            var point = PointData.Measurement(MeasurementName)
                            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            foreach (var entity in MeasurementEntities) {
                if (string.IsNullOrWhiteSpace(entity.Name) || string.IsNullOrWhiteSpace(entity.Value)) {
                    continue;
                }
                switch (entity.Type) {
                    case MetricEntityTypes.Tag:
                        point = point.Tag(entity.Name, Utilities.TokenResolver.ResolveTokens(entity.Value, this, metadata));
                        break;

                    case MetricEntityTypes.Field:
                        if (double.TryParse(Utilities.TokenResolver.ResolveTokens(entity.Value, this, metadata), out double d)) {
                            point = point.Field(entity.Name, d);
                        } else if (bool.TryParse(Utilities.TokenResolver.ResolveTokens(entity.Value, this, metadata), out bool b)) {
                            point = point.Field(entity.Name, b);
                        } else {
                            point = point.Field(entity.Name, Utilities.TokenResolver.ResolveTokens(entity.Value, this, metadata));
                        }
                        break;

                    default:
                        break;
                }
            }

            points.Add(point);

            await Utilities.Utilities.SendPoints(InfluxDbExporter.InfluxDbExporterOptions, points);
        }

        public IList<string> Issues { get; set; } = [];

        public bool Validate() {
            var i = new List<string>();

            if (string.IsNullOrWhiteSpace(MeasurementName)) {
                i.Add("Measurement name must be set");
            }

            if (!Utilities.Utilities.ConfigCheck(InfluxDbExporter.InfluxDbExporterOptions)) {
                i.Add("InfluxDB configuration is invalid, please check the plugin options");
            }

            if (MeasurementEntities.Count == 0) {
                i.Add("At least one measurement entity must be defined");
            } else {
                foreach (var entity in MeasurementEntities) {
                    if (string.IsNullOrWhiteSpace(entity.Name)) {
                        i.Add("All measurement entities must have a name");
                    }
                    if (string.IsNullOrWhiteSpace(entity.Value)) {
                        i.Add("All measurement entities must have a value");
                    }
                }
            }

            if (i != Issues) {
                Issues = i;
                RaisePropertyChanged(nameof(Issues));
            }

            return i.Count == 0;
        }

        public InfluxDbMetric(InfluxDbMetric copyMe) : this(
                                        cameraMediator: copyMe.cameraMediator,
                                        domeMediator: copyMe.domeMediator,
                                        filterWheelMediator: copyMe.filterWheelMediator,
                                        flatDeviceMediator: copyMe.flatDeviceMediator,
                                        focuserMediator: copyMe.focuserMediator,
                                        guiderMediator: copyMe.guiderMediator,
                                        rotatorMediator: copyMe.rotatorMediator,
                                        safetyMonitorMediator: copyMe.safetyMonitorMediator,
                                        switchMediator: copyMe.switchMediator,
                                        telescopeMediator: copyMe.telescopeMediator,
                                        weatherDataMediator: copyMe.weatherDataMediator) {
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            return new InfluxDbMetric(this) {
                MeasurementName = MeasurementName,
                MeasurementDescription = MeasurementDescription,
                MeasurementEntities = MeasurementEntities,
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {Name}, Description: {MeasurementDescription}";
        }

        public IWindowService WindowService {
            get {
                windowService ??= new WindowService();
                return windowService;
            }

            set => windowService = value;
        }

        // This attribute will auto generate a RelayCommand for the method. It is called <methodname>Command -> OpenConfigurationWindowCommand. The class has to be marked as partial for it to work.
        [RelayCommand]
        private async Task OpenConfigurationWindow(object o) {
            var conf = new InfluxDbMetricSetup() {
                MeasurementName = measurementName,
                MeasurementDescription = measurementDescription,
                MeasurementEntities = measurementEntities,
            };

            await WindowService.ShowDialog(conf, "InfluxDB Metric Setup", System.Windows.ResizeMode.CanResizeWithGrip, System.Windows.WindowStyle.SingleBorderWindow);

            MeasurementName = conf.MeasurementName;
            MeasurementDescription = conf.MeasurementDescription;
            MeasurementEntities = conf.MeasurementEntities;
        }
    }

    public partial class InfluxDbMetricSetup : BaseINPC {

        //This will create a public property for the class with the same name but a starting capital letter
        //The generated property will have a getter and setter and the setter will automatically raise INotifyPropertyChanged so the UI can update automatically the value
        [ObservableProperty]
        private string measurementName = string.Empty;

        [ObservableProperty]
        private string measurementDescription = string.Empty;

        [ObservableProperty]
        private ObservableCollection<InfluxDbMetricEntity> measurementEntities = [];

        [ObservableProperty]
        private InfluxDbMetricEntity? selectedEntity = null;

        [RelayCommand]
        private void AddEntityRow(object o) {
            MeasurementEntities.Add(new InfluxDbMetricEntity());
        }

        [RelayCommand]
        private void RemoveEntityRow(object o) {
            if (SelectedEntity != null) {
                if (MeasurementEntities.Count == 1) {
                    MeasurementEntities.Clear();
                    MeasurementEntities.Add(new InfluxDbMetricEntity());
                    SelectedEntity = null;
                    return;
                }

                MeasurementEntities.Remove(SelectedEntity);
                SelectedEntity = null;
            }
        }
    }

    public class InfluxDbMetricEntity : INotifyPropertyChanged {
        private MetricEntityTypes _type = MetricEntityTypes.Field;
        private string _name = string.Empty;
        private string _value = string.Empty;

        public MetricEntityTypes Type {
            get => _type;
            set { if (_type != value) { _type = value; OnPropertyChanged(); } }
        }

        public string Name {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        public string Value {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

#nullable disable