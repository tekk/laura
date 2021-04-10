/*
 * Baofeng/Wouxun/TYT/Kenwood/Retevis/Kenwood etc. Computer-Aided Auto PTT Control Circuit Firmware
 * Using Pro Micro 3.3V / 5V
 * 
 * Created:
 * 05. September 2016
 * 
 * Written by Peter Javorsky <tekk.sk@gmail.com>
 *
 */

const unsigned int serialReportInterval = 200;  // milliseconds
const unsigned int linkACKinterval = 1200;      // milliseconds
const byte numChars = 32;                       // length of input buffer (bytes count)
char receivedChars[numChars];                   // input buffer
boolean newData = false;
const char startMarker = '<';
const char endMarker = '>';

const char PIN_PTT_CONTROL = 9;               // mode: OUTPUT on TX, INPUT_PULLUP on RX; TX when LOW
const char PIN_TRANSMITTING_STATE = A0;       // mode: INPUT; TX when LOW
const char PIN_PTT_BTN = A1;                  // mode: INPUT_PULLUP; pressed when LOW
const char PIN_SOUND_PRESENT = A2;            // mode: INPUT
const char PINS_LED_RGB[3] = { 2, 3, 4 };     // mode: OUTPUT
const char PIN_LED_RED = 8;                   // mode: OUTPUT
const char PIN_LED_GREEN = 7;                 // mode: OUTPUT
const char PIN_LED_YELLOW = 6;                // mode: OUTPUT
const char PIN_DTMF_SIGNAL = 10;              // mode: INPUT
const char PIN_DTMF_Q1 = A3;
const char PIN_DTMF_Q2 = 14;
const char PIN_DTMF_Q3 = 15;
const char PIN_DTMF_Q4 = 5;
const char PIN_TRANSISTOR_BASE_PTT = 16;      // mode: OUTPUT
const char* DTMFTable = "D1234567890*#ABC";

const unsigned int ledFlashAfterRX = 7 * 1000;        // milliseconds
const unsigned int ledOnAfterRX = 2000;               // milliseconds
const unsigned int ledRXFlashInterval = 800;          // milliseconds
const unsigned int startupLEDFlash = 100;             // milliseconds
const unsigned int forceTXMinimumInterval = 300;      // milliseconds
unsigned long lastRX = millis() + ledFlashAfterRX;    // milliseconds
unsigned long startupLEDs = millis() + 2 * startupLEDFlash; // milliseconds
unsigned long linkTimeout = millis() + linkACKinterval;     // milliseconds
unsigned long forceTransmit = 0;                      // milliseconds
unsigned long recievingDTMF = 0;
bool lastRisingDTMF = false;
bool risingDTMF = false;
char currentDTMFBinary = 0;
char receivedDTMF[10] = "";
byte receivedDTMFCount = 0;
byte DTMFReceivedNow = false;

bool isTransmitting() {
  return !((bool)digitalRead(PIN_TRANSMITTING_STATE));
}

void setTX(bool enable) {
  pinMode(PIN_PTT_CONTROL, enable ? OUTPUT : INPUT_PULLUP);
  digitalWrite(PIN_PTT_CONTROL, !enable);
  digitalWrite(PIN_TRANSISTOR_BASE_PTT, enable);
}

bool isPTTBTNPressed() {
  return !digitalRead(PIN_PTT_BTN);       // pressed when LOW
}

bool isSoundPresent() {
  return !digitalRead(PIN_SOUND_PRESENT); // present when LOW
}

void correctTransmittingLED(){
  digitalWrite(PIN_LED_RED, isTransmitting());
}

void correctReceivingLED() {
  if (isSoundPresent()) lastRX = millis();

  if (millis() < lastRX + ledOnAfterRX) {
    digitalWrite(PIN_LED_GREEN, HIGH);
  } else if (millis() < lastRX + ledFlashAfterRX) {
    digitalWrite(PIN_LED_GREEN, millis() % ledRXFlashInterval < 30);
  } else {
    digitalWrite(PIN_LED_GREEN, LOW);
  }
}

void setup() {
  Serial.begin(115200);
  Serial.println("<Laura ready>");

  setTX(false);
  pinMode(PIN_PTT_CONTROL, INPUT_PULLUP);
  pinMode(PIN_TRANSMITTING_STATE, INPUT);
  correctTransmittingLED();
  pinMode(PIN_PTT_BTN, INPUT_PULLUP);
  pinMode(PIN_SOUND_PRESENT, INPUT);
  pinMode(PIN_TRANSISTOR_BASE_PTT, OUTPUT);
  pinMode(PIN_DTMF_Q1, INPUT);
  pinMode(PIN_DTMF_Q2, INPUT);
  pinMode(PIN_DTMF_Q3, INPUT);
  pinMode(PIN_DTMF_Q4, INPUT);
  pinMode(PIN_DTMF_SIGNAL, INPUT);
  digitalWrite(PIN_TRANSISTOR_BASE_PTT, LOW);
  
  for (char i = 0; i < 3; i++)
  {
    pinMode(PINS_LED_RGB[i], OUTPUT);
    digitalWrite(PINS_LED_RGB[i], HIGH);
  }
  pinMode(PIN_LED_RED, OUTPUT);
  pinMode(PIN_LED_GREEN, OUTPUT);
  pinMode(PIN_LED_YELLOW, OUTPUT);

  digitalWrite(PIN_LED_RED, LOW);
  digitalWrite(PIN_LED_GREEN, LOW);
  digitalWrite(PIN_LED_YELLOW, LOW);
}

