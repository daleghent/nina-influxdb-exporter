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
using InfluxDB.Client;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.IO;
using InfluxDB.Client.Writes;
using System.Collections.Generic;

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
            try {
                var imgName = args.PathToImage.LocalPath;

                if (!options.SaveFullImagePath) {
                    imgName = Path.GetFileName(imgName);
                }

                var points = new List<PointData>();
                double valueDouble;
                long valueLong;

                var imgTime = args.MetaData.Image.ExposureStart.ToUniversalTime();
                var targetName = string.IsNullOrEmpty(args.MetaData.Target.Name) ? "No Target" : args.MetaData.Target.Name;

                valueDouble = double.IsNaN(args.Statistics?.Mean ?? 0d) ? 0d : args.Statistics.Mean;
                points.Add(PointData.Measurement("imageStatsMean")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.Median ?? 0d) ? 0d : args.Statistics.Median;
                points.Add(PointData.Measurement("imageStatsMedian")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.StDev ?? 0d) ? 0d : args.Statistics.StDev;
                points.Add(PointData.Measurement("imageStatsSD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.MedianAbsoluteDeviation ?? 0d) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                points.Add(PointData.Measurement("imageStatsMAD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.Min < 0) ? 0 : args.Statistics.Min;
                points.Add(PointData.Measurement("imageStatsMinimumADU")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.MinOccurrences < 0) ? 0 : args.Statistics.MinOccurrences;
                points.Add(PointData.Measurement("imageStatsMinimumADUcount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.Max < 0) ? 0 : args.Statistics.Max;
                points.Add(PointData.Measurement("imageStatsMaximumADU")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.MaxOccurrences < 0) ? 0 : args.Statistics.MaxOccurrences;
                points.Add(PointData.Measurement("imageStatsMaximumADUcount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFR ?? 0d) ? 0d : args.StarDetectionAnalysis.HFR;
                points.Add(PointData.Measurement("imageStatsHFR")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFRStDev ?? 0d) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                points.Add(PointData.Measurement("imageStatsHFR_SD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.StarDetectionAnalysis?.DetectedStars < 0) ? 0 : args.StarDetectionAnalysis.DetectedStars;
                points.Add(PointData.Measurement("imageStatsStarCount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.RA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.RA;
                points.Add(PointData.Measurement("imageStatsAverageRMS_RA")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.Dec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.Dec;
                points.Add(PointData.Measurement("imageStatsAverageRMS_Dec")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakRA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                points.Add(PointData.Measurement("imageStatsPeakRMS_RA")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakDec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                points.Add(PointData.Measurement("imageStatsPeakRMS_Dec")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Tag("targetname", targetName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
                using var writeApi = client.GetWriteApi();
                writeApi.WritePoints(points, options.InfluxDbBucket, options.InfluxDbOrgId);
                writeApi.Flush();
                writeApi.Dispose();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void Unregister() {
            imageSaveMediator.ImageSaved -= ImageSaved;
        }
    }
}