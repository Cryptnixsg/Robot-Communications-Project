#include "mbed.h"
#include <chrono>

#define BUFFERSIZE 50

// Initialize serial communication with the PC and XBee router
BufferedSerial pc(USBTX, USBRX, 9600);
BufferedSerial router(PA_11, PA_12, 9600); // RX- PA_11, TX- PA_12

// Setup PWM output on PC_7
PwmOut servo(D9); // Connect your servo's control wire to pin PC_7
char buffer[BUFFERSIZE] = {0};

void setServoAngle(float angle) {
    // Convert the angle to a pulse width in microseconds (1000us to 2000us for 0 to 180 degrees)
    float pulseWidth = 0.001 + (angle / 180.0) * 0.001;
    servo.pulsewidth(pulseWidth);

    // Print debug message
    char debug_msg[80];
    int length = sprintf(debug_msg, "Setting angle to %.1f degrees, pulse width = %.1f us\n", angle, pulseWidth * 1e6);
    pc.write(debug_msg, length);
}

bool parseMessage() {
    // Check if there are any bytes available to read
    if (router.readable()) {
        // Read the incoming byte
        int bytes = router.read(buffer, BUFFERSIZE - 1); // Ensure space for null-terminator
        if (bytes > 0) {
            buffer[bytes] = '\0'; // Null-terminate the string
            printf("Message received from XBEE: %s\n", buffer); // Print received message

            int receivedValue = atoi(buffer); // Convert the received string to an integer
            if (receivedValue >= 0 && receivedValue <= 255) {
                printf("Potentiometer value received: %d\n", receivedValue); // Debug message
                float angle = (receivedValue / 255.0) * 180.0; // Convert the potentiometer value to an angle
                setServoAngle(angle);
                return true; // Message parsed successfully
            }
        }
    }
    return false; // No message or incorrect message parsed
}

int main() {
    // Set PWM period to 20ms (50Hz)
    servo.period(0.02f);
    char start_msg[] = "Starting servo control based on received signal\n";
    pc.write(start_msg, sizeof(start_msg) - 1);

    while (true) {
        parseMessage();
        ThisThread::sleep_for(chrono::milliseconds(100)); // Small delay to avoid excessive CPU usage
    }
}
