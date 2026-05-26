#include "PumpController.h"
#include "utils/TimeUtils.h"

PumpController::PumpController(uint8_t pumpPin)
    : pumpPin(pumpPin),
      pumpOn(false),
      lastRunMillis(0UL),
      lastRunDurationMillis(0UL),
      cumulativeOnMillis(0UL),
      lastRunStartEpoch(0) {}

void PumpController::begin() {
  pinMode(pumpPin, OUTPUT);
  setState(false);
}

void PumpController::setState(bool on) {
  if (on && !pumpOn) {
    lastRunMillis = millis();
    lastRunDurationMillis = 0UL;
    time_t now = TimeUtils::getInstance()->now();
    if (now > 0) {
      lastRunStartEpoch = now;
    }
  }

  if (!on && pumpOn) {
    lastRunDurationMillis = millis() - lastRunMillis;
    cumulativeOnMillis += lastRunDurationMillis;
  }

  pumpOn = on;
  digitalWrite(pumpPin, pumpOn ? HIGH : LOW);
}

bool PumpController::isOn() const {
  return pumpOn;
}

unsigned long PumpController::getLastRunMillis() const {
  return lastRunMillis;
}

unsigned long PumpController::getLastRunDurationMillis() const {
  if (pumpOn) {
    return millis() - lastRunMillis;
  }
  return lastRunDurationMillis;
}

unsigned long PumpController::getTotalOnMillis() const {
  if (pumpOn) {
    return cumulativeOnMillis + (millis() - lastRunMillis);
  }
  return cumulativeOnMillis;
}

float PumpController::getLastRunHours() const {
  return (float)lastRunMillis / 3600000.0f;
}

float PumpController::getLastRunDurationHours() const {
  return (float)getLastRunDurationMillis() / 3600000.0f;
}

float PumpController::getTotalOnHours() const {
  return (float)getTotalOnMillis() / 3600000.0f;
}

String PumpController::getLastRunStartTimeString() const {
  if (lastRunStartEpoch == 0) {
    return String("--:--");
  }

  return TimeUtils::getInstance()->formatTime(lastRunStartEpoch);
}

String PumpController::getLastRunDurationString() const {
  unsigned long duration = getLastRunDurationMillis();
  unsigned long seconds = duration / 1000UL;
  unsigned long hours = seconds / 3600UL;
  unsigned long minutes = (seconds % 3600UL) / 60UL;
  unsigned long secs = seconds % 60UL;

  char buffer[16];
  snprintf(buffer, sizeof(buffer), "%02lu:%02lu:%02lu", hours, minutes, secs);
  return String(buffer);
}
