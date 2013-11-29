wp8-heatpumpcontrol
===================

Windows Phone 8 application to control split-unit heat pumps through the Internet, by using an Arduino board as the server to send the infrared commands.

The supported heat pumps at the moment are:
* Panasonic E9/E12-CKP and E9/E12-DKE (Panasonic remote control P/N A75C2295 and P/N A75C2616)
* Midea MSR1-12HRN1-QC2 + MOA1-12HN1-QC2, sold as Ultimate Pro Plus Basic 13FP in Finland (Midea remote control P/N RG51M1/E)

Also, at the moment I only have Finnish text on the UI of the application. Perhaps you would like to contribute?

For the Arduino server, see https://github.com/ToniA/arduino-wp-heatpump-controller

To build this application
=========================

You need
* A 'developer unlocked' Windows Phone 8 device
* Visual Studio 2012 Express (free, but requires registration)

The solution does not include NuGet packages 'SSH.NET' and 'WPtoolkit'. To restore these on the first build:
* Right-click the solution on the Solution Explorer, and select 'Enable Nuget Package Restore'
* Restore the packages on Tools -> Library Package Manager -> Package Manager Console. It should say 'Some NuGet package are missing...'. Click 'Restore'
* You should now be able to build the solution

To use the application
======================

* Connect your Arduino into your home network
    * See the instructions on the arduino-wp-heatpump-controller repository
* Connect your Windows Phone into your home network
* Start the application, it should automatically find your Arduino controller
    * Each controller is identified by its MAC address
	* Give a name and a heatpump type to the controller
	* Use the app to send commands to the heatpump

Remote control over the Internet
================================

The application supports remote control over the Internet, using the SSH protocol. You must first find and configure your controllers in your home network, though.
* In the settings page, you need to configure the 'standard' SSH settings. You can use either a password or a key
* For sending commands over the Internet:
    * Using a Dynamic DNS provider will easy up your life :)
    * You need to have a way to connect to a machine running the sshd (firewall hole etc)
	* You need to have a way to command the machine to send a UDP broadcast message in your home network
	* I'm using 'socat' directly on my Linksys WRT-54GL with the DD-WRT firmware, that's the stock example on the settings