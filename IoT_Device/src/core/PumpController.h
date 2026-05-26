#ifndef PUMP_CONTROLLER_H
#define PUMP_CONTROLLER_H

#include <Arduino.h>

class PumpController {
public:
  explicit PumpController(uint8_t pumpPin);

  void begin();
  void setState(bool on);
  bool isOn() const;

  unsigned long getLastRunMillis() const;
  unsigned long getLastRunDurationMillis() const;
  unsigned long getTotalOnMillis() const;

  float getLastRunHours() const;
  float getLastRunDurationHours() const;
  float getTotalOnHours() const;

  String getLastRunStartTimeString() const;
  String getLastRunDurationString() const;

private:
  uint8_t pumpPin;
  bool pumpOn;
  unsigned long lastRunMillis;
  unsigned long lastRunDurationMillis;
  unsigned long cumulativeOnMillis;
  time_t lastRunStartEpoch;
};

#endif // PUMP_CONTROLLER_H
