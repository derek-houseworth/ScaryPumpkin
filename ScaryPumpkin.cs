using Microsoft.Extensions.Configuration;
using NetCoreAudio;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ScaryPumpkin;

class ScaryPumpkin
{

#if DEBUG
    private const string BUILD_TYPE = "DEBUG";
#else
    private const string BUILD_TYPE = "Release";
#endif

    //name of JSON file containing app settings key/value pairs, located in project root
    private const string APP_SETTINGS_FILE = "appsettings.json";

    //keys to retrieve values loaded from appsettings.json config file
    private const string LIGHT_SWITCH_PIN_CONFIG_KEY = "lightSwitchPin";
    private const string MOTION_SENSOR_PIN_CONFIG_KEY = "motionSensorPin";
    private const string SOUND_EFFECTS_PATH_CONFIG_KEY = "soundEffectsPath";
    private const string WELCOME_SOUND_CONFIG_KEY = "welcomeSound";

    //spooky sound effects play list
    private readonly static List<string> _soundEffectFileNames = [];
    private static string _soundEffectsPath = "";
    private static string _welcomeSound = "";

    //audio player
    private static Player _playerSpookySounds;

    private readonly static Random _random = new();

    private static string _appName = "";

    private static readonly DateTime _startTime = DateTime.Now;

    //PIR motion sensor
    private static int _motionSensorPin = 0;
    private static PIRSensor _pirSensor;

    //transistor to toggle light switch on/off
    private static int _lightSwitchPin = 0;
	private static GpioController _controller;

    static void Main()
    {
		_appName = GetAppName(typeof(ScaryPumpkin).GetTypeInfo().Assembly);
		DebugOutput("launching " + GetAppVersion(typeof(ScaryPumpkin).GetTypeInfo().Assembly));
		DebugOutput("press Ctrl+C to exit");

        //load settings from JSON file
		DebugOutput($"loading settings from {APP_SETTINGS_FILE} file...");
		if (!LoadSettingsFromJson(APP_SETTINGS_FILE))
        {
			DebugOutput($"error reading settings from {APP_SETTINGS_FILE}, shutting down");
			Environment.Exit(0);
		}

		//initialize motion detector object
		_pirSensor = new PIRSensor(_motionSensorPin);
		_pirSensor.MotionDetected += OnMotionDetected;
		
        //enumerate files in sound effects dir and load sound effects list
        InitializeSoundEffectsList();
        if (_soundEffectFileNames.Count == 0)
        {
            DebugOutput($"no sound effect files found in {_soundEffectsPath}, shutting down");
            Environment.Exit(0);
        }

        //initialize light switch GPIO pin
        if (!InitializeLightSwitch())
        {
            DebugOutput("error initializing GPIO controller, shutting down");
            Environment.Exit(0);
        }

        bool lightsOn = false;

        //initialize audio player
        _playerSpookySounds = new Player();
		_playerSpookySounds.PlaybackFinished += OnPlaybackFinished;

        //register handler function to respond to  Ctrl+C keypress event by freeing 
        //app resources and exiting
        Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
        {
            Shutdown();
            Environment.Exit(0);
        };

        //one-time playback of 'welcome' sound effect concludes app initialization
        PlaySoundEffectAsync(_soundEffectsPath + _welcomeSound);

        //main loop 
        while (true)
        {
            //blink lights at random intervals after PIR sensor has detected motion and 
            //while sound effect playback in progress 
            lightsOn = _playerSpookySounds.Playing ? !lightsOn : false;

            _controller.Write(_lightSwitchPin, lightsOn ? PinValue.High : PinValue.Low);
            int delayMs = 50;
            if (_playerSpookySounds.Playing && _random.Next(1, 10) > 5)
            {
                delayMs += _random.Next(20, 140);
            }
            Thread.Sleep(delayMs);

        } //main loop

    } //Main

    private static bool InitializeLightSwitch()
    {
        bool initialized = false;

        try
        {
            _controller = new GpioController();
            _controller.OpenPin(_lightSwitchPin, PinMode.Output);
            initialized = true;
        }
        catch (Exception ex)
        {
            DebugOutput("light switch " + ex.Message);
        }
        DebugOutput($"initialize light switch on pin {_lightSwitchPin:N0} " +
            (initialized ? "SUCCESS" : "ERROR"));
        return initialized;

    } //InitializeLightSwitch


    /// <summary>
    /// cleans up allocated resources at application exit
    /// </summary>
    private static void Shutdown()
    {
        DebugOutput("");
        _pirSensor?.Shutdown();
        _controller?.Dispose();
        DebugOutput("shutting down");

    } //Shutdown


