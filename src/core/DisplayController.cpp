#include "DisplayController.h"

DisplayController::DisplayController(uint8_t sdaPin, uint8_t sclPin, uint8_t address)
    : sdaPin(sdaPin), sclPin(sclPin), address(address) {
}

void DisplayController::begin() {
  Wire.begin(sdaPin, sclPin);
  Wire.setClock(100000);
  Serial.printf("Display I2C initialized: SDA=%u SCL=%u addr=0x%02X\n", sdaPin, sclPin, address);
}

void DisplayController::showPercentage(float percent) {
  if (percent < 0.0f) {
    percent = 0.0f;
  }
  if (percent > 100.0f) {
    percent = 100.0f;
  }

  writePercent(static_cast<uint8_t>(round(percent)));
}

void DisplayController::writePercent(uint8_t percent) const {
  Wire.beginTransmission(address);
  Wire.write(address); // command/register for display data, adjust for your device if needed
  Wire.write(percent);
  Wire.endTransmission();
}
