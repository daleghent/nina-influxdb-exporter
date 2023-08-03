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
                PointData point;

                double valueDouble;
                long valueLong;

                var imgTime = args.MetaData.Image.ExposureStart.ToUniversalTime();
                var targetName = args.MetaData.Target.Name;

                valueDouble = double.IsNaN(args.Statistics?.Mean ?? 0d) ? 0d : args.Statistics.Mean;
                point = PointData.Measurement("imageStatsMean")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.Median ?? 0d) ? 0d : args.Statistics.Median;
                point = PointData.Measurement("imageStatsMedian")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.StDev ?? 0d) ? 0d : args.Statistics.StDev;
                point = PointData.Measurement("imageStatsSD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.MedianAbsoluteDeviation ?? 0d) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                point = PointData.Measurement("imageStatsMAD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.Min < 0) ? 0 : args.Statistics.Min;
                point = PointData.Measurement("imageStatsMinimumADU")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.MinOccurrences < 0) ? 0 : args.Statistics.MinOccurrences;
                point = PointData.Measurement("imageStatsMinimumADUcount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.Max < 0) ? 0 : args.Statistics.Max;
                point = PointData.Measurement("imageStatsMaximumADU")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.MaxOccurrences < 0) ? 0 : args.Statistics.MaxOccurrences;
                point = PointData.Measurement("imageStatsMaximumADUcount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFR ?? 0d) ? 0d : args.StarDetectionAnalysis.HFR;
                point = PointData.Measurement("imageStatsHFR")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFRStDev ?? 0d) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                point = PointData.Measurement("imageStatsHFR_SD")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueLong = (args.StarDetectionAnalysis?.DetectedStars < 0) ? 0 : args.StarDetectionAnalysis.DetectedStars;
                point = PointData.Measurement("imageStatsStarCount")
                    .Field("value", valueLong)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.RA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.RA;
                points.Add(PointData.Measurement("imageStatsAverageRMS_RA")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.Dec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.Dec;
                point = PointData.Measurement("imageStatsAverageRMS_Dec")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakRA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                point = PointData.Measurement("imageStatsPeakRMS_RA")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakDec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                point = PointData.Measurement("imageStatsPeakRMS_Dec")
                    .Field("value", valueDouble)
                    .Tag("imagename", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("targetname", targetName);
                }
                points.Add(point);

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