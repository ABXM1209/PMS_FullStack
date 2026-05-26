#include "TimeUtils.h"
#include <sys/time.h>

TimeUtils* TimeUtils::instancePtr = nullptr;

TimeUtils::TimeUtils() : tzInfo(nullptr) {}

TimeUtils* TimeUtils::getInstance() {
  if (instancePtr == nullptr) {
    instancePtr = new TimeUtils();
  }
  return instancePtr;
}

void TimeUtils::begin(const char* tzInfo) {
  if (tzInfo != nullptr) {
    this->tzInfo = tzInfo;
    setenv("TZ", tzInfo, 1);
    tzset();
  }
}

bool TimeUtils::syncTime(const char* server1,
                         const char* server2,
                         unsigned long timeoutMs) {
  if (tzInfo != nullptr) {
    configTzTime(tzInfo, server1, server2);
  } else {
    configTime(0, 0, server1, server2);
  }

  unsigned long start = millis();
  time_t now = time(nullptr);
  while (now < 8 * 3600 && (millis() - start) < timeoutMs) {
    delay(500);
    now = time(nullptr);
  }

  if (tzInfo != nullptr) {
    setenv("TZ", tzInfo, 1);
    tzset();
  }

  return now >= 8 * 3600;
}

time_t TimeUtils::now() const {
  return time(nullptr);
}

String TimeUtils::formatTime(const time_t timestamp) const {
  struct tm timeinfo;
  localtime_r(&timestamp, &timeinfo);
  char buffer[9];
  strftime(buffer, sizeof(buffer), "%H:%M:%S", &timeinfo);
  return String(buffer);
}

String TimeUtils::formatDate(const time_t timestamp) const {
  struct tm timeinfo;
  localtime_r(&timestamp, &timeinfo);
  char buffer[11];
  strftime(buffer, sizeof(buffer), "%Y-%m-%d", &timeinfo);
  return String(buffer);
}

String TimeUtils::formatDateTime(const time_t timestamp) const {
  struct tm timeinfo;
  localtime_r(&timestamp, &timeinfo);
  char buffer[20];
  strftime(buffer, sizeof(buffer), "%Y-%m-%d %H:%M:%S", &timeinfo);
  return String(buffer);
}

bool TimeUtils::isTimeValid() const {
  return now() >= 8 * 3600;
}
