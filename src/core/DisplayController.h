#ifndef DISPLAY_CONTROLLER_H
#define DISPLAY_CONTROLLER_H

#include <Arduino.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

class DisplayController {
public:
  DisplayController(uint8_t sdaPin, uint8_t sclPin, uint8_t address = 0x27);

  void begin();
  void showPercentage(float percent);

private:
  uint8_t sdaPin;
  uint8_t sclPin;
  uint8_t address;
  LiquidCrystal_I2C lcd;
};

#endif // DISPLAY_CONTROLLER_H
