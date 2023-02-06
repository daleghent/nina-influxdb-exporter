#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using DaleGhent.NINA.InfluxDbExporter.Interfaces;
using InfluxDB.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    public class InfluxDbC {
        private readonly IInfluxDbExporterOptions options;
        private readonly InfluxDBClient client;

        public InfluxDbC(IInfluxDbExporterOptions options) {
            this.options = options;
            client = new InfluxDBClient(this.options.InfluxDbUrl, this.options.InfluxDbToken);
        }

        public WriteApi GetWriteApi() {
            return client.GetWriteApi();
        }
    }
}