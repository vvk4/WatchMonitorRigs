
#include <Arduino.h>
#include "WiFi.h"
#include "EEPROM.h"

#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include <UniversalTelegramBot.h>
#include <ArduinoJson.h>
// #include <esp_task_wdt.h>
#include <ArduinoJson.h>

#include "AsyncTCP.h"
#include "ESPAsyncWebServer.h"
#include "AsyncElegantOTA.h"
#include "esp_wifi.h"

#define GET_MAC_DEF

#define TEST_PIN 23
#define RESET_PIN 26
#define ONOFF_PIN 4
#define Rs_PIN 33
#define LED 2
#define MODE_PIN 27

#define SPIKES_PERIOD 10000
#define FILTER_ARR_SIZE 300

#define LED_1000 1000
#define LED_3000 3000
#define LED_50 50
#define LED_100 100

#define SIZE_StrArray 100

#define HEADER1 0xff  // 0x39
#define HEADER2 0xff  // 0xC3

#if CONFIG_FREERTOS_UNICORE
#define ARDUINO_RUNNING_CORE 0
#else
#define ARDUINO_RUNNING_CORE 1
#endif

#define SIZEOF_RECBUFFER 5000
#define EEPROM_SIZE 1000
#define INLINE_YESNO_TIMEOUT 60000
#define DISCONNECT_CNT 3000

const char PassChanged[] = "The WiFi password/SSID has been changed !!!";
const char KyChanged[] = "The API keys have been changed !!!";
char BOTtoken[60] = "Set bot token here";
char CHAT_ID[20] = "Set chat_id here";
char ssid[40] = "your default ssid";
char password[65] = "your default password";
String Version = "Firmware version: 2.0.0.1";
AsyncWebServer server(80);

char RigNameToWatch[200];
IPAddress SendIP(192, 168, 0, 255);  // default host IP. Last octet has to be 255 (broadcast)
String StrName, myMAC;
String StrArray[SIZE_StrArray];
String keyboardJson;
uint16_t IndexStrArray;
char OrgID[50];
char Key[50];
char Secret[100];

WiFiClientSecure client;
SemaphoreHandle_t xWiFiSemaphore;
SemaphoreHandle_t xSerialMutex;

uint8_t UDPRecBuffer[1000];
uint8_t serialRecBuffer[SIZEOF_RECBUFFER];
uint8_t serialTrmBuffer[250];
uint8_t LEDFlash, byteFromSerial, LEDStateMachine, LEDFlashStateMachine;
uint8_t byteFromSerialPrev, OffStateMachine, LastSemaphore;
uint8_t mac[6], GetStatus, SendCMDToPCStateMachine;
int16_t i, ledCnt, cntLEDFlash, crc_err, Cnt2sec;
uint16_t cntTimeOut, cntRec, TestConnectionCnt, CntConnected, RestartDelay, OffCounter, ResetCounter, TCnt, HCnt, CmdCnt, RestartAttempts;
uint16_t RestartAttemptsCnt, ReconnectedWiFiCnt, WtReplyCnt, Attempts, EmptyStringResponseCnt, CntRstNoConnection;
uint16_t cntEtcPriceFl;
uint32_t RestartDelayL, RestartDelayCnt, interval = 30000, UsedMem, WiFiCntTimeout, RestartNoConnTime, RestartNoConnCnt, MenuResultStateMachineCnt;

hw_timer_t *timer = NULL;
bool gettingPacket, trmConfigMode, Connected, PassChangedToBot, KyChangedToBot, MsgToTelegramm, MsgToTelegrammReady, AddMsgToSend, ScanFl;
bool SendPacketToPC, ReadyToRecMsg = true, TstConn, OffResetFl, Starting = true, WiFiConnected, WriteResetCounter = false, DecIndexStrArray;
bool SendWiFiScanResultFl, SendOptionsFl, RestartingFl, enableOTA = false, WaitForReply, WtReply, ts;
uint8_t CmdToPC, MenuResultStateMachine, RestartingStateMachine;
String StrToTelegramm, MessageToSend, SSIDStr, StrSSID, MACShort = "";
uint32_t spikesOnDuration, spikesOffDuration;

enum LED_STATES {
  LED_NO,
  LED_WiFiCONNECTING,
  LED_WiFiCONNECTED,
  LED_DATA_EXCHANGE,
  LED_WiFi_CONFIG
};
enum UART1_CMD {
  SET_WiFi_NET = 2,
  TEST_CONNECTION,
  GET_WIFI,
  SET_KY,
  GET_KY,
  SET_RESTART_DELAY,
  SET_RESTART_ONOFF,
  TIMER_EXPIRED,
  POWER_KEY_ON,
  POWER_KEY_OFF,
  RESET_KEY_ON,
  RESET_KEY_OFF,
  SET_PC_OFF,
  SET_PC_RESET,
  SET_RIG_NAME,
  STARTING_OFF,
  MESSAGE,
  RESET_RESETCOUNTER,
  SET_TOKEN_CHATID_TELEGRAM,
  GET_TOKEN_CHATID_TELEGRAM,
  CLEAR_FLASH,
  SCAN,
  GET_MAC,
  SET_RESTART_NO_CONN_TIME,
  SET_RESTART_ATTEMPTS,
  CLEAR_ReconnectedWiFiCnt,
  ENABLE_OTA,
  DISABLE_OTA,
  TEST_W,
  WiFi_ON,
  WiFi_OFF,
  SPIKES_ON_DURATION,
  SPIKES_ON,
  TEST_ON_OFF,
  SWITCH_ON_PC_START,
  SPIKES_OFF_DURATION,
  OFF_KEY_COUNTER_MEM,
  DEBUG_INFO,
  ENABLE_TELEGRAM
};

enum UART1_CMD_TO_PC {
  CONNECTION_REPLY = 2,
  WIFI_INFO,
  KY_INFO,
  GET_ALLTEMPERATURE,
  RIG_NAME,
  TOKEN_CHATID_INFO,
  GET_ALLHASHRATES,
  GET_ALLRIGS,
  WIFI_SSIDs,
  MAC_INFO,
  GET_STATUS,
  SEND_OPTIONS_TO_PC,
  STOP_MONITORING,
  START_MONITORING,
  SEND_ETC_PRICE
};

typedef union {
  unsigned long int Flags;
  struct
  {
    unsigned int Fl0 : 1;
    unsigned int Fl1 : 1;
    unsigned int Fl2 : 1;
    unsigned int Fl3 : 1;
    unsigned int Fl4 : 1;
    unsigned int Fl5 : 1;
    unsigned int Fl6 : 1;
    unsigned int Fl7 : 1;
    unsigned int Fl8 : 1;
    unsigned int Fl9 : 1;
    unsigned int Fl10 : 1;
    unsigned int Fl11 : 1;
    unsigned int Fl12 : 1;
    unsigned int Fl13 : 1;
    unsigned int Fl14 : 1;
    unsigned int Fl15 : 1;
    unsigned int Fl16 : 1;
    unsigned int Fl17 : 1;
    unsigned int Fl18 : 1;
    unsigned int Fl19 : 1;
    unsigned int Fl20 : 1;
    unsigned int Fl21 : 1;
    unsigned int Fl22 : 1;
    unsigned int Fl23 : 1;
    unsigned int Fl24 : 1;
    unsigned int Fl25 : 1;
    unsigned int Fl26 : 1;
    unsigned int Fl27 : 1;
    unsigned int Fl28 : 1;
    unsigned int Fl29 : 1;
    unsigned int Fl30 : 1;
    unsigned int Fl31 : 1;
  };
} FLAGS;

FLAGS StatusFlags, flags;

#define ResetRigFl StatusFlags.Fl0
#define PCOffFl StatusFlags.Fl1
#define PCResetFl StatusFlags.Fl2
#define WiFiOn StatusFlags.Fl3
#define spikesFl StatusFlags.Fl4
#define testOnOff StatusFlags.Fl5
#define switchOnPCWhenStart StatusFlags.Fl6
#define debugInfo StatusFlags.Fl7
#define telegramEnabled StatusFlags.Fl8

#define getPriceStr flags.Fl0
#define spikesOff flags.Fl1
#define spikesOn flags.Fl2

int UsedMem_ADDR = 0;
int ssid_ADDR = sizeof(UsedMem) + 4;
int password_ADDR = sizeof(ssid) + 4;
int OrgID_ADDR = password_ADDR + sizeof(password) + 4;
int Key_ADDR = OrgID_ADDR + sizeof(OrgID) + 4;
int Secret_ADDR = Key_ADDR + sizeof(Key) + 4;
int RestartDelay_ADDR = Secret_ADDR + sizeof(Secret) + 4;
int StatusFlags_ADDR = RestartDelay_ADDR + sizeof(RestartDelay) + 4;
int RigNameToWatch_ADDR = StatusFlags_ADDR + sizeof(StatusFlags.Flags) + 4;
int ResetCounter_ADDR = RigNameToWatch_ADDR + sizeof(RigNameToWatch) + 4;
int BOTtoken_ADDR = ResetCounter_ADDR + sizeof(ResetCounter) + 4;
int CHAT_ID_ADDR = BOTtoken_ADDR + sizeof(BOTtoken) + 4;
int RestartNoConnTime_ADDR = CHAT_ID_ADDR + sizeof(CHAT_ID) + 4;
int RestartAttempts_ADDR = RestartNoConnTime_ADDR + sizeof(RestartNoConnTime) + 4;
int ReconnectedWiFiCnt_ADDR = RestartAttempts_ADDR + sizeof(RestartAttempts) + 4;
int spikesOnDuration_ADDR = ReconnectedWiFiCnt_ADDR + sizeof(ReconnectedWiFiCnt) + 4;
int spikesOffDuration_ADDR = spikesOnDuration_ADDR + sizeof(spikesOnDuration) + 4;
int OffCounterMem_ADDR = spikesOffDuration_ADDR + sizeof(spikesOffDuration) + 4;

uint8_t spikeStateMachine;

bool ReadPacket(void);
uint8_t CalcCheckSumm(uint16_t N, uint8_t *Mass);
void WiFiConfigMode(void);
void SerialReceivePacket(void);
void TrmSerial(void);
void TrmInfoToPCSerial(void);
void LEDFunc(void);
void ProcessCMDFromPC(void);
void ReadEEPROM(void);
void TestConnection(void);
void Messaging(void *pvParameters);
void TaskMain(void *pvParameters);
void Scanning(void *pvParameters);
void spikesTask(void *pvParameters);
void Timer(void *pvParameters);
void FirmUpdate(void *pvParameters);
void WatchMain(void);
void SendWiFiInfo(void);
void SendKyInfo(void);
void SendCMDToPC(void);
void SendRigName(void);
void StateLed(void);
void AddMessageToSend(String Str);
void LoadDefaultsSettings(void);
void SendWiFiScanResult(void);
void SendMenu(String txt);
void SendMAC(void);
bool compare(uint8_t mc, uint8_t index);
void SendStatus(void);
void SendOptions(void);
void RestartPC(int attempt);
void MenuYesNo(void);
void SendingPacket(void);
void LEDHigh(void);
void etcPriceFlSending(void);
void onOffSpikes(void);
void RigOff(void);
void RigOn(void);
void ledFlash(uint16_t period, uint8_t cnt);
void esp32WifiModuleClientReconnect(void);
void dbgInfo(void);

