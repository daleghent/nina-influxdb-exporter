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
                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbUserName, options.InfluxDbUserPassword, options.InfluxDbDbName, string.Empty);
                using var writeApi = client.GetWriteApi();

                var imgName = args.PathToImage.LocalPath;

                if (!options.SaveFullImagePath) {
                    imgName = Path.GetFileName(imgName);
                }

                var imgTime = args.MetaData.Image.ExposureStart.ToUniversalTime();
                var targetName = string.IsNullOrEmpty(args.MetaData.Target.Name) ? "No Target" : args.MetaData.Target.Name;

                var meanValue = double.IsNaN(args.Statistics.Mean) ? 0d : args.Statistics.Mean;
                var mean = new Mean { ImageName = imgName, TargetName = targetName, Value = meanValue, Time = imgTime };
                writeApi.WriteMeasurement(mean, WritePrecision.Ns);

                var medianValue = double.IsNaN(args.Statistics.Median) ? 0d : args.Statistics.Median;
                var median = new Median { ImageName = imgName, TargetName = targetName, Value = medianValue, Time = imgTime };
                writeApi.WriteMeasurement(median, WritePrecision.Ns);

                var sdValue = double.IsNaN(args.Statistics.StDev) ? 0d : args.Statistics.StDev;
                var sd = new SD { ImageName = imgName, TargetName = targetName, Value = sdValue, Time = imgTime };
                writeApi.WriteMeasurement(sd, WritePrecision.Ns);

                var madValue = double.IsNaN(args.Statistics.MedianAbsoluteDeviation) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                var mad = new MAD { ImageName = imgName, TargetName = targetName, Value = madValue, Time = imgTime };
                writeApi.WriteMeasurement(mad, WritePrecision.Ns);

                var minAduValue = double.IsNaN(args.Statistics.Min) ? 0d : args.Statistics.Min;
                var minAdu = new MinAdu { ImageName = imgName, TargetName = targetName, Value = minAduValue, Time = imgTime };
                writeApi.WriteMeasurement(minAdu, WritePrecision.Ns);

                var maxAduValue = double.IsNaN(args.Statistics.Max) ? 0d : args.Statistics.Max;
                var maxAdu = new MaxAdu { ImageName = imgName, TargetName = targetName, Value = maxAduValue, Time = imgTime };
                writeApi.WriteMeasurement(maxAdu, WritePrecision.Ns);

                var hfrValue = double.IsNaN(args.StarDetectionAnalysis.HFR) ? 0d : args.StarDetectionAnalysis.HFR;
                var hfr = new HFR { ImageName = imgName, TargetName = targetName, Value = hfrValue, Time = imgTime };
                writeApi.WriteMeasurement(hfr, WritePrecision.Ns);

                var hfrSdValue = double.IsNaN(args.StarDetectionAnalysis.HFRStDev) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                var hfrSd = new HFRSD { ImageName = imgName, TargetName = targetName, Value = hfrSdValue, Time = imgTime };
                writeApi.WriteMeasurement(hfrSd, WritePrecision.Ns);

                var starCountValue = double.IsNaN(args.StarDetectionAnalysis.DetectedStars) ? 0d : args.StarDetectionAnalysis.DetectedStars;
                var starCount = new StarCount { ImageName = imgName, TargetName = targetName, Value = starCountValue, Time = imgTime };
                writeApi.WriteMeasurement(starCount, WritePrecision.Ns);

                var rmsRaValue = double.IsNaN(args.MetaData.Image.RecordedRMS.RA) ? 0d : args.MetaData.Image.RecordedRMS.RA;
                var rmsRa = new RmsRa { ImageName = imgName, TargetName = targetName, Value = rmsRaValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsRa, WritePrecision.Ns);

                var rmsDecValue = double.IsNaN(args.MetaData.Image.RecordedRMS.Dec) ? 0d : args.MetaData.Image.RecordedRMS.Dec;
                var rmsDec = new RmsDec { ImageName = imgName, TargetName = targetName, Value = rmsDecValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsDec, WritePrecision.Ns);

                var rmsRaPeakValue = double.IsNaN(args.MetaData.Image.RecordedRMS.PeakRA) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                var rmsRaPeak = new RmsRaPeak { ImageName = imgName, TargetName = targetName, Value = rmsRaPeakValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsRaPeak, WritePrecision.Ns);

                var rmsDePeakValue = double.IsNaN(args.MetaData.Image.RecordedRMS.PeakDec) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                var rmsDecPeak = new RmsDecPeak { ImageName = imgName, TargetName = targetName, Value = rmsDePeakValue, Time = imgTime };
                writeApi.WriteMeasurement(rmsDecPeak, WritePrecision.Ns);

                writeApi.Flush();
                writeApi.Dispose();
                client.Dispose();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        [Measurement("mean")]
        private class Mean {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("median")]
        private class Median {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("sd")]
        private class SD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("hfr")]
        private class HFR {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("hfrsd")]
        private class HFRSD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("mad")]
        private class MAD {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("minadu")]
        private class MinAdu {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("maxadu")]
        private class MaxAdu {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("starcount")]
        private class StarCount {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("rmsRA")]
        private class RmsRa {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("rmsDec")]
        private class RmsDec {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("peakRmsRA")]
        private class RmsRaPeak {
            [Column("imagename", IsTag = true)] public string ImageName { get; set; }
            [Column("targetname", IsTag = true)] public string TargetName { get; set; }
            [Column("value")] public double Value { get; set; }
            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }

        [Measurement("peakRmsDec")]
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