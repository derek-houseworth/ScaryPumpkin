using NetCoreAudio;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
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

    //spooky sound effects play list
    private const string SOUND_EFFECTS_PATH = "./SoundEffects/";
    //private readonly static ObservableCollection<string> _soundEffectFileNames = new();
    private readonly static List<string> _soundEffectFileNames = new();

    private static Player _playerSpookySounds;

    private readonly static Random _random = new();

    private static string _appName;

	private static readonly Stopwatch _stopwatch = new();
    private static readonly DateTime _startTime = DateTime.Now;

	//LEDs
	const int LIGHT_SWITCH_PIN = 26;

    //PIR motion sensor
    const int MOTION_SENSOR_PIN = 18;
    private static PIRSensor _pirSensor;

    //GPIO controller for light switch 
    private static GpioController _controller;

    static void Main(string[] args)
    {
		_appName = GetAppName(typeof(ScaryPumpkin).GetTypeInfo().Assembly);

		DebugOutput("launching " + GetAppVersion(typeof(ScaryPumpkin).GetTypeInfo().Assembly));
        DebugOutput("Press Ctrl+C to exit");

        InitializeSoundEffectsList();

        InitializeLightSwitch();

        bool lightsOn = false;
        
        //initiaize audo player
        _playerSpookySounds = new Player();
        _playerSpookySounds.PlaybackFinished += OnPlaybackFinished;

        //handle Ctrl+C keypress event to cleanup app resouces and exit
        Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
        {
            Shutdown();
            Environment.Exit(0);
        };

        //initialize motion detector object
        _pirSensor = new PIRSensor(MOTION_SENSOR_PIN);
        _pirSensor.MotionDetected += OnMotionDetected;

        //one-time playback of 'welcome' sound effect concludes app initialization
        PlaySoundEffectAsync("./SoundEffects/welcome-01.mp3");

        //main loop 
        while (true)
        {
            //blink lights at random intervals after PIR sensor has detected motion and 
            //while sound effect playback in progress 
            lightsOn = _playerSpookySounds.Playing ? !lightsOn : false;

            _controller.Write(LIGHT_SWITCH_PIN, lightsOn ? PinValue.High : PinValue.Low);
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
            _controller.OpenPin(LIGHT_SWITCH_PIN, PinMode.Output);
            initialized = true;
        }
        catch (Exception ex)
        {
            DebugOutput("light switch " + ex.Message);
        }
        DebugOutput($"initiaize light switch on pin {LIGHT_SWITCH_PIN:N0} " + 
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
    /// hanlder for PIRSensor object's MotionDetected event: begins playback of 
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
            foreach (string fileName in Directory.EnumerateFiles(SOUND_EFFECTS_PATH, "*.mp3", SearchOption.AllDirectories))
            {
                _soundEffectFileNames.Add(fileName);
            }

            DebugOutput($"found {_soundEffectFileNames.Count} sound effect files in {SOUND_EFFECTS_PATH}");

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
    /// assembly specified by appAsmp parameter
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
    /// assembly specified by appAsmp parameter
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
		Console.WriteLine($"{elapsed.ToString(@"hh\:mm\:ss")} {_appName}: {message}");

	} //DebugOutput


} //ScaryPumpkin