__attribute__((section(".noinit"))) volatile uint32_t varNoInit;
bool memReset;
__attribute__((section(".noinit"))) volatile float etcPriceFl, etcPriceFlAv, etcPriceFlArray[FILTER_ARR_SIZE];
__attribute__((section(".noinit"))) volatile bool startFiltering = true;
__attribute__((section(".noinit"))) volatile uint32_t cntSpikesPeriodMem = 3600000;
__attribute__((section(".noinit"))) volatile uint32_t cntSpikesPeriod = 0, cntSpikesPeriodMemCnt = 150000, spikesOffDurationCnt, onOffSpikesStateMachineCnt;
__attribute__((section(".noinit"))) volatile uint16_t onOffSpikesStateMachine = 0, OffCounterMem;

volatile uint32_t sz;

void setup() {
  Serial.begin(460800);
  Serial.println();

  // sz=sizeof(ELEGANT_HTML3);
  sz = sizeof(ELEGANT_HTML);

  if (varNoInit != 0x12345678) {
    varNoInit = 0x12345678;
    memReset = true;
    etcPriceFl = 0;
    etcPriceFlAv = 0;
    for (int i = 0; i < FILTER_ARR_SIZE; i++)
      etcPriceFlArray[i] = 0;
    startFiltering = true;
    cntSpikesPeriodMem = 3600000;
    cntSpikesPeriod = 0;
    cntSpikesPeriodMemCnt = 150000;
    spikesOffDurationCnt = 0;
    onOffSpikesStateMachineCnt = 0;
    onOffSpikesStateMachine = 0, OffCounterMem = 0;
  } else
    memReset = false;

  //---> esp_task_wdt_init(10, true); // enable panic so ESP32 restarts

  // Serial.println("Strt");

  xWiFiSemaphore = xSemaphoreCreateMutex();
  xSemaphoreGive((xWiFiSemaphore));
  xSerialMutex = xSemaphoreCreateMutex();
  xSemaphoreGive((xSerialMutex));

  Serial.println("Starting...");
  Serial.println();

  if (EEPROM.begin(EEPROM_SIZE)) {
    ReadEEPROM();
    Serial.println("EEPROM Ok");
  } else {
    Serial.println("EEPROM failed");
  }

  Serial.print("Starting BOTtoken: ");
  Serial.println(BOTtoken);
  Serial.print("Starting CHAT_ID: ");
  Serial.println(CHAT_ID);

  Serial.print("CHAT_ID_ADDR: ");
  Serial.println(CHAT_ID_ADDR);

  xTaskCreatePinnedToCore(
    Timer, "Timer", 1024  // Stack size
    ,
    NULL, 6  // Priority
    ,
    NULL, ARDUINO_RUNNING_CORE);

  xTaskCreatePinnedToCore(
    TaskMain, "TaskMain", 10024  // Stack size
    ,
    NULL, 5  // Priority
    ,
    NULL, ARDUINO_RUNNING_CORE);

  digitalWrite(Rs_PIN, HIGH);
  pinMode(Rs_PIN, OUTPUT_OPEN_DRAIN);
  pinMode(RESET_PIN, OUTPUT_OPEN_DRAIN);
  pinMode(ONOFF_PIN, OUTPUT);  //_OPEN_DRAIN);
  digitalWrite(RESET_PIN, HIGH);
  digitalWrite(ONOFF_PIN, HIGH);

  pinMode(TEST_PIN, OUTPUT);
  digitalWrite(TEST_PIN, HIGH);

  pinMode(LED, OUTPUT);

  pinMode(MODE_PIN, INPUT_PULLUP);

  ledFlash(200, 5);

  if (!digitalRead(MODE_PIN)) {
    // delay(500);
    //     if (!digitalRead(MODE_PIN))
    //       WiFiConfigMode(); //Config mode
  }

  Serial.println();

  Serial.print("Connecting to ");
  Serial.println(ssid);
  myMAC = WiFi.macAddress();

  for (int i = 0; i < myMAC.length(); i++) {
    if (myMAC[i] != ':')
      MACShort = MACShort + myMAC[i];
  }

  //  Serial.println(MACShort);
  if (WiFiGenericClass::getMode() == WIFI_MODE_NULL) {
    esp_read_mac(mac, ESP_MAC_WIFI_STA);
  } else {
    esp_wifi_get_mac(WIFI_IF_STA, mac);
  }

  if (compare(mac[4], 0) == false) {
    // Serial.println(MACShort);
    delay(1000);
    ESP.restart();
    digitalWrite(Rs_PIN, LOW);  // Rstrt
  }

  // SendMAC();
  StrSSID = ssid;
  if (StrSSID != "Your SSID") {
    // WiFi.begin((const char *)ssid, password);
    esp32WifiModuleClientReconnect();
    client.setCACert(TELEGRAM_CERTIFICATE_ROOT);

    WiFiCntTimeout = 15000;
    while (WiFi.status() != WL_CONNECTED) {

      if (!WiFiCntTimeout) {
        Serial.println("WiFi is not connected. Check your SSID and password");
        break;
      }
      int f = WiFiCntTimeout / 1000;
      Serial.println(f);
      delay(1000);
    }
    if (WiFi.status() == WL_CONNECTED) {
      Serial.println("");
      Serial.println("WiFi connected");
      Serial.println("IP address: ");
      Serial.println(WiFi.localIP());

      SendIP = WiFi.localIP();

      SendIP[3] = 0xff;
    }
  } else
    Serial.println("WiFi is not connected. SSID is not set.");

  RestartAttemptsCnt = RestartAttempts;

  xTaskCreatePinnedToCore(
    Messaging,
    "Messaging"  // A name just for humans
    ,
    20024  // This stack size can be checked & adjusted by reading the Stack Highwater
    ,
    NULL, 1  // Priority, with 3 (configMAX_PRIORITIES - 1) being the highest, and 0 being the lowest.
    ,
    NULL, ARDUINO_RUNNING_CORE);

  xTaskCreatePinnedToCore(
    Scanning, "Scanning", 5024  // Stack size
    ,
    NULL, 1  // Priority
    ,
    NULL, ARDUINO_RUNNING_CORE);

  if (spikesFl) {
    xTaskCreatePinnedToCore(
      spikesTask, "spikesTask", 15024  // Stack size
      ,
      NULL, 4  // Priority
      ,
      NULL, ARDUINO_RUNNING_CORE);
  }
}

void loop() {
}

bool ReadPacket(void) {
  uint16_t NumBytes;
  cntTimeOut = 500;

  if (!gettingPacket) {
    if ((byteFromSerialPrev == HEADER1) && (byteFromSerial == HEADER2)) {
      byteFromSerialPrev = 0;
      gettingPacket = true;
      cntRec = 2;
    } else {
      byteFromSerialPrev = byteFromSerial;
    }
  } else {
    if (cntRec > (SIZEOF_RECBUFFER - 10))
      gettingPacket = false;
    else {
      serialRecBuffer[cntRec] = byteFromSerial;
      cntRec++;
      if (cntRec > 3) {
        NumBytes = serialRecBuffer[2] + ((ushort)serialRecBuffer[3] << 8);
        if (cntRec > (NumBytes + 2)) {
          gettingPacket = false;

          uint8_t check = CalcCheckSumm(NumBytes, &serialRecBuffer[2]);

          if (check != serialRecBuffer[serialRecBuffer[2] + ((uint16_t)serialRecBuffer[3] << 8) + 2]) {
            crc_err++;
            return false;
          } else {
            asm("Nop");
            return true;
          }
        }
      }
    }
  }
  return false;
}

uint8_t CalcCheckSumm(uint16_t N, uint8_t *Mass) {
  uint16_t Summ = 0, j, n = N;

  for (j = 0; j < n; j++)
    Summ = Summ + Mass[j];

  Summ = ~Summ;

  return (uint8_t)Summ;
}

uint16_t ser;

void SerialReceivePacket(void) {
  {
    if (trmConfigMode) {
      trmConfigMode = false;
      TrmInfoToPCSerial();
    }
    ser = Serial.available();
    while (ser > 0) {
      byteFromSerial = Serial.read();
      ser--;
      if (ReadPacket()) {
        LEDFlash = 2;
        ProcessCMDFromPC();
      }
    }
  }
}

void LEDFunc(void) {
  switch (LEDStateMachine) {
    case LED_NO:
      digitalWrite(LED, LOW);
      break;
    case LED_WiFiCONNECTING:
      {
        if (ledCnt < LED_1000)
          ledCnt++;
        else {
          ledCnt = 0;
          if (digitalRead(LED))
            digitalWrite(LED, LOW);
          else
            LEDHigh();
        }
      }
      break;
    case LED_WiFiCONNECTED:
      if (ledCnt) {
        ledCnt--;
        if (!ledCnt) {
          if (digitalRead(LED)) {
            ledCnt = LED_3000;
            digitalWrite(LED, LOW);
          } else {
            LEDHigh();
            ledCnt = LED_50;
          }
        }
      } else
        ledCnt = 1;

      break;
    case LED_DATA_EXCHANGE:

      switch (LEDFlashStateMachine) {
        case 0:
          if (LEDFlash) {
            cntLEDFlash = 3000;
            LEDFlashStateMachine = 1;
            digitalWrite(LED, LOW);
            ledCnt = LED_50;
          }
          break;
        case 1:
          ledCnt--;
          if (!ledCnt) {
            LEDFlashStateMachine = 2;
            ledCnt = LED_50;
            LEDHigh();
          }
          break;
        case 2:
          ledCnt--;
          if (!ledCnt) {
            LEDFlashStateMachine = 3;
            ledCnt = LED_50;
            digitalWrite(LED, LOW);
          }
          break;
        case 3:
          ledCnt--;
          if (!ledCnt) {
            LEDFlashStateMachine = 0;
            ledCnt = LED_100;
            LEDFlash--;
          }
          break;
      }
      break;
    case LED_WiFi_CONFIG:
      LEDHigh();
      break;
    default:
      LEDStateMachine = LED_NO;
      break;
  }

  if (cntLEDFlash) {
    cntLEDFlash--;
    if (!cntLEDFlash)
      LEDStateMachine = LED_WiFiCONNECTED;
  }
}

void LEDHigh(void) {
  digitalWrite(LED, HIGH);
  //--->esp_task_wdt_reset();
}

