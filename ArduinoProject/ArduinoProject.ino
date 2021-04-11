#include <Wire.h>
#include <LiquidCrystal_I2C.h>

LiquidCrystal_I2C lcd(0x27, 20, 4);
String inData;
int pipeCount = 0;

void setup() {
  Serial.begin(9600);
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0, 0);
}

void loop() {
  while (Serial.available() > 0) {
    char received = Serial.read();
	
    if (received == '*') {
      received = char(223); // degrees	
    }
    
    inData += received;

    if (received == '|') {
      // message separator
      pipeCount++;
      inData.remove(inData.length() - 1, 1);
      
      lcd.setCursor(0, pipeCount - 1);
      lcd.print(inData);
      
      inData = "";
    } else if (received == '#') {
      // end of message
      pipeCount = 0;
      lcd.setCursor(0, 0);
      
      inData = "";
    }
  }
}
