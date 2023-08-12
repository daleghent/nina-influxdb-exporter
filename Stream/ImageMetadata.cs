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
using DaleGhent.NINA.InfluxDbExporter.Utilities;

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
                if (!Utilities.Utilities.ConfigCheck(this.options)) return;

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
                point = PointData.Measurement("image_mean")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.Median ?? 0d) ? 0d : args.Statistics.Median;
                point = PointData.Measurement("image_median")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.StDev ?? 0d) ? 0d : args.Statistics.StDev;
                point = PointData.Measurement("image_std_deviation")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.Statistics?.MedianAbsoluteDeviation ?? 0d) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                point = PointData.Measurement("image_mad")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.Min < 0) ? 0 : args.Statistics.Min;
                point = PointData.Measurement("image_min_adu")
                    .Field("value", valueLong)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.MinOccurrences < 0) ? 0 : args.Statistics.MinOccurrences;
                point = PointData.Measurement("image_min_adu_count")
                    .Field("value", valueLong)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.Max < 0) ? 0 : args.Statistics.Max;
                point = PointData.Measurement("image_max_adu")
                    .Field("value", valueLong)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueLong = (args.Statistics?.MaxOccurrences < 0) ? 0 : args.Statistics.MaxOccurrences;
                point = PointData.Measurement("image_max_adu_count")
                    .Field("value", valueLong)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFR ?? 0d) ? 0d : args.StarDetectionAnalysis.HFR;
                point = PointData.Measurement("image_hfr")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFRStDev ?? 0d) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                point = PointData.Measurement("image_hfr_std_deviation")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueLong = (args.StarDetectionAnalysis?.DetectedStars < 0) ? 0 : args.StarDetectionAnalysis.DetectedStars;
                point = PointData.Measurement("image_star_count")
                    .Field("value", valueLong)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.RA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.RA;
                points.Add(PointData.Measurement("image_avg_rms_ra")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns));

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.Dec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.Dec;
                point = PointData.Measurement("image_avg_rms_dec")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakRA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                point = PointData.Measurement("image_peak_rms_ra")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakDec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                point = PointData.Measurement("image_peak_rms_dec")
                    .Field("value", valueDouble)
                    .Tag("image_file_name", imgName)
                    .Timestamp(imgTime, WritePrecision.Ns);

                if (!string.IsNullOrEmpty(targetName)) {
                    point.Tag("target_name", targetName);
                }
                points.Add(point);

                using var client = new InfluxDBClient(options.InfluxDbUrl, options.InfluxDbToken);
                using var writeApi = client.GetWriteApi();
                writeApi.EventHandler += WriteEventHandler.WriteEvent;
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