void TrmInfoToPCSerial(void) {
  uint16_t cntBytes = 4, i;
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[3] = 21;

  serialTrmBuffer[cntBytes++] = (uint8_t)sizeof(ssid);
  for (i = 0; i < sizeof(ssid); i++)
    serialTrmBuffer[cntBytes++] = ssid[i];

  serialTrmBuffer[cntBytes++] = (uint8_t)sizeof(password);
  for (i = 0; i < sizeof(password); i++)
    serialTrmBuffer[cntBytes++] = password[i];

  serialTrmBuffer[2] = cntBytes - 3;

  serialTrmBuffer[cntBytes] = CalcCheckSumm(serialTrmBuffer[2] + 1, &serialTrmBuffer[2]);
  TrmSerial();
}

void TrmSerial(void) {
  uint16_t i;
  for (i = 0; i < (serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8) + 3); i++)
    Serial.write(serialTrmBuffer[i]);
}

void ProcessCMDFromPC(void) {
  uint8_t IPbyte1, IPbyte2, IPbyte3, IPbyte4, i;

  if (!IndexStrArray)
    MsgToTelegrammReady = true;

  switch (serialRecBuffer[4]) {
    case SET_WiFi_NET:
      Serial.println();
      Serial.println("The WiFi password or SSID has been changed  !!!");

      PassChangedToBot = true;

      for (i = 0; i < serialRecBuffer[5]; i++)
        ssid[i] = serialRecBuffer[i + 6];
      ssid[i] = 0;

      for (i = 0; i < serialRecBuffer[6 + serialRecBuffer[5]]; i++)
        password[i] = serialRecBuffer[i + 7 + serialRecBuffer[5]];
      password[i] = 0;

      EEPROM.writeString(ssid_ADDR, ssid);
      EEPROM.writeString(password_ADDR, password);

      EEPROM.commit();

      ReadEEPROM();

      ESP.restart();
      digitalWrite(Rs_PIN, LOW);  // Rstrt

      break;
    case TEST_CONNECTION:
      //  if (!Connected)
      //    ledFlash(1000,1);
      Connected = true;
      CntConnected = DISCONNECT_CNT;
      RestartDelayCnt = 0;
      TestConnection();
      break;
    case GET_WIFI:
      SendWiFiInfo();
      break;
    case SET_KY:
      Serial.println();
      Serial.println("The api keys have been changed  !!!");

      KyChangedToBot = true;

      for (i = 0; i < serialRecBuffer[5]; i++)
        OrgID[i] = serialRecBuffer[i + 6];
      OrgID[i] = 0;

      for (i = 0; i < serialRecBuffer[6 + serialRecBuffer[5]]; i++)
        Key[i] = serialRecBuffer[i + 7 + serialRecBuffer[5]];
      Key[i] = 0;

      for (i = 0; i < serialRecBuffer[7 + serialRecBuffer[5] + serialRecBuffer[6 + serialRecBuffer[5]]]; i++)
        Secret[i] = serialRecBuffer[i + 8 + serialRecBuffer[5] + serialRecBuffer[6 + serialRecBuffer[5]]];
      Secret[i] = 0;

      EEPROM.writeString(OrgID_ADDR, OrgID);
      EEPROM.writeString(Key_ADDR, Key);
      EEPROM.writeString(Secret_ADDR, Secret);

      EEPROM.commit();

      ReadEEPROM();

      SendKyInfo();

      break;
    case GET_KY:
      // ledFlash(500,1);
      // delay (500);
      SendKyInfo();
      SendOptionsFl = true;
      break;
    case SET_RESTART_DELAY:
      RestartDelay = serialRecBuffer[6];
      RestartDelay = RestartDelay << 8;
      RestartDelay = RestartDelay + serialRecBuffer[5];

      EEPROM.writeUInt(RestartDelay_ADDR, RestartDelay);
      EEPROM.commit();
      break;
    case SET_RESTART_ONOFF:
      if (serialRecBuffer[5])
        ResetRigFl = 1;
      else
        ResetRigFl = 0;

      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;
    case SET_PC_OFF:
      PCOffFl = 1;
      PCResetFl = 0;
      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;
    case SET_PC_RESET:
      PCOffFl = 0;
      PCResetFl = 1;
      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;
    case SET_RIG_NAME:
      for (i = 0; i < serialRecBuffer[5]; i++) {
        RigNameToWatch[i] = (char)serialRecBuffer[i + 6];
      }
      RigNameToWatch[i] = 0;
      EEPROM.writeString(RigNameToWatch_ADDR, RigNameToWatch);
      EEPROM.commit();
      ReadEEPROM();

      SendRigName();

      SendOptionsFl = true;
      break;

    case TIMER_EXPIRED:
      {
        // ledFlash(1000,3);

        StrToTelegramm = "";
        int i = 5;
        do {
          StrToTelegramm = StrToTelegramm + (char)serialRecBuffer[i++];
        } while (serialRecBuffer[i] != 0);
        //  i=StrToTelegramm.length();
        //      Serial.println(i);
        if (i > 5) {
          MsgToTelegramm = true;
          AddMessageToSend(StrToTelegramm);

          Serial.println("RESET RECEIVED!!!   RESET RECEIVED!!!   RESET RECEIVED!!!   ");

          if (ResetRigFl) {
            MsgToTelegrammReady = true;  // false;
            OffResetFl = true;
            Starting = true;
          }
        }
        ResetCounter++;
        WriteResetCounter = true;
      }
      break;

    case MESSAGE:
      {
        // ledFlash(1000,4);

        StrToTelegramm = "";
        int i = 5;
        do {
          StrToTelegramm = StrToTelegramm + (char)serialRecBuffer[i++];
        } while (serialRecBuffer[i] != 0);
        if (i > 5) {
          WtReply = false;
          MsgToTelegramm = true;
          AddMessageToSend(StrToTelegramm);
        }
      }
      break;
    case POWER_KEY_ON:
      digitalWrite(ONOFF_PIN, LOW);
      ResetRigFl = 1;
      PCOffFl = 1;
      PCResetFl = 0;
      Connected = true;
      CntConnected = DISCONNECT_CNT;
      if (spikesFl)
        OffCounter = OffCounterMem;
      else
        OffCounter = 7000;
      OffStateMachine = 1;
      TestConnection();
      SendKyInfo();
      break;
    case POWER_KEY_OFF:
      digitalWrite(ONOFF_PIN, HIGH);
      Connected = true;
      OffStateMachine = 0;
      TestConnection();
      break;
    case RESET_KEY_ON:
      digitalWrite(RESET_PIN, LOW);
      OffCounter = 2000;
      Connected = true;
      CntConnected = 2000;
      ResetRigFl = 1;
      PCOffFl = 0;
      PCResetFl = 1;
      TestConnection();
      SendKyInfo();
      break;
    case RESET_KEY_OFF:
      digitalWrite(RESET_PIN, HIGH);
      Connected = true;
      TestConnection();
      break;

    case STARTING_OFF:
      // ledFlash(1000,5);
      Starting = false;
      break;

    case RESET_RESETCOUNTER:
      ResetCounter = 0;
      WriteResetCounter = true;
      break;

    case SET_TOKEN_CHATID_TELEGRAM:
      Serial.println();
      Serial.println("The BOT token and/or chat_id has been changed  !!!");

      PassChangedToBot = true;

      for (i = 0; i < serialRecBuffer[5]; i++)
        BOTtoken[i] = serialRecBuffer[i + 6];
      BOTtoken[i] = 0;

      for (i = 0; i < serialRecBuffer[6 + serialRecBuffer[5]]; i++)
        CHAT_ID[i] = serialRecBuffer[i + 7 + serialRecBuffer[5]];
      CHAT_ID[i] = 0;

      EEPROM.writeString(BOTtoken_ADDR, BOTtoken);
      EEPROM.writeString(CHAT_ID_ADDR, CHAT_ID);

      EEPROM.commit();

      ReadEEPROM();

      Serial.print("Starting BOTtoken: ");
      Serial.println(BOTtoken);
      Serial.print("Starting CHAT_ID: ");
      Serial.println(CHAT_ID);

      Serial.println("Restarting ESP...");

      // ESP.restart();
      digitalWrite(Rs_PIN, LOW);  // Rstrt

      break;

    case GET_TOKEN_CHATID_TELEGRAM:
      {
        int Cnt = 5;
        serialTrmBuffer[0] = HEADER1;
        serialTrmBuffer[1] = HEADER2;

        serialTrmBuffer[4] = TOKEN_CHATID_INFO;

        serialTrmBuffer[Cnt++] = (uint8_t)sizeof(BOTtoken);
        for (int i = 0; i < sizeof(BOTtoken); i++) {
          serialTrmBuffer[Cnt++] = BOTtoken[i];
        }

        serialTrmBuffer[Cnt++] = (uint8_t)sizeof(CHAT_ID);
        for (int i = 0; i < sizeof(CHAT_ID); i++) {
          serialTrmBuffer[Cnt++] = CHAT_ID[i];
        }

        serialTrmBuffer[2] = Cnt - 2;
        serialTrmBuffer[3] = 0;

        serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
        TrmSerial();
      }
      break;
    case CLEAR_FLASH:
      LoadDefaultsSettings();
      Serial.println("User datas are cleared.");
      Serial.println("Restarting...");
      ESP.restart();
      digitalWrite(Rs_PIN, LOW);  // Rstrt
      break;
    case SCAN:
      ScanFl = true;
      break;
    case GET_MAC:
      SendMAC();
      break;
    case SET_RESTART_NO_CONN_TIME:
      RestartNoConnTime = ((uint32_t)serialRecBuffer[8]) << 24;
      RestartNoConnTime = RestartNoConnTime + (((uint32_t)serialRecBuffer[7]) << 16);
      RestartNoConnTime = RestartNoConnTime + (((uint32_t)serialRecBuffer[6]) << 8);
      RestartNoConnTime = RestartNoConnTime + serialRecBuffer[5];
      EEPROM.writeULong(RestartNoConnTime_ADDR, RestartNoConnTime);
      EEPROM.commit();
      ReadEEPROM();
      SendOptionsFl = true;
      break;
    case SPIKES_ON_DURATION:
      spikesOnDuration = ((uint32_t)serialRecBuffer[8]) << 24;
      spikesOnDuration = spikesOnDuration + (((uint32_t)serialRecBuffer[7]) << 16);
      spikesOnDuration = spikesOnDuration + (((uint32_t)serialRecBuffer[6]) << 8);
      spikesOnDuration = spikesOnDuration + serialRecBuffer[5];
      EEPROM.writeULong(spikesOnDuration_ADDR, spikesOnDuration);
      EEPROM.commit();
      ReadEEPROM();
      SendOptionsFl = true;
      break;
    case SPIKES_OFF_DURATION:
      spikesOffDuration = ((uint32_t)serialRecBuffer[8]) << 24;
      spikesOffDuration = spikesOffDuration + (((uint32_t)serialRecBuffer[7]) << 16);
      spikesOffDuration = spikesOffDuration + (((uint32_t)serialRecBuffer[6]) << 8);
      spikesOffDuration = spikesOffDuration + serialRecBuffer[5];
      EEPROM.writeULong(spikesOffDuration_ADDR, spikesOffDuration);
      EEPROM.commit();
      ReadEEPROM();
      SendOptionsFl = true;
      break;
    case OFF_KEY_COUNTER_MEM:
      OffCounterMem = ((uint16_t)serialRecBuffer[6]) << 8;
      OffCounterMem = OffCounterMem + (uint16_t)serialRecBuffer[5];
      EEPROM.writeShort(OffCounterMem_ADDR, OffCounterMem);
      EEPROM.commit();
      ReadEEPROM();
      SendOptionsFl = true;
      break;
    case SPIKES_ON:
      if (serialRecBuffer[5]) {
        ResetRigFl = 0;
        spikesFl = 1;
      } else
        spikesFl = 0;

      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      delay(1000);
      ESP.restart();
      break;

    case TEST_ON_OFF:
      if (serialRecBuffer[5])
        testOnOff = 1;
      else
        testOnOff = 0;

      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;
    case SWITCH_ON_PC_START:
      if (serialRecBuffer[5])
        switchOnPCWhenStart = 1;
      else
        switchOnPCWhenStart = 0;

      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;

    case DEBUG_INFO:
      if (serialRecBuffer[5])
        debugInfo = 1;
      else
        debugInfo = 0;
      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;
    case ENABLE_TELEGRAM:
      if (serialRecBuffer[5])
        telegramEnabled = 1;
      else
        telegramEnabled = 0;
      EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
      EEPROM.commit();
      ReadEEPROM();
      SendKyInfo();
      break;

    case SET_RESTART_ATTEMPTS:
      RestartAttempts = serialRecBuffer[6];
      RestartAttempts = RestartAttempts << 8;
      RestartAttempts = RestartAttempts + serialRecBuffer[5];
      EEPROM.writeShort(RestartAttempts_ADDR, RestartAttempts);
      EEPROM.commit();
      ReadEEPROM();
      SendOptionsFl = true;
      break;
    case CLEAR_ReconnectedWiFiCnt:
      ReconnectedWiFiCnt = 0;
      EEPROM.writeShort(ReconnectedWiFiCnt_ADDR, ReconnectedWiFiCnt);
      EEPROM.commit();
      ReadEEPROM();
      break;
    case ENABLE_OTA:
      if (!enableOTA) {
        enableOTA = true;
        xTaskCreatePinnedToCore(
          FirmUpdate, "FirmUpdate", 5024  // Stack size
          ,
          NULL, 1  // Priority
          ,
          NULL, ARDUINO_RUNNING_CORE);
      }
      break;
    case DISABLE_OTA:
      // ESP.restart();
      digitalWrite(Rs_PIN, LOW);  // Rstrt
      break;
    case TEST_W:
      // ledFlash(1000,6);
      ts = true;
      break;

    default:
      break;
  }
}