    /// <summary>
    /// handler for PIRSensor object's MotionDetected event: begins playback of 
    /// random sound effect if none currently playing 
    /// </summary>
    public static void OnMotionDetected()
    {

        if (!_playerSpookySounds.Playing)
        {
            if (0 == _soundEffectFileNames.Count)
            {
                InitializeSoundEffectsList();
            }
            string currentSoundEffectFileName = _soundEffectFileNames[_random.Next(0, _soundEffectFileNames.Count - 1)];

            //remove selected sound from list: each sounds in list played once before list is reloaded
            _soundEffectFileNames.Remove(currentSoundEffectFileName);

            PlaySoundEffectAsync(currentSoundEffectFileName);
        }

    } //OnMotionDetected


    /// <summary>
    /// selects and plays random sound from sound effects list property
    /// </summary>
    private static async void PlaySoundEffectAsync(string soundEffectFileName)
    {

        await Task.Run(() =>
        {

            DebugOutput($"playing {soundEffectFileName[(soundEffectFileName.LastIndexOf('/') + 1)..]}... ");
            
			_playerSpookySounds.Play(soundEffectFileName);

        });

    } //PlayRandomSoundAsync


    /// <summary>
    /// stores names of all .MP3 files from sound effects directory in _soundEffectFileNames property
    /// </summary>
    private static void InitializeSoundEffectsList()
    {
        try
        {
            foreach (string fileName in Directory.EnumerateFiles(_soundEffectsPath, "*.mp3", SearchOption.AllDirectories))
            {
                _soundEffectFileNames.Add(fileName);
            }

            DebugOutput($"found {_soundEffectFileNames.Count} sound effect files in {_soundEffectsPath}");

        }
        catch (Exception e)
        {
            DebugOutput(e.Message);
        }

    } //InitializeSoundEffectsList


    /// <summary>
    /// handler for Player's PlaybackFinished event 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void OnPlaybackFinished(object sender, EventArgs e)
    {
        DebugOutput("done!");

        if (!_pirSensor.IsRunning)
        {
            _pirSensor.IsRunning = true;
            Task.Delay(3000).Wait();
        }

    } //OnPlaybackFinished


    /// <summary>
    /// gets application name, version & build type retrieved from 
    /// assembly specified by appAsm parameter
    /// </summary>
    /// <param name="appAsm"></param>
    /// <returns>string containing application name, version and build type for specified assembly</returns>
    private static string GetAppVersion(Assembly appAsm)
    {
        int start = appAsm.FullName.IndexOf('=') + 1;
        int end = appAsm.FullName.IndexOf(',', start);
        string version = appAsm.FullName[start..end];
        return $"{version} {BUILD_TYPE}";

    } //GetAppVersion


    /// <summary>
    /// returns string containing application name retrieved from 
    /// assembly specified by appAsm parameter
    /// </summary>
    /// <param name="appAsm"></param>
    /// <returns></returns>
    public static string GetAppName(Assembly appAsm)
    {
        return appAsm.FullName[..appAsm.FullName.IndexOf(',')];

    } //GetAppName


    /// <summary>
    /// writes current system time, app name and message string parameter value to console
    /// </summary>
    /// <param name="message"></param>
    private static void DebugOutput(string message)
    {

        TimeSpan elapsed = DateTime.Now - _startTime;
        Console.WriteLine($"{elapsed:hh\\:mm\\:ss} {_appName}: {message}");

	} //DebugOutput


    // loads app settings values from JSON format file specified in jsonSettingsFileName parameter
    // example file format: 
    //  {
    //      "motionSensorPin": 18,
    //      "lightSwitchPin": 26,
    //      "soundEffectsPath": "./SoundEffects/",
    //      "welcomeSound": "welcome-01.mp3"
    //  }
    private static bool LoadSettingsFromJson(string jsonSettingsFileName)
    {
        if (!File.Exists(jsonSettingsFileName))
        {
            return false;
        }

        try
        {
            // load config settings from appsettings.json file:
            IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddJsonFile(jsonSettingsFileName, false, true)
            .Build();
		
            _motionSensorPin = Convert.ToInt16(configRoot[MOTION_SENSOR_PIN_CONFIG_KEY]);
			_lightSwitchPin = Convert.ToInt16(configRoot[LIGHT_SWITCH_PIN_CONFIG_KEY]);
			_soundEffectsPath = configRoot[SOUND_EFFECTS_PATH_CONFIG_KEY];
			_welcomeSound = configRoot[WELCOME_SOUND_CONFIG_KEY];
		}
		catch (Exception)
		{
            return false;
        }

		return true;

	} //LoadSettingsFromJson

} //ScaryPumpkin