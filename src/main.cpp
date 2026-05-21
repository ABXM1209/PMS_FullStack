#include <Arduino.h>
#include <WiFi.h>

#include <ThingerESP32.h>

#include "config.h"
#include "core/MoistureSensorController.h"
#include "core/PumpController.h"
#include "core/DisplayController.h"
#include "utils/TimeUtils.h"
#include "core/ThingerUploader.h"

// Configuration values are provided by src/config.h

PumpController pumpController(WATER_PUMP_PIN);

MoistureSensorController moistureController(
    SOIL_MOISTURE_PIN,
    pumpController,
    SOIL_MOISTURE_RAW_WET,
    SOIL_MOISTURE_RAW_DRY,
    MOISTURE_THRESHOLD_PERCENT,
    SEND_INTERVAL_MS);

ThingerUploader thingerUploader(
    THINGER_USERNAME,
    THINGER_DEVICE_ID,
    THINGER_DEVICE_CREDENTIAL,
    THINGER_BUCKET_ID,
    THINGER_SEND_INTERVAL_MS);

DisplayController displayController(
    DISPLAY_I2C_SDA_PIN,
    DISPLAY_I2C_SCL_PIN,
    DISPLAY_I2C_ADDRESS);

static void connectToWiFi() {
  Serial.printf("Connecting to WiFi '%s'...\n", WIFI_SSID);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print('.');
  }
  Serial.println();
  Serial.print("WiFi connected, IP: ");
  Serial.println(WiFi.localIP());
}

static void syncLocalTime() {
  Serial.println("Starting NTP time sync...");
  TimeUtils::getInstance()->begin(TZ_INFO);

  if (TimeUtils::getInstance()->syncTime()) {
    time_t now = TimeUtils::getInstance()->now();
    Serial.printf("Time synced: %s %s\n",
                  TimeUtils::getInstance()->formatDateTime(now).c_str(),
                  TZ_INFO);
  } else {
    Serial.println("Time sync failed or timed out.");
  }
}

// ThingerESP32 thing(USERNAME, DEVICE_ID, DEVICE_CREDENTIAL);

void setup() {
  Serial.begin(115200);
  delay(1000);

  moistureController.begin();
  displayController.begin();

  connectToWiFi();
  syncLocalTime();

  thingerUploader.begin();
}

void loop() {
  moistureController.update();
  displayController.showPercentage(moistureController.getLastMoisturePercent());

  thingerUploader.update(
      moistureController.getLastRawValue(),
      moistureController.getLastMoisturePercent(),
      moistureController.isPumpOn());
}