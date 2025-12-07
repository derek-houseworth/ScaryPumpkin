# ScaryPumpkin
A Microsoft .NET 10 console app written in C# and that enables a jack-o-lantern Halloween decoration to play spooky 
sounds and flash lights when motion has been detected. Primarily developed for Raspberry Pi 2.x/3.x and Raspberry Pi OS 
but should work with minimal modification on any .NET compatible device that has GPIO capability.

Code utilizes Fiodar Sazanavets' NetCoreAudio v1.7.0 package (https://www.nuget.org/packages/NetCoreAudio/1.7.0?_src=template) 
for MP3 playback. 

Developed for use with Parallax PIR proximity/motion sensor Rev. A (https://www.parallax.com/product/555-28027) oprating at 3.3v logic 
and output but any PIR sensor that oputs a GPIO HIGH value when motion detected should work.

Light flashing capability assumes a digital switch connected to a Raspberry Pi GPIO pin where a PinValue.High written to the 
turns the swich on and PinValue.Low turns the switch off. Could also be managed with a PWM controlled switch intended for 
radio control model applications like https://www.amazon.com/gp/product/B08FLZXSD7/ref=ox_sc_saved_image_4?smid=A1JTH8JAMM4IYJ&th=1

Setup:
* install .NET 10 on Raspberry Pi: curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel Current
* get local copy of ScaryPumpkin source files: sudo git clone https://github.com/derek-houseworth/ScaryPumpkin
* change owenership of ScaryPumpkin source folder to enable build: sudo chown -R <Rasberry Pi username, e.g. "pi"> ScaryPumpkin
* make source directory current: cd ScaryPumpkin
* build & run debug: dotnet run
* build & run release: dotnet run -c Release