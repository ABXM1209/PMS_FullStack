#include "MoistureSensorController.h"
#include "PumpController.h"

MoistureSensorController::MoistureSensorController(uint8_t moisturePin,
                                                   PumpController& pumpController,
                                                   int rawWet,
                                                   int rawDry,
                                                   float thresholdPercent,
                                                   unsigned long sendIntervalMs)
    : moisturePin(moisturePin),
      pumpController(pumpController),
      rawWet(rawWet),
      rawDry(rawDry),
      thresholdPercent(thresholdPercent),
      sendIntervalMs(sendIntervalMs),
      lastSendMs(0),
      lastRawValue(0),
      lastMoisturePercent(0.0f) {}

void MoistureSensorController::begin() {
  pinMode(moisturePin, INPUT);
  pumpController.begin();
}

void MoistureSensorController::update() {
  unsigned long now = millis();
  if (now - lastSendMs < sendIntervalMs) {
    return;
  }

  int rawValue = readRawValue();
  float moisturePercent = readMoisturePercent(rawValue);
  lastRawValue = rawValue;
  lastMoisturePercent = moisturePercent;

  Serial.print("Soil moisture raw: ");
  Serial.print(rawValue);
  Serial.print(" | Moisture percent: ");
  Serial.print(moisturePercent, 0);
  Serial.print("% | pump: ");
  Serial.print(pumpController.isOn() ? "ON" : "OFF");
  Serial.print(" | started: ");
  Serial.print(pumpController.getLastRunStartTimeString());
  Serial.print(" | duration: ");
  Serial.print(pumpController.getLastRunDurationString());
  Serial.print(" | total on: ");
  Serial.print(pumpController.getTotalOnMillis());
  Serial.print(" ms");
  Serial.println();

  bool shouldRun = moisturePercent < thresholdPercent;
  if (shouldRun != pumpController.isOn()) {
    setPumpState(shouldRun);
  }

  lastSendMs = now;
}

int MoistureSensorController::readRawValue() {
  return analogRead(moisturePin);
}

int MoistureSensorController::getLastRawValue() const {
  return lastRawValue;
}

float MoistureSensorController::getLastMoisturePercent() const {
  return lastMoisturePercent;
}

float MoistureSensorController::readMoisturePercent(int rawValue) const {
  int constrainedValue = clampRawValue(rawValue);
  int range = rawDry - rawWet;
  if (range <= 0) {
    return 0.0f;
  }

  float percent = 100.0f * (float)(rawDry - constrainedValue) / (float)range;
  return roundf(percent);
}

bool MoistureSensorController::isPumpOn() const {
  return pumpController.isOn();
}

void MoistureSensorController::setPumpState(bool state) {
  pumpController.setState(state);
}

void MoistureSensorController::setThresholdPercent(float thresholdPercent) {
  this->thresholdPercent = thresholdPercent;
}

void MoistureSensorController::setCalibration(int rawWet, int rawDry) {
  this->rawWet = rawWet;
  this->rawDry = rawDry;
}

int MoistureSensorController::clampRawValue(int rawValue) const {
  return constrain(rawValue, rawWet, rawDry);
}
