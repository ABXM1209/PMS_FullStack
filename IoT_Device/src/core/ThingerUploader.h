#ifndef THINGER_UPLOADER_H
#define THINGER_UPLOADER_H

#include <Arduino.h>
#include <ThingerESP32.h>

class ThingerUploader {
public:
  ThingerUploader(const char* username,
                  const char* deviceId,
                  const char* deviceCredential,
                  const char* bucketId,
                  unsigned long sendIntervalMs = 10000UL);

  void begin();
  void update(int rawValue, float moisturePercent, bool pumpOn);

private:
  ThingerESP32 thing;
  const char* bucketId;
  unsigned long sendIntervalMs;
  unsigned long lastSendMs;
  int lastRawValue;
  float lastMoisturePercent;
  bool lastPumpOn;
};

#endif // THINGER_UPLOADER_H
