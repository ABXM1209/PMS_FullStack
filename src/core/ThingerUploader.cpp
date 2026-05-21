#include "ThingerUploader.h"
#include "config.h"

ThingerUploader::ThingerUploader(const char* username,
                                 const char* deviceId,
                                 const char* deviceCredential,
                                 const char* bucketId,
                                 unsigned long sendIntervalMs)
    : thing(username, deviceId, deviceCredential),
      bucketId(bucketId),
      sendIntervalMs(sendIntervalMs),
      lastSendMs(0),
      lastRawValue(0),
      lastMoisturePercent(0.0f),
      lastPumpOn(false) {}

void ThingerUploader::begin() {
//   thing.add_wifi(WIFI_SSID, WIFI_PASSWORD);
  thing["data"] >> [this](pson& out) {
    out["raw"] = lastRawValue;
    out["percent"] = lastMoisturePercent;
    out["pump"] = lastPumpOn ? "ON" : "OFF";
  };
}

void ThingerUploader::update(int rawValue, float moisturePercent, bool pumpOn) {
  lastRawValue = rawValue;
  lastMoisturePercent = moisturePercent;
  lastPumpOn = pumpOn;
  thing.handle();

  unsigned long now = millis();
  if (now - lastSendMs < sendIntervalMs) {
    return;
  }

  Serial.printf("Thinger: WiFi=%d, Connected=%d\n",
                WiFi.status(), thing.is_connected());

  if (!thing.is_connected()) {
    Serial.println("Thinger: not connected, skipping upload.");
    return;
  }

  if (thing.write_bucket(bucketId, "data")) {
    Serial.printf("Thinger: bucket write succeeded (bucket=%s)\n", bucketId);
  } else {
    Serial.printf("Thinger: bucket write failed (bucket=%s)\n", bucketId);
  }

  lastSendMs = now;
}


