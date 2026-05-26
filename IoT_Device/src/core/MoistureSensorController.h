#ifndef MOISTURE_SENSOR_CONTROLLER_H
#define MOISTURE_SENSOR_CONTROLLER_H

#include <Arduino.h>

class PumpController;

class MoistureSensorController {
public:
  MoistureSensorController(uint8_t moisturePin,
                           PumpController& pumpController,
                           int rawWet,
                           int rawDry,
                           float thresholdPercent,
                           unsigned long sendIntervalMs = 5000UL);

  void begin();
  void update();

  int readRawValue();
  float readMoisturePercent(int rawValue) const;
  bool isPumpOn() const;
  void setPumpState(bool state);
  void setThresholdPercent(float thresholdPercent);
  void setCalibration(int rawWet, int rawDry);

  int getLastRawValue() const;
  float getLastMoisturePercent() const;

private:
  uint8_t moisturePin;
  PumpController& pumpController;
  int rawWet;
  int rawDry;
  float thresholdPercent;
  unsigned long sendIntervalMs;
  unsigned long lastSendMs;

  int lastRawValue;
  float lastMoisturePercent;

  int clampRawValue(int rawValue) const;
};

#endif // MOISTURE_SENSOR_CONTROLLER_H
