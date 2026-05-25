#include "DisplayController.h"

DisplayController::DisplayController(uint8_t sdaPin, uint8_t sclPin, uint8_t address)
    : sdaPin(sdaPin), sclPin(sclPin), address(address), lcd(address, 16, 2) {
}

void DisplayController::begin() {
  Wire.begin(sdaPin, sclPin);
  lcd.init();
  lcd.backlight();
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Moisture: --%");
  lcd.setCursor(0, 1);
  lcd.print("Waiting...");
  Serial.printf("LCD initialized: SDA=%u SCL=%u addr=0x%02X\n", sdaPin, sclPin, address);
}

void DisplayController::showPercentage(float percent) {
  if (percent < 0.0f) {
    percent = 0.0f;
  }
  if (percent > 100.0f) {
    percent = 100.0f;
  }

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Moisture:");
  lcd.setCursor(10, 0);
  lcd.print((int)round(percent));
  lcd.print("%");
  lcd.setCursor(0, 1);
  lcd.print("Watering ");
  lcd.print(percent < 30.0f ? "ON " : "OFF");
}
