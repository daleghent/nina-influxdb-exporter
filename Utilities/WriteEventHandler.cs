#region "copyright"

/*
    Copyright Dale Ghent <daleg@elemental.org>

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/
*/

#endregion "copyright"

using InfluxDB.Client.Writes;
using NINA.Core.Utility;
using System;

namespace DaleGhent.NINA.InfluxDbExporter.Utilities {

    public class WriteEventHandler {

        public static void WriteEvent(object sender, EventArgs e) {
            switch (e) {
                case WriteErrorEvent error:
                    Logger.Error($"Error writing point: {@error.Exception.Message}");
                    break;

                case WriteRuntimeExceptionEvent error:
                    Logger.Error($"Failed to write point: {@error.Exception.Message}");
                    break;
            }
        }
    }
}