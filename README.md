# ScaryPumpkin
C# .NET 7 console app running on Raspberry Pi 2.x/3.x to make a Halloween decoration that plays spooky sounds and flashes lights when motion has been detected.

Code utilizes Fiodar Sazanavets' NetCoreAudio v1.7.0 package (https://www.nuget.org/packages/NetCoreAudio/1.7.0?_src=template) for MP3 playback. Developed for Parallax PIR proximity/motion sensor Rev. A (https://www.parallax.com/product/555-28027) oprating at 3.3v logic and output but any PIR sensor that oputs a GPIO HIGH value when motion detected shoudl work.
