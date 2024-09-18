#include "mbed.h"
#include <stdio.h>
#include <string.h>

// Define the buffer size for the message
#define BUFFSIZE 50

// Initialize serial communication with the PC and XBee router
BufferedSerial pc(USBTX, USBRX, 9600);
BufferedSerial router(PA_11, PA_12, 9600); // TX- PA_11, RX- PA_12

// Setup Analog Input on A5 for receiving signal
AnalogIn pot(A5);
char xbeeBuff[BUFFSIZE] = {0}; // Adjusted buffer size for the message

void CreateMessage(char *xbeeMsg, uint16_t potValue) {
    // Message frame creation code here
    const char startByte = 0x7E;
    const char type = 0x10;  // Transmit Request
    const char frameID = 0x01;  // Frame ID
    const char destAdd1[] ={0x00, 0x13, 0xA2, 0x00, 0x42, 0x34, 0x74, 0x1B}; // Updated destination address
    const char destAdd2[] = {0xFF, 0xFE};  // 16-bit address (broadcast)
    const char broadcastRad = 0x00;  // Broadcast radius
    const char options = 0x00;  // Options

    char msg[4];
    sprintf(msg, "%03u", potValue); // Convert the potentiometer value to a string

    int msgLen = strlen(msg) + 14;
    char length[] = { (char)((msgLen >> 8) & 0xFF), (char)(msgLen & 0xFF) };

    uint16_t checksum = type + frameID;
    for (int i = 0; i < 8; i++) checksum += destAdd1[i];
    checksum += destAdd2[0] + destAdd2[1] + broadcastRad + options;
    for (int i = 0; i < strlen(msg); i++) checksum += (uint8_t)msg[i];
    checksum = 0xFF - (checksum & 0xFF);

    // Construct the message frame
    int offset = 0;
    xbeeMsg[offset++] = startByte;
    xbeeMsg[offset++] = length[0];
    xbeeMsg[offset++] = length[1];
    xbeeMsg[offset++] = type;
    xbeeMsg[offset++] = frameID;
    for (int i = 0; i < 8; i++) xbeeMsg[offset++] = destAdd1[i];
    xbeeMsg[offset++] = destAdd2[0];
    xbeeMsg[offset++] = destAdd2[1];
    xbeeMsg[offset++] = broadcastRad;
    xbeeMsg[offset++] = options;
    for (int i = 0; i < strlen(msg); i++) xbeeMsg[offset++] = msg[i];
    xbeeMsg[offset] = (char)checksum;
}

int main() {
    // Print destination address for debugging
    const char destAdd1[] = {0x00, 0x13, 0xA2, 0x00, 0x42, 0x34, 0x74, 0x1B}; 
    printf("Destination Address: ");
    for (int i = 0; i < 8; i++) {
        printf("%02X ", (unsigned char)destAdd1[i]);
    }
    printf("\n");

    while (true) {
        // Read potentiometer value
        uint16_t potValue = pot.read_u16() >> 8; // Convert to 8-bit value
        printf("Potentiometer Value: %u\n", potValue); // Debug message

        CreateMessage(xbeeBuff, potValue);

        router.write(xbeeBuff, 19 + 3); // message length is 3
        ThisThread::sleep_for(500ms);
    }
}
