# Influx Exporter
[Documentation](https://daleghent.com/influxdb-exporter)

## 1.0.0.900 - 2025-10-16
* Added event metrics for hardware, imaging, and autofocus events. A metric with Grafana-style Annotation tags and fields are emitted when the following occurs:
  * Hardware connect and disconnect
  * Hardware use or movement:
	* Mount or dome slewed, parked, unparked, and homed
	* Filter changed
	* Rotator moved
	* Safe or unsafe condition change, with annotation time range
	* Guide dither
	* Flat/cover device opened or closed, light on or off, and light brightness changes
  * Image capture completed
  * Autofocus run completed
* Added **InfluxDB Metric** instruction to allow free-form event metrics to be defined and placed where required in a sequence. Field and tag data accepts Ground Station-style tokens.

## 1.0.0.105 - 2025-02-07
* Added example Grafana dashboard
* Fixed: Uncaught exception when closing NINA

## 1.0.0.104 - 2025-01-08
* Added metrics for Hocus Focus' FWHM and star eccentricity

## 1.0.0.103 - 2025-01-07
* Added metrics for QHY camera sensor chamber humidity and air pressure
* Fixed: Relaxed unintentionally strict URL validation, permitting connections to InfluxDB Cloud 2

## 1.0.0.101 - 2024-12-17
* Initial **beta** release
