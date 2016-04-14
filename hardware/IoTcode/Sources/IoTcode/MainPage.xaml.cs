﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.Media.SpeechSynthesis;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTcode
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //A class which wraps the color sensor
        TCS34725 colorSensor;
        //A SpeechSynthesizer class for text to speech operations
        SpeechSynthesizer synthesizer;
        //A MediaElement class for playing the audio
        MediaElement audio;
        //A GPIO pin for the pushbutton
        GpioPin buttonPin;
        //The GPIO pin number we want to use to control the pushbutton
        int gpioPin = 4;

        private string[] code = { "red", "red", "blue", "white", "yellow" };
        private int codeIndex = 0;
 
        public MainPage()
        {
            this.InitializeComponent();
            code[1] = code[3];
            code[3] = code[4];
            code[4] = code[0];
        }

        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            try
            {
                //Create a new object for the color sensor class
                colorSensor = new TCS34725();
                //Initialize the sensor
                await colorSensor.Initialize();

                //Create a new SpeechSynthesizer
                synthesizer = new SpeechSynthesizer();

                //Create a new MediaElement
                audio = new MediaElement();

                //Initialize the GPIO pin for the pushbutton
                InitializeGpio();

                await SayReady();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        //This method is used to initialize a GPIO pin
        private void InitializeGpio()
        {
            //Create a default GPIO controller
            GpioController gpioController = GpioController.GetDefault();
            //Use the controller to open the gpio pin of given number
            buttonPin = gpioController.OpenPin(gpioPin);
            //Debounce the pin to prevent unwanted button pressed events
            buttonPin.DebounceTimeout = new TimeSpan(1000);
            //Set the pin for input
            buttonPin.SetDriveMode(GpioPinDriveMode.Input);
            //Set a function callback in the event of a value change
            buttonPin.ValueChanged += buttonPin_ValueChanged;
        }

        //This method will be called everytime there is a change in the GPIO pin value
        private async void buttonPin_ValueChanged(object sender, GpioPinValueChangedEventArgs e)
        {
            //Only read the sensor value when the button is released
            if (e.Edge == GpioPinEdge.RisingEdge)
            {
                //Read the approximate color from the sensor
                string colorRead = await colorSensor.getClosestColor();
                //Output the colr name to the speaker
                await verifyCode(colorRead);
            }
        }

        private async Task verifyCode(string colorRead)
        {
            if (0 != string.Compare(colorRead, code[codeIndex], true))
            {
                codeIndex = 0;
                await SpeakColor(colorRead, "You have the wrong code");
                return;
            }

            if (++codeIndex == code.Length)
            {
                codeIndex = 0;
                await SpeakColor(colorRead, "You have the right code, congratulations." + GetFlag() + GetFlag());
                return;
            }

            await SpeakColor(colorRead);
        }

        private string GetFlag()
        {
            bool first = true;
            string flag = " The flag is: I, N, S, opening curly bracket, ";
            foreach (string color in code)
            {
                if (!first)
                    flag += ", _ ,";
                flag += color;
                first = false;
            }
            flag += ", closing curly bracket. ";
            return flag;
        }

        //This method is used to output a string to the speaker
        private async Task SpeakColor(string colorRead, string message = "")
        {
            await Speak("The color appears to be " + colorRead + ". " + message);
        }

        private async Task SayReady()
        {
            await Speak("I am ready, waiting for the code");
        }

        //This method is used to output a string to the speaker
        private async Task Speak(string message)
        {
            Debug.WriteLine(message);

            //Create a SpeechSynthesisStream using a string
            var stream = await synthesizer.SynthesizeTextToStreamAsync(message);
            //Use a dispatcher to play the audio
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Set the souce of the MediaElement to the SpeechSynthesisStream
                audio.SetSource(stream, stream.ContentType);
                //Play the stream
                audio.Play();
            });
        }
    }
}