void SendRigName(void) {
  int i, Cnt = 5;

  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = RIG_NAME;

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(RigNameToWatch);
  for (i = 0; i < sizeof(RigNameToWatch); i++) {
    serialTrmBuffer[Cnt++] = RigNameToWatch[i];
  }

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void SendKyInfo(void) {
  int i, Cnt = 5;

  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = KY_INFO;

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(OrgID);
  for (i = 0; i < sizeof(OrgID); i++) {
    serialTrmBuffer[Cnt++] = OrgID[i];
  }

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(Key);
  for (i = 0; i < sizeof(Key); i++) {
    serialTrmBuffer[Cnt++] = Key[i];
  }

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(Secret);
  for (i = 0; i < sizeof(Secret); i++) {
    serialTrmBuffer[Cnt++] = Secret[i];
  }
  serialTrmBuffer[Cnt++] = (uint8_t)RestartDelay;
  serialTrmBuffer[Cnt++] = (uint8_t)(RestartDelay >> 8);

  serialTrmBuffer[Cnt++] = (uint8_t)StatusFlags.Flags;
  serialTrmBuffer[Cnt++] = (uint8_t)(StatusFlags.Flags >> 8);
  serialTrmBuffer[Cnt++] = (uint8_t)(StatusFlags.Flags >> 16);
  serialTrmBuffer[Cnt++] = (uint8_t)(StatusFlags.Flags >> 24);

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void SendWiFiInfo(void) {
  int i, Cnt = 5;

  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = WIFI_INFO;

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(ssid);
  for (i = 0; i < sizeof(ssid); i++) {
    serialTrmBuffer[Cnt++] = ssid[i];
  }

  serialTrmBuffer[Cnt++] = (uint8_t)sizeof(password);
  for (i = 0; i < sizeof(password); i++) {
    serialTrmBuffer[Cnt++] = password[i];
  }

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void TestConnection(void) {
  uint16_t Cnt;
  IPAddress IP = WiFi.localIP();
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;
  serialTrmBuffer[2] = 17;
  serialTrmBuffer[3] = 0;
  serialTrmBuffer[4] = CONNECTION_REPLY;
  serialTrmBuffer[5] = IP[0];
  serialTrmBuffer[6] = IP[1];
  serialTrmBuffer[7] = IP[2];
  serialTrmBuffer[8] = IP[3];
  if (ReadyToRecMsg)
    serialTrmBuffer[9] = 1;
  else
    serialTrmBuffer[9] = 0;

  if (!digitalRead(RESET_PIN))
    serialTrmBuffer[10] = 1;
  else
    serialTrmBuffer[10] = 0;

  if (!digitalRead(ONOFF_PIN))
    serialTrmBuffer[10] = serialTrmBuffer[10] | 2;
  else
    serialTrmBuffer[10] &= ~2;

  serialTrmBuffer[11] = ResetCounter;
  serialTrmBuffer[12] = ResetCounter >> 8;

  serialTrmBuffer[13] = ReconnectedWiFiCnt;
  serialTrmBuffer[14] = ReconnectedWiFiCnt >> 8;

  serialTrmBuffer[15] = crc_err;
  serialTrmBuffer[16] = crc_err >> 8;

  if (enableOTA)
    serialTrmBuffer[17] = 1;
  else
    serialTrmBuffer[17] = 0;

  if (WiFi.status() == WL_CONNECTED)
    serialTrmBuffer[18] = 1;
  else
    serialTrmBuffer[18] = 0;

  Cnt = 19;

  *(uint32_t *)&serialTrmBuffer[Cnt] = *(uint32_t *)&etcPriceFl;
  Cnt = Cnt + 3;

  serialTrmBuffer[2] = Cnt - 1;

  serialTrmBuffer[Cnt + 1] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void ReadEEPROM(void) {
  int i;
  String str;

  UsedMem = EEPROM.readLong(UsedMem_ADDR);
  if (UsedMem != 0x12345678)
    LoadDefaultsSettings();

  str = EEPROM.readString(ssid_ADDR);

  for (i = 0; i < sizeof(ssid); i++)
    ssid[i] = 0;
  for (i = 0; i < sizeof(password); i++)
    password[i] = 0;
  SendIP[0] = SendIP[1] = SendIP[2] = SendIP[3] = 0;

  for (i = 0; i < str.length(); i++)
    ssid[i] = str[i];
  ssid[i] = 0;

  str = EEPROM.readString(password_ADDR);

  for (i = 0; i < str.length(); i++)
    password[i] = str[i];
  password[i] = 0;

  str = EEPROM.readString(OrgID_ADDR);

  for (i = 0; i < str.length(); i++)
    OrgID[i] = str[i];
  OrgID[i] = 0;

  str = EEPROM.readString(Key_ADDR);

  for (i = 0; i < str.length(); i++)
    Key[i] = str[i];
  Key[i] = 0;

  str = EEPROM.readString(Secret_ADDR);

  for (i = 0; i < str.length(); i++)
    Secret[i] = str[i];
  Secret[i] = 0;

  RestartDelay = EEPROM.readUInt(RestartDelay_ADDR);
  RestartDelayL = RestartDelay * 1000;
  StatusFlags.Flags = EEPROM.readLong(StatusFlags_ADDR);

  str = EEPROM.readString(RigNameToWatch_ADDR);
  for (i = 0; i < str.length(); i++)
    RigNameToWatch[i] = str[i];
  RigNameToWatch[i] = 0;

  StrName = str;

  ResetCounter = EEPROM.readUInt(ResetCounter_ADDR);

  str = EEPROM.readString(BOTtoken_ADDR);

  for (i = 0; i < str.length(); i++)
    BOTtoken[i] = str[i];
  BOTtoken[i] = 0;

  str = EEPROM.readString(CHAT_ID_ADDR);

  for (i = 0; i < str.length(); i++)
    CHAT_ID[i] = str[i];
  CHAT_ID[i] = 0;

  RestartNoConnTime = EEPROM.readULong(RestartNoConnTime_ADDR);
  RestartAttempts = EEPROM.readShort(RestartAttempts_ADDR);

  ReconnectedWiFiCnt = EEPROM.readShort(ReconnectedWiFiCnt_ADDR);

  spikesOnDuration = EEPROM.readULong(spikesOnDuration_ADDR);
  spikesOffDuration = EEPROM.readULong(spikesOffDuration_ADDR);

  OffCounterMem = EEPROM.readShort(OffCounterMem_ADDR);
}

void WatchMain(void) {

  if (!Connected) {
    if (!TestConnectionCnt) {
      TestConnection();
      // digitalWrite(TEST_PIN, !digitalRead(TEST_PIN));

      TestConnectionCnt = 100;
    }
  }

  if (TstConn) {
    TstConn = false;
    TestConnection();
  }
  if (DecIndexStrArray) {
    DecIndexStrArray = false;
    for (int i = 0; i < (IndexStrArray - 1); i++) {
      StrArray[i] = StrArray[i + 1];
    }
    if (IndexStrArray)
      IndexStrArray--;
    Serial.print("IndexStrArray dec: ");
    Serial.println(IndexStrArray);
  }

  SerialReceivePacket();
}

void Messaging(void *pvParameters)  // This is a task.
{
  (void)pvParameters;
  bool sta = false;

  UniversalTelegramBot bot(BOTtoken, client);

  if (telegramEnabled)
    Serial.println("Sending message to telegramm bot...");
  if (memReset)
    MessageToSend = StrName + "/ Monitor started up, memReset=true";
  else
    MessageToSend = StrName + "/ Monitor started up, memReset=false";

  AddMsgToSend = true;

  WiFiConnected = true;
  WiFiCntTimeout = 15000;
  if (compare(mac[5], 1) == false) {
    // Serial.println(MACShort);
    delay(1000);
    ESP.restart();
    digitalWrite(Rs_PIN, LOW);  // Rstrt
  }
  // bot.longPoll = 1;
  for (;;)  // A Task shall never return or exit.
  {
    if (((WiFi.status() != WL_CONNECTED) && (!WiFiCntTimeout) && !ScanFl) && (StrSSID != "Your SSID")) {
      if (xSemaphoreTake(xWiFiSemaphore, (TickType_t)5) == pdTRUE) {
        LastSemaphore = 1;

        WiFiConnected = false;

        LEDStateMachine = LED_WiFiCONNECTING;
        Serial.println("");
        Serial.print("WiFi disconnected, reconnecting to ");
        Serial.println(ssid);
        WiFi.disconnect();

        //    WiFi.reconnect();
        // WiFi.begin((const char *)ssid, password);
        esp32WifiModuleClientReconnect();
        client.setCACert(TELEGRAM_CERTIFICATE_ROOT);

        WiFiCntTimeout = 15000;

        while (WiFi.status() != WL_CONNECTED) {
          if (!WiFiCntTimeout) {
            Serial.println("WiFi is not connected. Check your SSID or password");
            break;
          }
          int f = WiFiCntTimeout / 1000;
          Serial.println(f);
          delay(1000);
        }
        if (WiFi.status() == WL_CONNECTED) {
          Serial.println("");
          Serial.println("WiFi connected");
          Serial.println("IP address: ");
          Serial.println(WiFi.localIP());

          SendIP = WiFi.localIP();
          SendIP[3] = 0xff;
          ReconnectedWiFiCnt++;
          EEPROM.writeShort(ReconnectedWiFiCnt_ADDR, ReconnectedWiFiCnt);
          EEPROM.commit();
          ReadEEPROM();
          CntRstNoConnection = 0;
        } else {
          if (Connected) {
            CntRstNoConnection++;
            if (CntRstNoConnection > 2) {
              ReconnectedWiFiCnt++;
              //        Serial.println("Writing ReconnectedWiFiCnt to EEPROM");
              //          EEPROM.writeShort(ReconnectedWiFiCnt_ADDR, ReconnectedWiFiCnt);
              //            EEPROM.commit();
              Serial.println("Restarting ESP...");
              delay(1000);
              digitalWrite(Rs_PIN, LOW);  // Rstrt
              while (1)
                ;
              // NoResetWDT = true;
              // ESP.restart();
            }
          }
        }

        xSemaphoreGive(xWiFiSemaphore);
        WiFiCntTimeout = 15000;
      } else if (debugInfo)
        Serial.println("Mutex busy (Messaging: no WiFi) LS=" + String(LastSemaphore));
    }

    if (telegramEnabled) {
      if (IndexStrArray) {
        if (xSemaphoreTake(xWiFiSemaphore, (TickType_t)5) == pdTRUE) {
          LastSemaphore = 2;

          ReadyToRecMsg = false;
          MsgToTelegramm = false;
          Serial.print("Sending message to telegramm...  (");
          Serial.print(StrArray[0].length());
          Serial.println("bytes)");

          Serial.println(CHAT_ID);

          String StrTmp1 = CHAT_ID, StrTmp2 = BOTtoken;

          if ((StrTmp2 != "Your bot token") && (StrTmp1 != "-100000000") && (WiFi.status() == WL_CONNECTED)) {
            uint8_t RptCounter = 2;
            do {
              sta = bot.sendMessage(CHAT_ID, StrArray[0], "");
              if (sta) {
                Serial.print("Sent message to telegramm bot  (");
                Serial.print(StrArray[0].length());
                Serial.println("bytes)");
              } else {
                Serial.println("API error, repeat sending...");
                RptCounter--;
              }
            } while ((!sta) && (RptCounter));
          } else
            Serial.println("No BOTtoken and CHAT_ID are set or WiFi is not connected.");

          DecIndexStrArray = true;
          xSemaphoreGive(xWiFiSemaphore);
        } else if (debugInfo)
          Serial.println("Mutex busy (sending) LS=" + String(LastSemaphore));
      }
    }

    if (PassChangedToBot) {
      PassChangedToBot = false;
      //      bot.sendMessage(CHAT_ID, PassChanged, "");
      MessageToSend = PassChanged;
      AddMsgToSend = true;
    }

    if (KyChangedToBot) {
      KyChangedToBot = false;
      //      bot.sendMessage(CHAT_ID, KyChanged, "");
      MessageToSend = KyChanged;
      AddMsgToSend = true;
    }

    int numNewMessages = 0;
    if (telegramEnabled) {
      if (WiFi.status() == WL_CONNECTED) {
        if (xSemaphoreTake(xWiFiSemaphore, (TickType_t)5) == pdTRUE) {
          LastSemaphore = 3;
          uint8_t Rs;
          Serial.println("\r\nGetting telegram updates...");
          numNewMessages = bot.getUpdates(bot.last_message_received + 1);  //, &Rs);
          xSemaphoreGive(xWiFiSemaphore);
          if (!Rs) {
            EmptyStringResponseCnt++;
            if (debugInfo) {
              Serial.print("EmptyStringResponseCnt: ");
              Serial.println(EmptyStringResponseCnt);
            }
            if ((EmptyStringResponseCnt > 4) && (Connected))
              // ESP.restart();
              digitalWrite(Rs_PIN, LOW);  // Rstrt
          } else {
            EmptyStringResponseCnt = 0;
          }
        } else if (debugInfo)
          Serial.println("\r\nMutex busy (get updates) LS=" + String(LastSemaphore));
      } else
        Serial.println("\r\nWiFi is not WL_CONNECTED (updating)");

      //    Serial.print("bot.last_message_received: ");
      //  Serial.println(bot.last_message_received);
      // Serial.print("numNewMessages: ");
      // Serial.println(numNewMessages);

      for (int i = 0; i < numNewMessages; i++)
        Serial.println("CHAT_ID: " + String(bot.messages[i].chat_id));
    }

    if (numNewMessages) {
      CmdCnt++;
      Serial.print("CmdCnt: ");
      Serial.println(CmdCnt);

      for (int i = 0; i < numNewMessages; i++) {
        String chat_id = String(bot.messages[i].chat_id);
        Serial.println();
        Serial.println(chat_id);
        if (chat_id == CHAT_ID) {
          String text = bot.messages[i].text;

          Serial.println(text);
          String from_name = bot.messages[i].from_name;
          Serial.println(from_name);
          text.replace(" ", "");

          if ((text == ("T" + StrName)) || (text == "T")) {
            TCnt++;
            Serial.print("TCnt: ");
            Serial.println(TCnt);
            CmdToPC = GET_ALLTEMPERATURE;
            WaitForReply = true;
            SendPacketToPC = true;
          } else if ((text == ("H" + StrName)) || (text == "H")) {
            HCnt++;
            Serial.print("HCnt: ");
            Serial.println(HCnt);
            CmdToPC = GET_ALLHASHRATES;
            WaitForReply = true;
            SendPacketToPC = true;
          } else if ((text == ("Rigs" + StrName)) || (text == "Rigs")) {
            Serial.println("Rigs");
            CmdToPC = GET_ALLRIGS;
            WaitForReply = true;
            SendPacketToPC = true;
          } else if ((text == ("Rcnt" + StrName)) || (text == "Rcnt")) {
            Serial.println("Rcnt");
            MessageToSend = StrName + "/ Restart counter: " + ResetCounter;
            AddMsgToSend = true;
          } else if ((text == ("RstRcnt" + StrName)) || (text == "RstRcnt")) {
            ResetCounter = 0;
            Serial.println("RstRcnt");
            WriteResetCounter = true;
            MessageToSend = StrName + "/ Restart counter: " + ResetCounter;
            AddMsgToSend = true;
          } else if (text == ("M" + StrName) || (text == ("M")) || (text == ("Menu"))) {
            Serial.println("Sending menu:       " + text);

            String keyboardJson = "[[{ \"text\" : \"GPU temperatures (T)\", \"callback_data\" : \"T" + StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Hashrates (H)\", \"callback_data\" : \"H" + StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"All rigs info (Rigs)\", \"callback_data\" : \"Rigs" + StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Status (S)\", \"callback_data\" : \"S" + "\" }]";                             //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Restart counter (Rcnt)\", \"callback_data\" : \"Rcnt" + "\" }]";              //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Clear restart counter (RstRcnt)\", \"callback_data\" : \"RstRcnt" + "\" }]";  //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Restart Rig (RstrtRig)\", \"callback_data\" : \"RstrtRig" + "\" }]";          //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Switch OFF Rig (OffRig)\", \"callback_data\" : \"OffRig" + "\" }]";           //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Switch ON Rig (OnRig)\", \"callback_data\" : \"OnRig" + "\" }]";              //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Stop monitoring (Stp)\", \"callback_data\" : \"Stp" + "\" }]";                //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Start monitoring (Strt)\", \"callback_data\" : \"Strt" + "\" }]";             //+ StrName + "\" }]";
                                                                                                                                          //            keyboardJson = keyboardJson + ",[{ \"text\" : \"Stop spikes mining (SpikesOff)\", \"callback_data\" : \"SpikesOff" + "\" }]"; //+ StrName + "\" }]";
                                                                                                                                          //            keyboardJson = keyboardJson + ",[{ \"text\" : \"Start spikes mining (SpikesOn)\", \"callback_data\" : \"SpikesOn" + "\" }]";  //+ StrName + "\" }]";
                                                                                                                                          //            keyboardJson = keyboardJson + ",[{ \"text\" : \"TTTTTTTTT\", \"callback_data\" : \"SpikesOn" + "\" }]";  //+ StrName + "\" }]";
            keyboardJson = keyboardJson + ",[{ \"text\" : \"Get link to update firmware (IP)\", \"callback_data\" : \"IP" + "\" }]]";     //+ StrName + "\" }]]";

            //            Serial.println(keyboardJson);

            bot.sendMessageWithInlineKeyboard(chat_id, StrName + "/  Choose one of the following options:", "", keyboardJson);
          } else if ((text == ("Stp" + StrName)) || (text == "Stp")) {
            Serial.print("Stp: ");
            CmdToPC = STOP_MONITORING;
            WaitForReply = false;
            SendPacketToPC = true;
          } else if ((text == ("Strt" + StrName)) || (text == "Strt")) {
            Serial.print("Strt: ");
            CmdToPC = START_MONITORING;
            WaitForReply = false;
            SendPacketToPC = true;
          } else if ((text == ("Y" + StrName)) || (text == "Y")) {
            switch (MenuResultStateMachine) {
              case 0:
                break;
              case 1:
                if (!PCResetFl) {
                  MessageToSend = StrName + "/ Restarting rig by on/off key...";
                  AddMsgToSend = true;
                  digitalWrite(ONOFF_PIN, LOW);
                  CntConnected = DISCONNECT_CNT;
                  if (spikesFl)
                    OffCounter = OffCounterMem;
                  else
                    OffCounter = 7000;
                  OffStateMachine = 1;
                  TestConnection();
                  SendKyInfo();
                } else {
                  MessageToSend = StrName + "/ Restarting rig by reset key...";
                  AddMsgToSend = true;
                  digitalWrite(RESET_PIN, LOW);
                  OffCounter = 2000;
                  CntConnected = DISCONNECT_CNT;
                  TestConnection();
                  SendKyInfo();
                }
                MenuResultStateMachine = 0;
                break;
              case 2:
                RigOff();
                break;
              case 3:
                RigOn();
                break;
              default:
                MenuResultStateMachine = 0;
                break;
            }
          } else if ((text == ("N" + StrName)) || (text == "N")) {
            MessageToSend = StrName + "/ Operation canceled";
            AddMsgToSend = true;
          } else if ((text == ("RstrtRig" + StrName)) || (text == "RstrtRig")) {
            MenuYesNo();
            bot.sendMessageWithInlineKeyboard(chat_id, StrName + "/  Confirm operation", "", keyboardJson);
            MenuResultStateMachine = 1;
            MenuResultStateMachineCnt = 0;
          } else if ((text == ("OffRig" + StrName)) || (text == "OffRig")) {
            MenuYesNo();
            bot.sendMessageWithInlineKeyboard(chat_id, StrName + "/  Confirm operation", "", keyboardJson);
            MenuResultStateMachine = 2;
            MenuResultStateMachineCnt = 0;
          } else if ((text == ("OnRig" + StrName)) || (text == "OnRig")) {
            MenuYesNo();
            bot.sendMessageWithInlineKeyboard(chat_id, StrName + "/  Confirm operation", "", keyboardJson);
            MenuResultStateMachine = 3;
            MenuResultStateMachineCnt = 0;
          } else if ((text == ("S" + StrName)) || (text == "S")) {
            Serial.println("Status");

            GetStatus = 1;

            MessageToSend = StrName + "/ \r\nMonitor status:\r\n\r\nConnected with PC: ";
            if (Connected) {
              MessageToSend = MessageToSend + "Yes" + "\r\n" + "WiFi signal: " + WiFi.RSSI() + "dB\r\nwait for next message...\r\n";
              dbgInfo();
            } else {
              MessageToSend = MessageToSend + "NO !\r\n";
              MessageToSend = MessageToSend + "Restart interval: " + String(RestartNoConnTime / 1000) + "\r\n";
              MessageToSend = MessageToSend + "Restart Counter: " + String((RestartNoConnTime - RestartNoConnCnt) / 1000) + "\r\n";
              MessageToSend = MessageToSend + "Restart attempts: " + String(RestartAttempts) + "\r\n";
              MessageToSend = MessageToSend + "Attempts completed: " + String(RestartAttempts - RestartAttemptsCnt) + "\r\n";
              MessageToSend = MessageToSend + "WiFi signal: " + WiFi.RSSI() + "dB\r\n";
              MessageToSend = MessageToSend + "WiFi reconnect counter: " + String(ReconnectedWiFiCnt) + "\r\n";
              dbgInfo();
            }
            AddMsgToSend = true;
          } else if ((text == ("IP" + StrName)) || (text == "IP")) {
            MessageToSend = StrName + "/ IP adress: " + WiFi.localIP().toString() + "/update";
            AddMsgToSend = true;
          } else if ((text == ("SpikesOff" + StrName)) || (text == "SpikesOff")) {
            spikesFl = 0;
            EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
            EEPROM.commit();
            ReadEEPROM();
            SendKyInfo();
            MessageToSend = StrName + "/ Spikes mining is stopped: ";
            AddMsgToSend = true;
          } else if ((text == ("SpikesOn" + StrName)) || (text == "SpikesOn")) {
            ResetRigFl = 0;
            spikesFl = 1;
            EEPROM.writeLong(StatusFlags_ADDR, StatusFlags.Flags);
            EEPROM.commit();
            ReadEEPROM();
            SendKyInfo();
            MessageToSend = StrName + "/ Spikes mining is started: ";
            AddMsgToSend = true;
          } else if ((text == ("Rst" + StrName)) || (text == "Rst")) {
            //      delay(2000);
            //       ESP.restart();
          } else {
            MessageToSend = StrName + "/ Error command: " + text;
            AddMsgToSend = true;
          }
        }
      }
    }
    vTaskDelay(1000);  // one tick delay (15ms) in between reads for stability
  }
}

void TaskMain(void *pvParameters)  // This is a task.
{
  (void)pvParameters;
  //--->esp_task_wdt_add(nullptr);

  while (1) {
    //--->esp_task_wdt_reset();
    if (SendOptionsFl) {
      SendOptions();
      SendOptionsFl = false;
    }
    SendStatus();

    if (SendWiFiScanResultFl) {
      SendWiFiScanResultFl = false;
      SendWiFiScanResult();
    }

    if (AddMsgToSend) {
      AddMessageToSend(MessageToSend);
      AddMsgToSend = false;
    }

    if (SendPacketToPC) {
      SendPacketToPC = false;
      SendCMDToPCStateMachine = 1;
    }

    if (OffResetFl && MsgToTelegrammReady) {
      OffResetFl = false;
      if (PCOffFl) {
        digitalWrite(ONOFF_PIN, LOW);
        if (spikesFl)
          OffCounter = OffCounterMem;
        else
          OffCounter = 7000;
        OffStateMachine = 1;
      }
      if (PCResetFl) {
        digitalWrite(RESET_PIN, LOW);
        OffCounter = 2000;
      }
      TstConn = true;
    }

    if (WriteResetCounter) {
      WriteResetCounter = false;
      Serial.println(StatusFlags.Flags);
      EEPROM.writeUInt(ResetCounter_ADDR, ResetCounter);
      EEPROM.commit();
      ReadEEPROM();
      Serial.println(StatusFlags.Flags);
    }
    WatchMain();
    //    digitalWrite(TEST_PIN, !digitalRead(TEST_PIN));
    vTaskDelay(1);  // one tick delay (15ms) in between reads for stability
  }
}

void Timer(void *pvParameters) {
  (void)pvParameters;

  //  esp_task_wdt_add(nullptr);

  while (1) {

    //    digitalWrite(RESET_PIN, !digitalRead(RESET_PIN));
    //    digitalWrite(ONOFF_PIN, !digitalRead(ONOFF_PIN));

    SendingPacket();

    if (Cnt2sec < 2000) {
      Cnt2sec++;
      if (Cnt2sec >= 2000) {
        // Serial.println(enableOTA);
        Cnt2sec = 0;
      }
    }

    if (MenuResultStateMachineCnt < INLINE_YESNO_TIMEOUT) {
      MenuResultStateMachineCnt++;
      if (MenuResultStateMachineCnt >= INLINE_YESNO_TIMEOUT) {
        MenuResultStateMachine = 0;
      }
    }

    if (!Connected) {
      if (RestartNoConnCnt) {
        RestartNoConnCnt--;
        if (!RestartNoConnCnt) {
          if (RestartAttemptsCnt) {
            if (!spikesFl)
              RestartPC((int)(RestartAttempts - RestartAttemptsCnt));
            RestartAttemptsCnt--;
            // if (!RestartAttemptsCnt)
            // {
            //   MessageToSend = StrName + "/ Cannot restart rig.";
            //   AddMsgToSend = true;
            // }
          }
        }
      } else
        RestartNoConnCnt = RestartNoConnTime;
    } else {
      RestartAttemptsCnt = RestartAttempts;
      RestartNoConnCnt = RestartNoConnTime;
    }

    if (WiFiCntTimeout)
      WiFiCntTimeout--;
    if (!spikesFl) {
      if (!Starting && !Connected) {
        if (RestartDelayCnt < RestartDelayL) {
          RestartDelayCnt++;
          if (RestartDelayCnt >= RestartDelayL) {
            ResetCounter++;
            WriteResetCounter = true;
            StrToTelegramm = StrName + ": NoConnection with PC. ";
            if (ResetRigFl)
              StrToTelegramm = StrToTelegramm + "Reseting rig...";
            MsgToTelegramm = true;
            AddMessageToSend(StrToTelegramm);
            if (ResetRigFl) {
              MsgToTelegrammReady = true;  // false;
              OffResetFl = true;
              Starting = true;
            }
          }
        }
      }
    } else
      ResetRigFl = 0;
    if (RestartingFl) {
      OffCounter--;
      if (!OffCounter) {
        RestartingFl = false;
        digitalWrite(RESET_PIN, HIGH);
        digitalWrite(ONOFF_PIN, HIGH);
      }
    }
    switch (RestartingStateMachine) {
      case 0:
        if (PCResetFl) {
          if (OffCounter) {
            OffCounter--;
            if (!OffCounter) {
              digitalWrite(RESET_PIN, HIGH);
            }
          }
        }
        if (PCOffFl) {
          switch (OffStateMachine) {
            case 0:
              break;
            case 1:
              OffCounter--;
              if (!OffCounter) {
                digitalWrite(ONOFF_PIN, HIGH);
                OffStateMachine = 2;
                OffCounter = 5000;
              }
              break;
            case 2:
              OffCounter--;
              if (!OffCounter) {
                digitalWrite(ONOFF_PIN, LOW);
                OffStateMachine = 3;
                OffCounter = 1000;
              }
              break;
            case 3:
              OffCounter--;
              if (!OffCounter) {
                digitalWrite(ONOFF_PIN, HIGH);
                OffStateMachine = 0;
              }
              break;
            case 4:
              OffCounter--;
              if (!OffCounter) {
                digitalWrite(ONOFF_PIN, HIGH);
                OffStateMachine = 0;
              }
              break;
          }
        }
        break;
      case 1:
        OffCounter--;
        if (!OffCounter) {
          RestartingStateMachine = 0;
          digitalWrite(RESET_PIN, HIGH);
          digitalWrite(ONOFF_PIN, HIGH);
        }
        break;
      case 2:
        OffCounter--;
        if (!OffCounter) {
          RestartingStateMachine = 0;
          digitalWrite(RESET_PIN, HIGH);
          digitalWrite(ONOFF_PIN, HIGH);
        }
        break;
    }

    if (Connected)
      digitalWrite(TEST_PIN, HIGH);
    else
      digitalWrite(TEST_PIN, LOW);

    if (CntConnected) {
      CntConnected--;
      if (!CntConnected)
        Connected = false;
    }

    if (cntTimeOut) {
      cntTimeOut--;
      if (!cntTimeOut) {
        gettingPacket = false;
      }
    }
    if (TestConnectionCnt)
      TestConnectionCnt--;
    StateLed();
    if (!spikesFl)
      LEDFunc();
    else
      onOffSpikes();

    if (cntEtcPriceFl)
      cntEtcPriceFl--;
    else {
      cntEtcPriceFl = SPIKES_PERIOD;
      getPriceStr = 1;
    }

    cntSpikesPeriod++;

    if (cntSpikesPeriodMemCnt < cntSpikesPeriodMem) {
      cntSpikesPeriodMemCnt++;
      if (cntSpikesPeriodMemCnt >= cntSpikesPeriodMem) {
        spikesOn = 1;
        //  etcPriceFlSending();
      }
    }

    vTaskDelay(1);
  }
}

void SendCMDToPC(void) {
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;
  serialTrmBuffer[2] = 3;
  serialTrmBuffer[3] = 0;
  serialTrmBuffer[4] = CmdToPC;
  serialTrmBuffer[5] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void StateLed(void) {
  if (Connected) {
    if (WiFi.status() == WL_CONNECTED)
      LEDStateMachine = LED_DATA_EXCHANGE;
    else
      LEDStateMachine = LED_WiFiCONNECTING;
  } else {
    if (WiFi.status() == WL_CONNECTED)
      LEDStateMachine = LED_WiFiCONNECTED;
    else
      LEDStateMachine = LED_WiFiCONNECTING;
  }
}

void AddMessageToSend(String Str) {
  StrArray[IndexStrArray] = Str;
  if (IndexStrArray < (SIZE_StrArray - 1))
    IndexStrArray++;
  if (telegramEnabled) {
    Serial.print("IndexStrArray inc: ");
    Serial.println(IndexStrArray);
  }
}

void LoadDefaultsSettings(void) {
  for (int i = 0; i < EEPROM_SIZE; i++)
    EEPROM.writeByte(i, 0xff);
  EEPROM.writeLong(StatusFlags_ADDR, 0);
  UsedMem = 0x12345678;
  EEPROM.writeLong(UsedMem_ADDR, UsedMem);
  EEPROM.writeString(ssid_ADDR, "Your SSID");
  EEPROM.writeString(password_ADDR, "Your password");
  EEPROM.writeString(OrgID_ADDR, "Your ORGID");
  EEPROM.writeString(Key_ADDR, "Your key");
  EEPROM.writeString(Secret_ADDR, "Your secret key");
  EEPROM.writeUInt(RestartDelay_ADDR, 300);
  EEPROM.writeLong(StatusFlags_ADDR, 0);
  EEPROM.writeString(RigNameToWatch_ADDR, "Your rig name");
  EEPROM.writeUInt(ResetCounter_ADDR, 0);
  EEPROM.writeString(BOTtoken_ADDR, "Your bot token");
  EEPROM.writeString(CHAT_ID_ADDR, "-100000000");
  EEPROM.writeULong(RestartNoConnTime_ADDR, 900000000);
  EEPROM.writeUShort(RestartAttempts_ADDR, 3);
  EEPROM.writeUShort(ReconnectedWiFiCnt_ADDR, 0);
  EEPROM.writeShort(OffCounterMem_ADDR, 7000);
  EEPROM.writeULong(spikesOnDuration_ADDR, 420000);
  EEPROM.writeULong(spikesOffDuration_ADDR, 150000);

  EEPROM.commit();
}

void Scanning(void *pvParameters) {
  (void)pvParameters;

  while (1) {

    if (ScanFl) {
      if (xSemaphoreTake(xWiFiSemaphore, (TickType_t)5) == pdTRUE) {
        LastSemaphore = 4;
        WiFi.disconnect();
        WiFi.mode(WIFI_OFF);
        vTaskDelay(300);

        WiFi.mode(WIFI_STA);

        Serial.println("WiFi disconnected, scan start");

        vTaskDelay(300);
        SSIDStr = "";
        // WiFi.scanNetworks will return the number of networks found
        int n = WiFi.scanNetworks();
        Serial.println("scan done");
        if (n == 0) {
          Serial.println("no networks found");
        } else {
          Serial.print(n);
          Serial.println(" networks found");
          for (int i = 0; i < n; ++i) {
            SSIDStr = SSIDStr + WiFi.SSID(i) + "." + WiFi.RSSI(i) + ",";  //+(WiFi.encryptionType(i) == WIFI_AUTH_OPEN) ? " " : "*, ";
          }
        }

        Serial.println(SSIDStr);

        Serial.println("");
        ScanFl = false;

        SendWiFiScanResultFl = true;
        xSemaphoreGive(xWiFiSemaphore);
      }
      {
        if (debugInfo)
          Serial.println("Mutex busy (scanning) LS=" + String(LastSemaphore));
        vTaskDelay(1000);
      }
    }
    vTaskDelay(100);
  }
}

void SendWiFiScanResult(void) {
  int i, Cnt = 5;

  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = WIFI_SSIDs;

  i = SSIDStr.length();
  serialTrmBuffer[Cnt++] = i;
  serialTrmBuffer[Cnt++] = i >> 8;

  for (i = 0; i < SSIDStr.length(); i++) {
    serialTrmBuffer[Cnt++] = SSIDStr[i];
  }

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void FirmUpdate(void *pvParameters) {
  (void)pvParameters;

  while (WiFi.status() != WL_CONNECTED) {
    vTaskDelay(1000);
  }

  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request) {
    request->send(200, "text/plain", "Hi! I am antiPOS-W.");
  });

  AsyncElegantOTA.begin(&server);  // Start ElegantOTA
  server.begin();
  Serial.println("HTTP server started");

  while (1) {
    AsyncElegantOTA.loop();
    vTaskDelay(1);
  }
}

void SendMAC(void) {
  int Cnt = 5;
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = MAC_INFO;

  serialTrmBuffer[Cnt++] = (uint8_t)myMAC.length();
  serialTrmBuffer[Cnt++] = 0;

  for (int i = 0; i < (uint8_t)myMAC.length(); i++) {
    serialTrmBuffer[Cnt++] = myMAC[i];
  }

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void SendStatus(void) {
  switch (GetStatus) {
    case 0:
      break;
    case 1:
      CmdToPC = GET_STATUS;
      WaitForReply = true;
      SendPacketToPC = true;
      GetStatus = 2;
      break;
    default:
      GetStatus = 0;
      break;
  }
}

void SendOptions(void) {
  int Cnt = 5;
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;

  serialTrmBuffer[4] = SEND_OPTIONS_TO_PC;

  serialTrmBuffer[Cnt++] = (uint8_t)RestartNoConnTime;
  serialTrmBuffer[Cnt++] = (uint8_t)(RestartNoConnTime >> 8);
  serialTrmBuffer[Cnt++] = (uint8_t)(RestartNoConnTime >> 16);
  serialTrmBuffer[Cnt++] = (uint8_t)(RestartNoConnTime >> 24);

  serialTrmBuffer[Cnt++] = (uint8_t)RestartAttempts;
  serialTrmBuffer[Cnt++] = (uint8_t)(RestartAttempts >> 8);

  uint8_t l = Version.length();
  serialTrmBuffer[Cnt++] = l;

  for (int i = 0; i < l; i++) {
    serialTrmBuffer[Cnt++] = Version[i];
  }

  l = StrName.length();
  serialTrmBuffer[Cnt++] = l;

  for (int i = 0; i < l; i++) {
    serialTrmBuffer[Cnt++] = StrName[i];
  }

  serialTrmBuffer[Cnt++] = (uint8_t)spikesOnDuration;
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOnDuration >> 8);
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOnDuration >> 16);
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOnDuration >> 24);

  serialTrmBuffer[Cnt++] = (uint8_t)spikesOffDuration;
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOffDuration >> 8);
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOffDuration >> 16);
  serialTrmBuffer[Cnt++] = (uint8_t)(spikesOffDuration >> 24);

  serialTrmBuffer[Cnt++] = (uint8_t)OffCounterMem;
  serialTrmBuffer[Cnt++] = (uint8_t)(OffCounterMem >> 8);

  serialTrmBuffer[2] = Cnt - 2;
  serialTrmBuffer[3] = 0;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void RestartPC(int attempt) {
  if (spikesFl) {
    if (debugInfo)
      MessageToSend = StrName + "/ spikesFl=ON  spikesFl=ON  spikesFl=ON  spikesFl=ON  spikesFl=ON  spikesFl=ON " + String(attempt + 1);
    AddMsgToSend = true;
    return;
  } else {
    //  MessageToSend = StrName + "/ No connection with PC, restartng rig. Attempt: " + String(attempt + 1);
    if (debugInfo)
      MessageToSend = StrName + "/ spikesFl=OFF spikesFl=OFF spikesFl=OFF spikesFl=OFF spikesFl=OFF spikesFl=OFF " + String(attempt + 1);

    AddMsgToSend = true;
    return;
  }
  ResetCounter++;
  WriteResetCounter = true;

  digitalWrite(ONOFF_PIN, LOW);
  digitalWrite(RESET_PIN, LOW);
  OffCounter = 2000;
  RestartingFl = true;
}

void MenuYesNo(void) {
  keyboardJson = "";
  keyboardJson = keyboardJson + "[[{ \"text\" : \"YES? (Y)\", \"callback_data\" : \"Y" + "\" }]";
  keyboardJson = keyboardJson + ",[{ \"text\" : \"NO? (N)\", \"callback_data\" : \"N" + "\" }]]";
}

uint8_t nums[][2] = {
  0x40, 0x9C,  // 58:BF:25:17:40:9C
  0xB4, 0x94,  // 58:BF:25:83:B4:94
  0x5A, 0xDC,  // 08:3A:F2:7D:5A:DC
  0xC1, 0x9C,  // 4C:EB:D6:75:C1:9C
  0x3F, 0xD8,  // 58:BF:25:17:3F:D8
  0x40, 0xCC,  // 58:BF:25:17:40:CC
  0x40, 0x9C,  // 58:BF:25:17:40:9C
  0xA8, 0xF0,  // C8:C9:A3:CB:A8:F0
  0x98, 0xA8,  // 08:3A:F2:7C:98:A8
  0xA9, 0x64,  // 58:BF:25:83:A9:64
  0xB8, 0xCC,  // C8:C9:A3:CB:B8:CC
  0xAE, 0x24   // C8:C9:A3:CB:AE:24
};

bool compare(uint8_t mc, uint8_t index) {
  uint16_t sz = sizeof(nums) / sizeof(nums[0]);
#ifdef GET_MAC_DEF
  return true;
#endif
  Serial.println(sz);
  for (int i = 0; i < sz; i++) {
    if (nums[i][index] == mc)
      return true;
  }
  return false;
}

void SendingPacket(void) {
  switch (SendCMDToPCStateMachine) {
    case 0:
      break;
    case 1:
      if (WaitForReply) {
        WtReply = true;
        SendCMDToPCStateMachine = 2;
        WtReplyCnt = 10000;
        Attempts = 6;
      } else {
        WtReply = false;
        SendCMDToPCStateMachine = 0;
      }
      SendCMDToPC();
      break;
    case 2:
      if (WtReply) {
        WtReplyCnt--;
        if (WtReplyCnt == 0) {
          WtReplyCnt = 10000;
          SendCMDToPC();
          Attempts--;
          Serial.println(Attempts);
          if (!Attempts) {
            SendCMDToPCStateMachine = 0;
            WaitForReply = false;
          }
        }
      } else {
        SendCMDToPCStateMachine = 0;
        WaitForReply = false;
      }
      break;
  }
}

void spikesTask(void *pvParameters) {
  vTaskDelay(3000);
  bool tryingMutex = true;
  uint8_t spcnt = 0;

  spikeStateMachine = 0;

  while (1) {
    if (xSemaphoreTake(xWiFiSemaphore, (TickType_t)5) == pdTRUE) {
      LastSemaphore = 5;
      tryingMutex = true;
      if ((WiFi.status() == WL_CONNECTED) && (getPriceStr)) {
        HTTPClient http;
        digitalWrite(LED, HIGH);
        getPriceStr = 0;


        http.begin("https://api2.nicehash.com/main/api/v2/hashpower/orders/summaries?market=EU&algorithm=ETCHASH");


        int httpCode = http.GET();


        if (httpCode > 0) {


          String payload = http.getString();
          Serial.println(httpCode);
          Serial.println(payload);

          /*        StaticJsonDocument<1536> doc;

                  DeserializationError error = deserializeJson(doc, payload.c_str());

                  if (error)
                  {
                    Serial.print("deserializeJson() failed: ");
                    Serial.println(error.c_str());
                    return;
                  }
                  uint8_t cnt = 0;
                  for (JsonPair stat : doc["stats"].as<JsonObject>())
                  {
                    const char *stat_key = stat.key().c_str(); // "EU", "USA"
                    JsonObject stat_value_orders_0 = stat.value()["orders"][0];
                    const char *priceStr = stat_value_orders_0["price"];
        */
          StaticJsonDocument<768> doc;

          DeserializationError error = deserializeJson(doc, payload.c_str());

          if (error) {
            Serial.print("deserializeJson() failed: ");
            Serial.println(error.c_str());
            return;
          }
          uint8_t cnt = 0;
          JsonObject summaries_EU_ETCHASH = doc["summaries"]["EU,ETCHASH"];

          for (JsonObject summaries_EU_ETCHASH_prof : summaries_EU_ETCHASH["profs"].as<JsonArray>()) {

            const char *summaries_EU_ETCHASH_prof_type = summaries_EU_ETCHASH_prof["type"];      // "FIXED", "STANDARD"
            double summaries_EU_ETCHASH_prof_speed = summaries_EU_ETCHASH_prof["speed"];         // 0, 2860500834938.358
            double summaries_EU_ETCHASH_prof_price = summaries_EU_ETCHASH_prof["price"];         // 0, ...
            int summaries_EU_ETCHASH_prof_rigCount = summaries_EU_ETCHASH_prof["rigCount"];      // 0, 24607
            int summaries_EU_ETCHASH_prof_orderCount = summaries_EU_ETCHASH_prof["orderCount"];  // 0, 15

            if (cnt == 1) {
              etcPriceFl = (float)(summaries_EU_ETCHASH_prof_price * 10000);  // atof(priceStr);
              uint16_t i;

              if (!startFiltering) {
                for (i = 0; i < (FILTER_ARR_SIZE - 1); i++)
                  etcPriceFlArray[i] = etcPriceFlArray[i + 1];
                etcPriceFlArray[FILTER_ARR_SIZE - 1] = etcPriceFl;
              } else {
                startFiltering = false;
                for (i = 0; i < (FILTER_ARR_SIZE); i++)
                  etcPriceFlArray[i] = etcPriceFl;
              }

              double etcAvTmp = 0;
              for (i = 0; i < (FILTER_ARR_SIZE); i++)
                etcAvTmp = etcAvTmp + etcPriceFlArray[i];

              etcAvTmp = etcAvTmp / FILTER_ARR_SIZE;
              etcPriceFlAv = (float)etcAvTmp;

              //     Serial.println(priceStr);
              if (debugInfo) {
                Serial.println(etcPriceFl);
                Serial.println(etcPriceFlAv);
              }
              switch (spikeStateMachine) {
                case 0:
                  if (etcPriceFl > (etcPriceFlAv * 2.5))
                    spikeStateMachine = 1;
                  spcnt = 0;
                  break;
                case 1:
                  if (etcPriceFl > (etcPriceFlAv * 2.5)) {
                    spcnt++;
                    if (spcnt >= 3) {
                      if (testOnOff)
                        spikesOn = 1;
                      etcPriceFlAv = 60;
                      MessageToSend = StrName + "/ Spike starting";
                      if (!Connected)
                        dbgInfo();
                      AddMsgToSend = true;
                      spikeStateMachine = 2;
                    }
                  } else {
                    spcnt = 0;
                    spikeStateMachine = 0;
                  }
                  break;
                case 2:
                  if (etcPriceFl < (etcPriceFlAv * 3)) {
                    spikeStateMachine = 0;
                    etcPriceFlAv = 50;
                    cntSpikesPeriodMem = cntSpikesPeriod - spikesOnDuration;
                    cntSpikesPeriod = 0;
                    cntSpikesPeriodMemCnt = 0;
                    spikesOff = 1;
                  }
                  break;
              }

              if (xSemaphoreTake(xSerialMutex, (TickType_t)100) == pdTRUE) {
                etcPriceFlSending();
                xSemaphoreGive((xSerialMutex));
              }
            }
            cnt++;
          }
        } else {
          Serial.println("Ошибка HTTP-запроса");
        }

        http.end();
        digitalWrite(LED, LOW);
      }
      xSemaphoreGive(xWiFiSemaphore);
    } else {
      if (tryingMutex) {
        if (debugInfo)
          Serial.println("Mutex busy (spikesTask) LS=" + String(LastSemaphore));
        tryingMutex = false;
      }
    }
    vTaskDelay(1);
  }
}

void etcPriceFlSending(void) {
  uint16_t Cnt;
  for (Cnt = 0; Cnt < 200; Cnt++)
    serialTrmBuffer[Cnt] = 0;
  serialTrmBuffer[0] = HEADER1;
  serialTrmBuffer[1] = HEADER2;
  //  serialTrmBuffer[2] = 17;
  serialTrmBuffer[3] = 0;
  serialTrmBuffer[4] = SEND_ETC_PRICE;

  Cnt = 5;

  *(uint32_t *)&serialTrmBuffer[Cnt] = *(uint32_t *)&etcPriceFl;
  Cnt = Cnt + 4;

  *(uint32_t *)&serialTrmBuffer[Cnt] = *(uint32_t *)&etcPriceFlAv;
  Cnt = Cnt + 4;

  *(uint32_t *)&serialTrmBuffer[Cnt] = cntSpikesPeriod;
  Cnt = Cnt + 4;

  *(uint32_t *)&serialTrmBuffer[Cnt] = cntSpikesPeriodMem;
  Cnt = Cnt + 4;

  *(uint32_t *)&serialTrmBuffer[Cnt] = cntSpikesPeriodMemCnt;
  Cnt = Cnt + 4;

  serialTrmBuffer[2] = Cnt - 1;

  serialTrmBuffer[Cnt] = CalcCheckSumm(serialTrmBuffer[2] + ((uint16_t)serialTrmBuffer[3] << 8), &serialTrmBuffer[2]);
  TrmSerial();
}

void onOffSpikes(void) {
  switch (onOffSpikesStateMachine) {
    case 0:
      if (!testOnOff)
        onOffSpikesStateMachine = 10;
      else if (switchOnPCWhenStart) {
        if (!Connected) {
          onOffSpikesStateMachineCnt = 10000;
          onOffSpikesStateMachine = 1;
        } else {
          onOffSpikesStateMachine = 5;
        }
      } else
        onOffSpikesStateMachine = 2;
      break;
    case 1:
      onOffSpikesStateMachineCnt--;
      if (!onOffSpikesStateMachineCnt) {
        RigOn();
        onOffSpikesStateMachine = 2;
      } else {
        if (Connected) {
          onOffSpikesStateMachine = 5;
        }
      }
      break;
    case 2:  // PC is off
      if (Connected) {
        onOffSpikesStateMachine = 5;
      } else {
        if (spikesOn) {
          spikesOn = 0;
          onOffSpikesStateMachineCnt = 1;  // Switching On PC
          onOffSpikesStateMachine = 1;
        }
        if (spikesOff)
          spikesOff = 0;
      }
      break;
    case 3:
      if (Connected) {
        if (spikesOn)
          spikesOn = 0;
        if (spikesOff)
          spikesOff = 0;
      } else {
        onOffSpikesStateMachine = 2;
      }
      break;

    case 4:
      if (spikesOffDurationCnt) {
        spikesOffDurationCnt--;
        if (!spikesOffDurationCnt) {
          RigOff();
          onOffSpikesStateMachine = 3;
        }
      } else
        spikesOffDurationCnt = 1;
      break;

    case 5:  // PC is On
      if (Connected) {
        if (spikesOn)
          spikesOn = 0;
        if (spikesOff) {
          spikesOff = 0;
          spikesOffDurationCnt = spikesOffDuration;
          onOffSpikesStateMachine = 4;
          MessageToSend = StrName + "/ Spike is finished";
          AddMsgToSend = true;
        }
      } else {
        onOffSpikesStateMachine = 2;
      }
      break;
    case 10:
      if (spikesOn) {
        spikesOn = 0;
        RigOn();
      }
      if (spikesOff) {
        spikesOff = 0;
        RigOff();
      }
      if (testOnOff)
        onOffSpikesStateMachine = 2;
      break;
  }
}

void RigOff(void) {
  MessageToSend = StrName + "/ Switching OFF rig...";
  AddMsgToSend = true;
  digitalWrite(ONOFF_PIN, LOW);
  CntConnected = DISCONNECT_CNT;
  if (spikesFl)
    OffCounter = OffCounterMem;
  else
    OffCounter = 7000;
  TestConnection();
  SendKyInfo();
  MenuResultStateMachine = 0;
  RestartingStateMachine = 1;
}

void RigOn(void) {
  MessageToSend = StrName + "/ Switching ON rig...";
  AddMsgToSend = true;
  digitalWrite(ONOFF_PIN, LOW);
  CntConnected = DISCONNECT_CNT;
  OffCounter = 2000;
  TestConnection();
  SendKyInfo();
  MenuResultStateMachine = 0;
  RestartingStateMachine = 2;
}

void ledFlash(uint16_t period, uint8_t cnt) {
  uint8_t i;
  period = period >> 1;

  for (i = 0; i < cnt; i++) {
    digitalWrite(LED, HIGH);
    delay(period);
    digitalWrite(LED, LOW);
    delay(period);
  }
}

void esp32WifiModuleClientReconnect(void) {
  WiFi.config(0U, 0U, 0U, 0U, 0U);
  WiFi.begin((const char *)ssid, password);
}

void dbgInfo(void) {
  if (debugInfo) {
    if (spikesFl) {
      MessageToSend = MessageToSend + "------------------\r\n";
      MessageToSend = MessageToSend + "Turn on in (sec): " + String((cntSpikesPeriodMem - cntSpikesPeriodMemCnt) / 1000) + "\r\n";
      MessageToSend = MessageToSend + "Prev. period (sec): " + String(cntSpikesPeriodMem / 1000) + "\r\n";
      MessageToSend = MessageToSend + "Period caclulating (sec): " + String(cntSpikesPeriod / 1000) + "\r\n";
      MessageToSend = MessageToSend + "onOffSpikesStateMachine: " + String(onOffSpikesStateMachine) + "\r\n";
      MessageToSend = MessageToSend + "Delay after spike (sec): " + String(spikesOffDurationCnt / 1000) + "\r\n";
      MessageToSend = MessageToSend + "spikeStateMachine: " + String(spikeStateMachine) + "\r\n";
      MessageToSend = MessageToSend + "etcPriceFl: " + String(etcPriceFl) + "\r\n";
      MessageToSend = MessageToSend + "etcPriceFlAv: " + String(etcPriceFlAv) + "\r\n";
    }
  }
}