void corrrectStartupLEDs() {
  if (millis() < startupLEDs)
  {
    for (char i = 0; i < 3; i++)
    {
      digitalWrite(PINS_LED_RGB[i], millis() % (80 + 33 * i)  < 10);
    }
  }
  else
  {
      digitalWrite(PINS_LED_RGB[0], millis() < forceTransmit);
      digitalWrite(PINS_LED_RGB[1], millis() < forceTransmit);
      digitalWrite(PINS_LED_RGB[2], HIGH);
  }
}

void correctLinkLED() {
  digitalWrite(PIN_LED_YELLOW, linkTimeout > millis());
}

void recvWithStartEndMarkers() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char rc;
    
    while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();

        if (recvInProgress == true) {
            if (rc != endMarker) {
                receivedChars[ndx++] = rc;

                if (ndx >= numChars) {
                    ndx = numChars - 1;
                }
            }
            else {
                receivedChars[ndx] = '\0';      // terminate the string
                recvInProgress = false;
                ndx = 0;
                newData = true;
            }
        } 
        else if (rc == startMarker) {
            recvInProgress = true;
        }
    }
}

void processData() {
    if (newData) {
        Serial.print("RECV=");
        Serial.println(receivedChars);

        if (strcmp(receivedChars, "Laura") == 0) {
          ackLink();
        }

        if (strcmp(receivedChars, "TX") == 0) {
          setForceTX();
        }
        newData = false;
    }
}

void ackLink() {
  Serial.println("ACKOK");
  linkTimeout = millis() + linkACKinterval;
}

void setForceTX() {
  Serial.println("TXOK");
  forceTransmit = millis() + forceTXMinimumInterval;
}

void checkDTMF() {
  if (!DTMFReceivedNow && (millis() < lastRX + ledOnAfterRX)) { // recieving
    risingDTMF = digitalRead(PIN_DTMF_SIGNAL);

    if (risingDTMF && !lastRisingDTMF) {
      recievingDTMF = millis();
      delay(5);
      currentDTMFBinary = 0;
      currentDTMFBinary |= digitalRead(PIN_DTMF_Q4);
      currentDTMFBinary <<= 1;
      currentDTMFBinary |= digitalRead(PIN_DTMF_Q3);
      currentDTMFBinary <<= 1;
      currentDTMFBinary |= digitalRead(PIN_DTMF_Q2);
      currentDTMFBinary <<= 1;
      currentDTMFBinary |= digitalRead(PIN_DTMF_Q1);
      receivedDTMF[receivedDTMFCount++] = DTMFTable[currentDTMFBinary];
      receivedDTMF[receivedDTMFCount] = '\0';
    }
    lastRisingDTMF = risingDTMF;
  } else {
    if (receivedDTMF[0] != '\0') {
      DTMFReceivedNow = true;
    }
  }
}



// the loop function runs over and over again forever
void loop() {
  Serial.print("TX=");
  Serial.println((byte)isTransmitting());
  Serial.print("PTT=");
  Serial.println((byte)isPTTBTNPressed());
  Serial.print("RX=");
  Serial.println((byte)(millis() < lastRX + ledOnAfterRX));
  Serial.print("RXWAIT=");
  Serial.println((byte)(millis() < lastRX + ledFlashAfterRX));
  Serial.print("DTMFEN=");
  Serial.println(digitalRead(PIN_DTMF_SIGNAL));
  Serial.print("DTMF=");
  if (DTMFReceivedNow) {
    Serial.println(receivedDTMF);
    Serial.flush();
    DTMFReceivedNow = false;
    receivedDTMFCount = 0;
    receivedDTMF[0] = '\0';
  } else {
    Serial.println("X");
  }
  
  Serial.println("---");


  // here we create a non-blocking loop
  unsigned long at = millis() + serialReportInterval;

  while (millis() < at) {
    corrrectStartupLEDs();
    setTX(isPTTBTNPressed() || (millis() < forceTransmit));
    correctTransmittingLED();
    correctReceivingLED();
    correctLinkLED();
    recvWithStartEndMarkers();
    checkDTMF();
    processData();
  }
}
