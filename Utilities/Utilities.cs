#region "copyright"

/*
    Copyright 2023 Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    public class Utilities {

        public static bool ConfigCheck(IInfluxDbExporterOptions options) {
            if (options == null) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbUrl)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbBucket)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbToken)) { return false; }
            if (string.IsNullOrEmpty(options.InfluxDbOrgId)) { return false; }

            return true;
        }
    }
}