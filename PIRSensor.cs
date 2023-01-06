using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace ScaryPumpkin
{
    public class PIRSensor
    {

        //example of compatible PIR motion sensor:
        //Parallax PIR proximity/motion sensor Rev. A
        //https://www.parallax.com/product/555-28027

        public string Name;

        private CancellationTokenSource _cancellationTokenSource;

        //true after successful initialization of sensor
        public bool Initialized { get; internal set; }

        //current status of sensor: true if motion is being detected, false otherwise
        public bool DetectingMotion { get; private set; } = false;

        public delegate void MotionDectedHandler();
        public event MotionDectedHandler MotionDetected;

        //number of GPIO pin for sensor input
        public int Pin { get; }
        private GpioController _controller;

        /// <summary>
        /// calling application code sets IsRunning to true to start listening for signal from sensor, false to stop
        /// </summary>
        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    if (MotionDetected is null)
                    {
                        throw new ArgumentException($"{Name}'s MotionDetected event handler has not been set");
                    }
                    _isRunning = value;
                    if (IsRunning)
                    {
                        Start();
                    }
                    else if (_cancellationTokenSource is not null)
                    {
                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            Console.WriteLine($"{Name}: stopping");
                            _cancellationTokenSource?.Cancel();
                        }
                    }

                }
            }

        } //IsRunning


        /// <summary>
        /// attempts to instantiate GPIO controller object and set mode of sensor pin
        /// </summary>
        private void InitializeController()
        {
            Initialized = false;
            try
            {
                _controller = new GpioController();
                if (_controller is not null)
                {
                    _controller.OpenPin(Pin, PinMode.Output);
                    _controller.Write(Pin, PinValue.Low);
                    _controller.SetPinMode(Pin, PinMode.Input);
                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: {ex.Message}");
            }

            Console.WriteLine($"{Name}: initialize sensor on pin {Pin:N0} {(Initialized ? "SUCCESS" : "ERROR")}");
        }


        /// <summary>
        /// initialize GPIO controller if necessary and start task to listen for sensor output
        /// </summary>
        private async void Start()
        {
            if (_controller is null)
            {
                InitializeController();
            }
            try
            {
                using (_cancellationTokenSource = new())
                {
                    await ReadSensor(_cancellationTokenSource.Token);
                }
            }
            catch (TaskCanceledException tce)
            {
                Console.WriteLine($"{Name}: {tce.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource = null;
            }

        } //Start


        /// <summary>
        /// free allocated program resources, called once before app exit
        /// </summary>
        public void Shutdown()
        {

            IsRunning = false;
            Initialized = false;
            Console.WriteLine($"{Name}: shutting down");
            _controller?.Dispose();

        } //Shutdown


        /// <summary>
        /// cancelable task to monitor sensor input pin for sensor to signal motion by outputting high signal 
        /// MotionDetected handler called when pin value indicates motion detected
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private Task ReadSensor(CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"{Name}: now listening...");
                while (true)
                {
                    bool currentReading = (_controller.Read(Pin) == PinValue.High);
                    if (currentReading != DetectingMotion)
                    {
                        DetectingMotion = currentReading;
                        if (DetectingMotion)
                        {
                            MotionDetected?.Invoke();
                        }
                    }
                    Thread.Sleep(150);

                    cancelToken.ThrowIfCancellationRequested();
                }
            }, cancelToken);

        } //ReadSensor


        /// <summary>
        /// sensor object constructor 
        /// </summary>
        /// <param name="pin">GPIO pin to read input connected to sensor output</param>
        /// <param name="name">string representing name of sensor</param>
        public PIRSensor(int pin = 18, string name = null)
        {
            Pin = pin;
            IsRunning = false;
            Name = name ?? "PIR Sensor";


        } //PIRSensor

    } //PIRSensor

} //BotUtil