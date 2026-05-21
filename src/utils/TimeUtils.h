#ifndef TIME_UTILS_H
#define TIME_UTILS_H

#include <Arduino.h>
#include <time.h>

class TimeUtils {
public:
  static TimeUtils* getInstance();

  void begin(const char* tzInfo);
  bool syncTime(const char* server1 = "pool.ntp.org",
                const char* server2 = "time.nist.gov",
                unsigned long timeoutMs = 10000UL);

  time_t now() const;
  String formatTime(const time_t timestamp) const;
  String formatDate(const time_t timestamp) const;
  String formatDateTime(const time_t timestamp) const;
  bool isTimeValid() const;

private:
  TimeUtils();
  TimeUtils(const TimeUtils& obj) = delete;
  static TimeUtils* instancePtr;

  const char* tzInfo;
};

#endif // TIME_UTILS_H
