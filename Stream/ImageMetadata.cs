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
using InfluxDB.Client.Core;
using InfluxDB.Client;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.IO;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class ImageMetadata {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IInfluxDbExporterOptions options;

        public ImageMetadata(IInfluxDbExporterOptions options, IImageSaveMediator imageSaveMediator) {
            this.imageSaveMediator = imageSaveMediator;
            this.options = options;

            this.imageSaveMediator.ImageSaved += ImageSaved;
        }

        private void ImageSaved(object sender, ImageSavedEventArgs args) {
            Logger.Trace($"Connecting to {options.InfluxDbUrl}");

            try {
                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
                using var writeApi = client.GetWriteApi();

                var imgName = args.PathToImage.LocalPath;

                if (!options.SaveFullImagePath) {
                    imgName = Path.GetFileName(imgName);
                }

                var imgTime = args.MetaData.Image.ExposureStart.ToUniversalTime();
                var targetName = string.IsNullOrEmpty(args.MetaData.Target.Name) ? "No Target" : args.MetaData.Target.Name;

                var meanValue = double.IsNaN(args.Statistics.Mean) ? 0d : args.Statistics.Mean;
                var mean = new Mean { ImageName = imgName, TargetName = targetName, Value = meanValue, Time = imgTime };
                writeApi.WriteMeasurement(mean, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var medianValue = double.IsNaN(args.Statistics.Median) ? 0d : args.Statistics.Median;
                var median = new Median { ImageName = imgName, TargetName = targetName, Value = medianValue, Time = imgTime };
                writeApi.WriteMeasurement(median, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var sdValue = double.IsNaN(args.Statistics.StDev) ? 0d : args.Statistics.StDev;
                var sd = new SD { ImageName = imgName, TargetName = targetName, Value = sdValue, Time = imgTime };
                writeApi.WriteMeasurement(sd, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var madValue = double.IsNaN(args.Statistics.MedianAbsoluteDeviation) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                var mad = new MAD { ImageName = imgName, TargetName = targetName, Value = madValue, Time = imgTime };
                writeApi.WriteMeasurement(mad, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var minAduValue = double.IsNaN(args.Statistics.Min) ? 0d : args.Statistics.Min;
                var minAdu = new MinAdu { ImageName = imgName, TargetName = targetName, Value = minAduValue, Time = imgTime };
                writeApi.WriteMeasurement(minAdu, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var maxAduValue = double.IsNaN(args.Statistics.Max) ? 0d : args.Statistics.Max;
                var maxAdu = new MaxAdu { ImageName = imgName, TargetName = targetName, Value = maxAduValue, Time = imgTime };
                writeApi.WriteMeasurement(maxAdu, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var hfrValue = double.IsNaN(args.StarDetectionAnalysis.HFR) ? 0d : args.StarDetectionAnalysis.HFR;
                var hfr = new HFR { ImageName = imgName, TargetName = targetName, Value = hfrValue, Time = imgTime };
                writeApi.WriteMeasurement(hfr, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var hfrSdValue = double.IsNaN(args.StarDetectionAnalysis.HFRStDev) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                var hfrSd = new HFRSD { ImageName = imgName, TargetName = targetName, Value = hfrSdValue, Time = imgTime };
                writeApi.WriteMeasurement(hfrSd, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var starCountValue = double.IsNaN(args.StarDetectionAnalysis.DetectedStars) ? 0d : args.StarDetectionAnalysis.DetectedStars;
                var starCount = new StarCount { ImageName = imgName, TargetName = targetName, Value = starCountValue, Time = imgTime };
                writeApi.WriteMeasurement(starCount, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var rmsRaValue = double.IsNaN(args.MetaData.Image.RecordedRMS.RA) ? 0d : args.MetaData.Image.RecordedRMS.RA;
                var rmsRa = new RmsRa { ImageName = imgName, TargetName = targetName, Value = rmsRaValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsRa, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var rmsDecValue = double.IsNaN(args.MetaData.Image.RecordedRMS.Dec) ? 0d : args.MetaData.Image.RecordedRMS.Dec;
                var rmsDec = new RmsDec { ImageName = imgName, TargetName = targetName, Value = rmsDecValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsDec, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var rmsRaPeakValue = double.IsNaN(args.MetaData.Image.RecordedRMS.PeakRA) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                var rmsRaPeak = new RmsRaPeak { ImageName = imgName, TargetName = targetName, Value = rmsRaPeakValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsRaPeak, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                var rmsDePeakValue = double.IsNaN(args.MetaData.Image.RecordedRMS.PeakDec) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                var rmsDecPeak = new RmsDecPeak { ImageName = imgName, TargetName = targetName, Value = rmsDePeakValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsDecPeak, WritePrecision.Ns, options.InfluxDbBucket, options.InfluxDbOrgId);

                writeApi.Flush();
                writeApi.Dispose();
                client.Dispose();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        [Measurement("imageStatsMean")]
        private class Mean {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsMedian")]
        private class Median {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsSD")]
        private class SD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsHFR")]
        private class HFR {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsHFR_SD")]
        private class HFRSD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsMAD")]
        private class MAD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsMinimumADU")]
        private class MinAdu {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsMaximumADU")]
        private class MaxAdu {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsStarCount")]
        private class StarCount {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsAverageRMS_RA")]
        private class RmsRa {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsAverageRMS_Dec")]
        private class RmsDec {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsPeakRMS_RA")]
        private class RmsRaPeak {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("imageStatsPeakRMS_Dec")]
        private class RmsDecPeak {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        public void Unregister() {
            imageSaveMediator.ImageSaved -= ImageSaved;
        }
    }
}