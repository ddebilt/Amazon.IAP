Amazon.IAP
==========

C# Xamarin.Android bindings for Amazon's IAP SDK, as well as a close port of Amazon's ButtonClicker sample IAP application. It has not been optimized or refactored with C#.


Setup
=====
A high level overview can be found at:
https://developer.amazon.com/sdk/in-app-purchasing.html

Step 1:
-- Ensure you are not in USB mode (manually disconnect your Kindle with the "disconnect" button, but keep the the device connected via USB).

Step 2:
-- copy External/amazon.sdktester.json to the device via the following command: adb push C:\...\amazon.sdktester.json /mnt/sdcard

Step 3:
-- Load the AmazonSDKTester.apk onto the Kindle, via the following command: adb install C:\...\AmazonSDKTester.apk


