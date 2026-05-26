// Project configuration values — edit these for your hardware and network
#ifndef CONFIG_H
#define CONFIG_H

// --- WiFi ---------------------------------------------------------------
// Replace with your WiFi network name and password
#define WIFI_SSID "Galaxy S21 FE 5G1ba7"
#define WIFI_PASSWORD "jouf8296"

// --- Time / timezone ---------------------------------------------------
// TZ string for local timezone (POSIX tz format or e.g. "UTC0" or
// "CET-1CEST,M3.5.0/02:00:00,M10.5.0/03:00:00").
#define TZ_INFO "UTC0"

// --- Pins ---------------------------------------------------------------
// Adjust these to match your ESP32 wiring
#define WATER_PUMP_PIN 14
#define SOIL_MOISTURE_PIN 34

// Display I2C pins and address for Qapass 1602A
#define DISPLAY_I2C_SDA_PIN 21
#define DISPLAY_I2C_SCL_PIN 22
#define DISPLAY_I2C_ADDRESS 0x27

// --- Soil moisture calibration -----------------------------------------
// Raw ADC value when probe is fully wet
#define SOIL_MOISTURE_RAW_WET 1200
// Raw ADC value when probe is dry
#define SOIL_MOISTURE_RAW_DRY 2600
// Threshold percent to turn pump on (0-100)
#define MOISTURE_THRESHOLD_PERCENT 30.0f

// --- Intervals (milliseconds) -----------------------------------------
// How often the moisture controller samples/acts
#define SEND_INTERVAL_MS 5000UL
// How often Thinger uploader will attempt bucket sends
#define THINGER_SEND_INTERVAL_MS 60000UL

// --- Thinger.io credentials --------------------------------------------
// Replace these with your Thinger.io account/device values
#define THINGER_USERNAME "YOUR_THINGER_USERNAME"
#define THINGER_DEVICE_ID "YOUR_DEVICE_ID"
#define THINGER_DEVICE_CREDENTIAL "YOUR_DEVICE_CREDENTIAL"
#define THINGER_BUCKET_ID "YOUR_BUCKET_ID"

#endif // CONFIG_H
