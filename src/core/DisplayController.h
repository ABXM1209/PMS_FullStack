#ifndef DISPLAY_CONTROLLER_H
#define DISPLAY_CONTROLLER_H

#include <Arduino.h>
#include <Wire.h>

class DisplayController {
public:
  DisplayController(uint8_t sdaPin, uint8_t sclPin, uint8_t address = 0x3C);

  void begin();
  void showPercentage(float percent);

private:
  uint8_t sdaPin;
  uint8_t sclPin;
  uint8_t address;
  void writePercent(uint8_t percent) const;
};

#endif // DISPLAY_CONTROLLER_H
