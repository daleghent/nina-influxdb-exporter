# InfluxDB Exporter

The **InfluxDB Exporter** plugin transmits various equipment and image metrics to [InfluxDB](https://www.influxdata.com/) 2.x, a [time series database](https://en.wikipedia.org/wiki/Time_series_database). These metrics can then be consumed for graphing, alerting, and other analysis using tools such as [Grafana](https://grafana.com/).

Metrics are provided by class of equipment and the statistics of each saved image. For equipment, metrics for each class are sent each time N.I.N.A. polls the hardware for its status. This also means that only connected hardware will have statistics transmitted. Certain metrics will also have tags associated with them.

Global tags, applied to all metrics:
* `profile_name`: The name of the active N.I.N.A. profile
* `host_name`: The name of the computer

### Astrometric

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `astro_moon_altitude` | Moon's altitude in degrees | double |
| `astro_sun_altitude` | Sun's altitude in degrees | double |

### Camera

Additional tags:
* `camera_name`: The name of the connected canera device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `camera_sensor_temperature` | Sensor temperature in °C | double |
| `camera_cooler_power` | Cooler power level in percent | double |
| `camera_battery_level` | Camera battery charge level | integer |

### Focuser

Additional tags:
* `focuser_name`: The name of the connected focuser device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `focuser_temperature` | Focuser's temperature sensor in °C | double 
| `focuser_position` | Step number/position reported by focuser | integer |

### Guider

Additional tags:
* `guider_name`: The name of the connected guiding device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `guider_rms_ra_arcsec` | RA RMS in arcseconds | double |
| `guider_rms_dec_arcsec` | Declination RMS in arcseconds | double |
| `guider_rms_arcsec` | Comined RMS in arcseconds | double |
| `guider_rms_ra_pixel` | RA RMS in pixels | double |
| `guider_rms_dec_pixel` | Declination RMS in pixels | double |
| `guider_rms_pixel` | Combined RMS in pixels | double |
| `guider_rms_peak_ra_arcsec` | RA peak RMS in arcseconds | double |
| `guider_rms_peak_dec_arcsec` | Declination peak RMS in arcseconds | double |
| `guider_rms_peak_arcsec` | Combined peak RMS in arcseconds | double |
| `guider_rms_peak_ra_pixel` | RA peak RMS in arcseconds | double |
| `guider_rms_peak_dec_pixel` | Declination peak RMS in pixels | double |
| `guider_rms_peak_pixel` | Combined peak RMS in pixels | double |

### Mount

Additional tags:
* `mount_name`: The name of the connected mount device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `mount_altitude` | Altitude in degrees | double |
| `mount_azimuth` | Azimuth in degrees | double |

### Rotator

Additional tags:
* `rotator_name`: The name of the connected rotator device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `rotator_mechanical_angle` | Mecanical angle in degrees | double |
| `rotator_angle` | Sky angle in degrees | double |

### Switches

Additional tags:
* `guider_name`: The name of the connected guiding device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `switch_ro_sw<ID>` | Value of gauge (read-only switch) identified by the appended numeric identifier | double |

Each swtich metric is tagged with the human-readable name associated with the switch's ID.

### Weather

Additional tags:
* `wx_device_name`: The name of the connected weather/observing conditions device

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `wx_cloud_cover` | Cloud cover in percent | double |
| `wx_dewpoint` | Dewpoint in °C | double |
| `wx_humidity` | Relative humidity in percent | double |
| `wx_pressure` | Air pressure in hectopascals | double |
| `wx_rain_rate` | Rain rate in mm/hour | double |
| `wx_sky_brightness` | Sky brightness in lux | double |
| `wx_sky_quality` | Sky quality in magnitudes/arcsecond<sup>2</sup> | double |
| `wx_sky_temperature` | Sky temperature in °C | double |
| `wx_star_fwhm` | Measured star FWHM | double |
| `wx_temperature` | Ambient air temperature in °C | double |
| `wx_wind_direction` | Wind direction in azimuthal degrees | double |
| `wx_wind_gust` | Wind gust speed in meters/second | double |
| `wx_wind_speed` | Wind speed in meters/second | double |

### Image statistics

Statistics are produced only for images of type `LIGHT`. Calibration frames and snapshots are not processed. Statistics must be on for many of these metrics to be sent.

Additional tags:
* `image_file_name`: Name of the image file associated with these metrics.
* `target_name`: Name of the target imaged as defined in the sequencer's target field. This tag is omitted if no target name is defined.
* `sequence_title`: The title or name of the running sequence
* `camera_name`: Name of the camera
* `readout_mode`: Name of the readout mode the camera is operating under

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `image_eccentricity` | Average star eccentricity (Hocus Focus only) | double |
| `image_fwhm` | Average star Full Width at Half Maximum (Hocus Focus only) | double |
| `image_hfr` | Average star HFR | double |
| `image_hfr_std_deviation` | Standard deviation of measured stars' HFR | double |
| `image_mad` | Pixel value mean average deviation | double |
| `image_max_adu` | Maximum pixel ADU value | integer |
| `image_max_adu_count` | Number of occurences of max. pixel ADU value | integer |
| `image_mean` | Pixel mean value | double |
| `image_median` | Pixel median value | double |
| `image_min_adu` | Minimum pixel ADU value | integer |
| `image_min_adu_count` | Number of occurences of min. pixel ADU value | integer |
| `image_rms_avg_ra_arcsec` | Average guiding RMS of the RA axis during image exposure  | double |
| `image_rms_avg_dec_arcsec` | Average guiding RMS of the declination axis during image exposure | double |
| `image_rms_avg_arcsec` | Combined average RMS during image exposure | double |
| `image_rms_peak_ra_arcsec` | Peak guiding RMS of the RA axis during image exposure | double |
| `image_rms_peak_dec_arcsec` | Peak guiding RMS of the declination axis during image exposure  | double |
| `image_rms_peak_arcsec` | Combined peak RMS during image exposure | double |
| `image_star_count` | Count of stars in the image | integer |
| `image_std_deviation` | Pixel value stardard deviation | double |

### Vendor or device-specific metrics

These metrics are specific to certain devices or vendors.

| Metric | Definition | Type |
| ------ | ---------- | ---- |
| `qhy_sensor_air_pressure` | Sensor chamber air pressure (certain QHY camera models) | double |
| `qhy_sensor_humidity` | Sensor chamber humidity (certain QHY camera models) | double |

# Getting help #

Help for this plugin may be found in the **#plugin-discussions** channel on the NINA project [Discord chat server](https://discord.gg/nighttime-imaging) or by filing an issue report at this plugin's [Github repository](https://github.com/daleghent/nina-influxdb-exporter/issues).**
**