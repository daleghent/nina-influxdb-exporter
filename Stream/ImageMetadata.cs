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
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaleGhent.NINA.InfluxDbExporter.Stream {

    public class ImageMetadata {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IInfluxDbExporterOptions options;

        public ImageMetadata(IInfluxDbExporterOptions options, IImageSaveMediator imageSaveMediator) {
            this.options = options;
            this.imageSaveMediator = imageSaveMediator;

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
                double valueDouble;
                long valueLong;

                var imgTime = args.MetaData.Image.ExposureStart.ToUniversalTime();

                valueDouble = double.IsNaN(args.Statistics?.Mean ?? 0d) ? 0d : args.Statistics.Mean;
                points.Add(PointData.Measurement("image_mean")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.Median ?? 0d) ? 0d : args.Statistics.Median;
                points.Add(PointData.Measurement("image_median")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.StDev ?? 0d) ? 0d : args.Statistics.StDev;
                points.Add(PointData.Measurement("image_std_deviation")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.Statistics?.MedianAbsoluteDeviation ?? 0d) ? 0d : args.Statistics.MedianAbsoluteDeviation;
                points.Add(PointData.Measurement("image_mad")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.Min < 0) ? 0 : args.Statistics.Min;
                points.Add(PointData.Measurement("image_min_adu")
                    .Field("value", valueLong)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.MinOccurrences < 0) ? 0 : args.Statistics.MinOccurrences;
                points.Add(PointData.Measurement("image_min_adu_count")
                    .Field("value", valueLong)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.Max < 0) ? 0 : args.Statistics.Max;
                points.Add(PointData.Measurement("image_max_adu")
                    .Field("value", valueLong)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.Statistics?.MaxOccurrences < 0) ? 0 : args.Statistics.MaxOccurrences;
                points.Add(PointData.Measurement("image_max_adu_count")
                    .Field("value", valueLong)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFR ?? 0d) ? 0d : args.StarDetectionAnalysis.HFR;
                points.Add(PointData.Measurement("image_hfr")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.StarDetectionAnalysis?.HFRStDev ?? 0d) ? 0d : args.StarDetectionAnalysis.HFRStDev;
                points.Add(PointData.Measurement("image_hfr_std_deviation")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueLong = (args.StarDetectionAnalysis?.DetectedStars < 0) ? 0 : args.StarDetectionAnalysis.DetectedStars;
                points.Add(PointData.Measurement("image_star_count")
                    .Field("value", valueLong)
                    .Timestamp(imgTime, WritePrecision.Ns));

                double rmsAvgRA = 0d;
                double rmsAvgDec = 0d;
                double rmsAvgTotal = 0d;

                if (!double.IsNaN(args.MetaData.Image.RecordedRMS?.RA ?? 0d) && !double.IsNaN(args.MetaData.Image.RecordedRMS?.Dec ?? 0d)) {
                    rmsAvgRA = args.MetaData.Image.RecordedRMS.RA;
                    rmsAvgDec = args.MetaData.Image.RecordedRMS.Dec;
                    rmsAvgTotal = Math.Sqrt(Math.Pow(rmsAvgRA, 2) + Math.Pow(rmsAvgDec, 2));
                }

                points.Add(PointData.Measurement("image_rms_avg_ra_arcsec")
                    .Field("value", rmsAvgRA)
                    .Timestamp(imgTime, WritePrecision.Ns));

                points.Add(PointData.Measurement("image_rms_avg_dec_arcsec")
                    .Field("value", rmsAvgDec)
                    .Timestamp(imgTime, WritePrecision.Ns));

                points.Add(PointData.Measurement("image_rms_avg_arcsec")
                    .Field("value", rmsAvgTotal)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakRA ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakRA;
                points.Add(PointData.Measurement("image_rms_peak_ra_arcsec")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.PeakDec ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.PeakDec;
                points.Add(PointData.Measurement("image_rms_peak_dec_arcsec")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                valueDouble = double.IsNaN(args.MetaData.Image.RecordedRMS?.Total ?? 0d) ? 0d : args.MetaData.Image.RecordedRMS.Total;
                points.Add(PointData.Measurement("image_rms_peak_arcsec")
                    .Field("value", valueDouble)
                    .Timestamp(imgTime, WritePrecision.Ns));

                // Send the points
                var fullOptions = new InfluxDBClientOptions(options.InfluxDbUrl) {
                    Token = options.InfluxDbToken,
                };

                fullOptions.AddDefaultTag("image_file_name", imgName);

                if (!string.IsNullOrEmpty(args.MetaData.Target.Name)) {
                    fullOptions.AddDefaultTag("target_name", args.MetaData.Target.Name);
                }

                if (!string.IsNullOrEmpty(args.MetaData.Target.Name)) {
                    fullOptions.AddDefaultTag("sequence_title", args.MetaData.Sequence.Title);
                }

                using var client = new InfluxDBClient(fullOptions);
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