using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;
using System.Media;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CognitiveLoadMeasure
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectorHub.ConnectorHub myConnectorHub;
        /// <summary>
        /// Timer that calls the Sound play method;
        /// </summary>
        System.Timers.Timer InterventionTimer;
        /// <summary>
        /// Time after which to call the interval passed event from <see cref="InterventionTimer"/>
        /// </summary>
        private int TimeSpan = 0;
        /// <summary>
        /// random number generator
        /// </summary>
        Random rand;
        /// <summary>
        /// global keyboard hook generator for listening to keyboard events even when the application is not in focus
        /// </summary>
        ButtonHook buttonhook;

        public MainWindow()
        {
            InitializeComponent();
            //init sound player
            InitSoundPlayer();
            //init learing hub
            initLearningHub();
            //init button hook and subscribe to the key pressed event
            buttonhook = new ButtonHook();
            buttonhook.RegisterHotKey();
            buttonhook.KeyPressed += Bhook_KeyPressed;
            //init InterventionTimer
            //generate a random interval
            InterventionTimer = new System.Timers.Timer();
            InterventionTimer.AutoReset = false;
            InterventionTimer.Elapsed += InterventionTimer_Elapsed;
            
        }

        /// <summary>
        /// Method that starts <see cref="InterventionTimer"/>
        /// </summary>
        /// <param name="randomDuration">The time to wait before the elapsed event is thrown.</param>
        private void StartTimer(int randomDuration)
        {
            InterventionTimer.Interval = randomDuration;
            InterventionTimer.Start();
        }

        /// <summary>
        /// Stop <see cref="InterventionTimer"/> From calling any more methods
        /// </summary>
        private void StopTimer()
        {
            Debug.WriteLine("TimerStopped");
            InterventionTimer.Stop();
        }

        /// <summary>
        /// True when the sound is playing
        /// </summary>
        private bool _IsRinging = false;
        /// <summary>
        /// Handler for <see cref="buttonhook"/> instance.Button pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bhook_KeyPressed(object sender, EventArgs e)
        {
            Debug.WriteLine("hookedKeydown");
            //if the sound is playing
            if (_IsRinging)
            {
                //calculate the time required for the user to press the button after the sound started playing
                _timeTakenToReact = (DateTime.Now - ReactionTime).TotalMilliseconds;
                if (IsRecording)
                {
                    SendDataAsync();
                }
                Debug.WriteLine("Time taken to react: " + _timeTakenToReact);
                StopAudio();
                TimeSpan = rand.Next(1000, 3000);
                //init the timer with the random interval
                StartTimer(TimeSpan);
            }

        }
        /// <summary>
        /// handler for when the time is elapsed for <see cref="InterventionTimer"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterventionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Beep Beep");
            //Console.Beep(2500, 5000);
            PlayAudio();
            Debug.WriteLine("Beep wat?");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start Button Clicked");
            StartButton.Background = new SolidColorBrush(Colors.Red);
            StartRecordingData();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop Button Clicked");
            StartButton.Background = new SolidColorBrush(Colors.White);
            StopRecordingData();
        }

        /// <summary>
        /// When overridden with hooked keys this is not thrown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                Debug.WriteLine("LocalKeyDown");
                Debug.WriteLine("Space button clicked");
                StopAudio();
                TimeSpan = rand.Next(1000, 3000);

                //init the timer with the random interval
                StartTimer(TimeSpan);
            }
        }

        #region HandleAudio
        SoundPlayer player;
        /// <summary>
        /// returns the bin folder in the directory
        /// </summary>
        string directory = Environment.CurrentDirectory;
        private void InitSoundPlayer()
        {
            player = new SoundPlayer(directory + @"/Audio/beep.wav");
            player.LoadAsync();
        }
        /// <summary>
        /// plays audio file and sets <see cref="ReactionTime"/> to when the audio started playing
        /// </summary>
        private void PlayAudio()
        {
            ReactionTime = DateTime.Now;
            player.PlayLooping();
            _IsRinging = true;
        }
        /// <summary>
        /// Stops playing audio
        /// </summary>
        private void StopAudio()
        {
            player.Stop();
            _IsRinging = false;
        }

        #endregion


        #region Send data
        private DateTime _reactionTime = DateTime.Now;
        public DateTime ReactionTime{
            get { return _reactionTime; }
            set
            {
                _reactionTime = value;
            }
        }
        private double _timeTakenToReact = 0;

        private bool IsRecording = false;

        private async void initLearningHub()
        {
            await Task.Run(() =>
            {
                myConnectorHub = new ConnectorHub.ConnectorHub();
                myConnectorHub.Init();
                myConnectorHub.SendReady();
                myConnectorHub.StartRecordingEvent += MyConnectorHub_startRecordingEvent;
                myConnectorHub.StopRecordingEvent += MyConnectorHub_stopRecordingEvent;
                SetValueNames();
            });
        }

        private void MyConnectorHub_stopRecordingEvent(object sender)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
                         () =>
                         {
                             StartButton.Background = new SolidColorBrush(Colors.White);
                         }));
            StopRecordingData();

        }

        private void MyConnectorHub_startRecordingEvent(object sender)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
             () =>
             {
                 StartButton.Background = new SolidColorBrush(Colors.Red);
             }));
            StartRecordingData();
        }

        /// <summary>
        /// Calls the <see cref="LearningHubManager"/>'argsStroke SetValueNames method which assigns the variables names for storing
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            names.Add("ReactionTime");
            myConnectorHub.SetValuesName(names);
        }
        /// <summary>
        /// For calling the <see cref="SendData(StylusEventArgs, StylusPoint)"/> async
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        public async void SendDataAsync()
        {
            await Task.Run(() => SendData());
        }
        /// <summary>
        /// Method for sending data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        private void SendData()
        {
            List<string> values = new List<string>();
            values.Add(_timeTakenToReact.ToString());
              myConnectorHub.StoreFrame(values);
            //globals.Speech.SpeakAsync("Student Data sent");
        }

        /// <summary>
        /// Start Recording data
        /// </summary>
        private void StartRecordingData()
        {
            if (IsRecording == false)
            {

                rand = new Random();
                TimeSpan = rand.Next(1000, 3000);
                //init the timer with the random interval
                StartTimer(TimeSpan);
                this.IsRecording = true;
            }
        }
        /// <summary>
        /// Stop recording data
        /// </summary>
        private void StopRecordingData()
        {
            if (this.IsRecording == true)
            {
                //stop playing the audio
                StopAudio();
                this.IsRecording = false;
            }
        }
        #endregion


    }
}
