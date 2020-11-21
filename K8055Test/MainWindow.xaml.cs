using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;

namespace K8055Test
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private bool _digitalOutputTest;
        private bool _isConnected;

        private bool _lightSwitch;
        private bool _sofaSwitch;
        private bool _hoseSwitch;
        private bool _onOff;
        private bool _doDayLightCycle;
        private bool _varyTemp;
        private bool _isolate;
        private bool _blindSwitch;
        private int _openBlinds;//O quao abertas estão as persianas
        private bool _hosingTime;//è tempo de rega?

        private int HorasDia = 85;//8 da manhã
        private int HorasNoite = 212;//20 h da noite

        public MainWindow()
        {
            InitializeComponent();

            _timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            _timer.Tick += TimerTick;
        }

        /// <summary>
        /// Handles click events of UI elements.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void K8055ButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "K8055ConnectButton":
                    {
                        bool sk5 = K8055SK5Checkbox.IsChecked ?? false;
                        bool sk6 = K8055SK6Checkbox.IsChecked ?? false;

                        //Resolves correct deviceaddress from jumper placing.
                        int deviceAdress = Convert.ToInt32(!sk6) << 1 | Convert.ToInt32(!sk5);

                        //Falls die Verbindug fehlgeschlagen hat, soll aus der Methode gebrochen werden.
                        if (K8055.OpenDevice(deviceAdress) != deviceAdress)
                            return;

                        _isConnected = true;
                        K8055ConnectLabel.Content = $"Connected to {deviceAdress}";
                        _timer.Start();
                        break;
                    }

                case "K8055DisconnectButton":
                    {
                        if (_isConnected)
                        {
                            K8055.CloseDevice();
                            _isConnected = false;
                            K8055ConnectLabel.Content = "Disconnected";
                            _timer.Stop();
                        }
                        break;
                    }

                case "K8055SetAllDigitalButton":
                    {
                        foreach (CheckBox checkBox in K8055DigitalOutputCanvas.Children.OfType<CheckBox>())
                        {
                            checkBox.IsChecked = true;
                        }
                        break;
                    }

                case "K8055ClearAllDigitalButton":
                    {
                        foreach (CheckBox checkBox in K8055DigitalOutputCanvas.Children.OfType<CheckBox>())
                        {
                            checkBox.IsChecked = false;
                        }
                        break;
                    }

                case "K8055SetAllAnalogButton":
                    {
                        K8055.SetAllAnalog();
                        K8055AnalogOutputSlider1.Value = 255;
                        K8055AnalogOutputSlider2.Value = 255;
                        break;
                    }

                case "K8055ClearAllAnalogButton":
                    {
                        K8055.ClearAllAnalog();
                        K8055AnalogOutputSlider1.Value = 0;
                        K8055AnalogOutputSlider2.Value = 0;
                        break;
                    }

                case "K8055OutputTestButton":
                    {
                        _digitalOutputTest = !_digitalOutputTest;
                        if (_digitalOutputTest)
                        {
                            Thread thread = new Thread(K8055DigitalOutputTest);
                            thread.Start();
                        }
                        break;
                    }

                //THE NEW CODE GOES HERE
                //----------------------------------------------------------------------------------------
                case "dinner":
                    {
                        //Luz jantar (digital in 1)
                        // -> Liga luzes sala jantar (digital out 1)
                        // -> Liga luzes Cozinha (digital out 2)
                        Thread thread = new Thread(LightToggle);
                        thread.Start();
                        break;
                    }
                case "sofa":
                    {
                        //Sofá (digital in 2)
                        // -> Liga luzes sala jantar (digital out 3)
                        // -> Liga luzes Cozinha (digital out 4)
                        Thread thread = new Thread(SofaRemote);
                        thread.Start();
                        break;
                    }
                case "temp":
                    {
                        //Temperatura (analog in 2)
                        // -> Ar condicionado (digital out 5)
                        // -> Persianas (analog out 1)
                        Thread thread = new Thread(RandTemperature);
                        thread.Start();

                        break;
                    }
                case "blinds":
                    {
                        //Blinds (digital in 3)
                        // -> Persianas (analog out 1)
                        Thread thread = new Thread(HomeBlinds);
                        thread.Start();
                        break;
                    }
                case "hose":
                    {
                        //Rega (digital in 4)
                        // -> Aspressor (digital out 6)
                        Thread thread = new Thread(HoseRegulation);
                        thread.Start();
                        break;
                    }
                case "onOff":
                    {
                        // ON/OFF (digital in 5)

                        Thread threadOn = new Thread(OnOffCreation);
                        threadOn.Start();
                        break;
                    }
                //Utilities for demonstration below
                case "dayNight":
                    {
                        _doDayLightCycle = !_doDayLightCycle;
                        if (_doDayLightCycle)
                        {
                            Thread thread = new Thread(AnalogOutputCycle);
                            thread.Start();
                        }
                        break;
                    }
                case "varyTemp":
                    {
                        _varyTemp = !_varyTemp;
                        if (_varyTemp)
                        {
                            Thread thread = new Thread(VaryTemp);
                            thread.Start();
                        }
                        break;
                    }
            }
        }


        /// <summary>
        /// Updates UI elements with the values of the K8055 periodically.
        /// </summary>
        private void TimerTick(object sender, EventArgs e)
        {
            if (!_isConnected) { return; }

            foreach (CheckBox checkBox in K8055DigitalInputCanvas.Children.OfType<CheckBox>())
            {
                checkBox.IsChecked = K8055.ReadDigitalChannel(Convert.ToInt32(checkBox.Content));
            }

            foreach (ProgressBar progressBar in K8055AnalogInputCanvas.Children.OfType<ProgressBar>())
            {
                progressBar.Value = K8055.ReadAnalogChannel(int.Parse(progressBar.Name[progressBar.Name.Length - 1].ToString()));
            }

            K8055Counter1TextBox.Text = K8055.ReadCounter(1).ToString();
            K8055Counter2TextBox.Text = K8055.ReadCounter(2).ToString();
        }

        /// <summary>
        /// Updates the analog output of the K8055 with the new output slider values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void K8055AnalogOutputSliderValueChanged(object sender, RoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            K8055.OutputAnalogChannel(int.Parse(slider.Name[slider.Name.Length - 1].ToString()), Convert.ToInt32(slider.Value));
        }

        /// <summary>
        /// Once a digital output checkbox state changes to "checked", the K8055 digital output is set. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void K8055OutputCheckboxChecked(object sender, RoutedEventArgs e)
        {
            K8055.SetDigitalChannel(Convert.ToInt32(((CheckBox)sender).Content));
        }

        /// <summary>
        /// Once a digital output checkbox state changes to "unchecked", the K8055 digital output is cleared. 
        /// </summary>
        private void K8055OutputCheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            K8055.ClearDigitalChannel(Convert.ToInt32(((CheckBox)sender).Content));
        }

        /// <summary>
        /// The digital outputs are sequentially switched on and off until the digital output test checkbox is unchecked.
        /// </summary>
        private void K8055DigitalOutputTest()
        {
            while (_digitalOutputTest)
            {
                for (int i = 1; i < 9; i++)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        K8055ButtonClick(new Button { Name = "K8055ClearAllDigitalButton" }, null);
                        ((CheckBox)K8055DigitalOutputCanvas.Children[i - 1]).IsChecked = true;
                    }));
                    Thread.Sleep(100);
                }

                K8055.ClearDigitalChannel(8);
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055DigitalOutputCheckbox8.IsChecked = false;
                }));
            }
        }

        /// <summary>
        /// Once the MainWindow closes _digitalOutputTest is set to "false" in order to terminate the "output test" thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            _digitalOutputTest = false;
            _doDayLightCycle = false;
            _lightSwitch = false;
            _sofaSwitch = false;
            _hoseSwitch = false;
            _onOff = false;
            _varyTemp = false;
            _isolate = false;
            _blindSwitch = false;
            _hosingTime = false;

            Application.Current.Shutdown();
        }

        /// <summary>
        /// The selected counter is reset.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void K8055CounterResetButtonClick(object sender, RoutedEventArgs e)
        {
            K8055.ResetCounter(int.Parse(((Button)sender).Name[12].ToString()));
        }

        /// <summary>
        /// Sets the debounce time of the selected counter to the entered value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void K8055SetDebounceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                int counter = int.Parse(((Button)sender).Name[16].ToString());
                int milliseconds = counter == 1 ? int.Parse(K8055SetDebounce1TextBox.Text)
                                                : int.Parse(K8055SetDebounce2TextBox.Text);

                K8055.SetCounterDebounceTime(counter, milliseconds);
            }
            catch
            {
                //ignored
            }
        }

        //NEW CODE IS BELOW HERE
        //--------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Asists with the passing of time.
        /// To simulate real life conditions.
        /// </summary>
        private void AnalogOutputCycle()
        {
            while (_doDayLightCycle)
            {
                for (int i = 1; i < 255; i++)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        K8055.SetAnalogInputChannel(1, i);
                    }));
                    Thread.Sleep(100);

                    if (!_doDayLightCycle)
                    {
                        break;
                    }
                }

                if (_doDayLightCycle)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        K8055.SetAnalogInputChannel(1, 0);
                    }));
                }
            }
        }

        /// <summary>
        /// Asists with the random variation of temperature
        /// To simulate real life conditions.
        /// </summary>
        private void VaryTemp()
        {
            var rand = new Random();

            while (_varyTemp)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055.SetAnalogInputChannel(2, K8055.ReadAnalogChannel(2)
                                                        + rand.Next(-3, 4));
                }));
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Designates the behaviour of the light system.
        /// </summary>
        private void LightToggle()
        {
            //Luz jantar (digital in 1)
            // -> Liga luzes sala jantar (digital out 1)
            // -> Liga luzes Cozinha (digital out 2)
            _lightSwitch = !_lightSwitch;

            while (_lightSwitch)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055.SetDigitalInputChannel(1, _lightSwitch);
                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = _lightSwitch;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = _lightSwitch;
                    }
                }));
                Thread.Sleep(100);
            }

            try
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = _lightSwitch;
                ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = _lightSwitch;
            }
            catch (Exception)
            {
                //ignore
            }
            
        }

        /// <summary>
        /// Designates the behaviour of the Sofa system.
        /// </summary>
        private void SofaRemote()
        {
            //Sofá (digital in 2)
            // -> Liga luzes sala jantar (digital out 3)
            // -> Liga luzes Cozinha (digital out 4)
            _sofaSwitch = !_sofaSwitch;

            while (_sofaSwitch)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055.SetDigitalInputChannel(2, _sofaSwitch);

                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[2]).IsChecked = _sofaSwitch;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[3]).IsChecked = _sofaSwitch;
                    }
                }));
                Thread.Sleep(100);
            }

            try
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[2]).IsChecked = _sofaSwitch;
                ((CheckBox)K8055DigitalOutputCanvas.Children[3]).IsChecked = _sofaSwitch;
            }
            catch (Exception)
            {
                //ignore
            }
            
        }

        /// <summary>
        /// Designates the behaviour of the Hose system.
        /// </summary>
        private void HomeBlinds()
        {
            //Blinds (digital in 3)
            // -> Persianas (analog out 1)
            _blindSwitch = !_blindSwitch;

            K8055.SetDigitalInputChannel(3, _blindSwitch);

            while (_blindSwitch)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    int data = _isolate
                        ? Math.Max(_openBlinds - 20, 0)
                        : _openBlinds;

                    K8055.OutputAnalogChannel(1, data);
                }));
                Thread.Sleep(100);
            }
            K8055.OutputAnalogChannel(1, 0);
        }

        /// <summary>
        /// Designates the behaviour of the Hose system.
        /// </summary>
        private void HoseRegulation()
        {
            //Rega (digital in 4)
            // -> Aspressor (digital out 6)
            _hoseSwitch = !_hoseSwitch;

            K8055.SetDigitalInputChannel(4, _hoseSwitch);

            while (_hoseSwitch)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    ((CheckBox)K8055DigitalOutputCanvas.Children[5]).IsChecked = _onOff && _hosingTime
                        ? true
                        : false;
                }));
                Thread.Sleep(100);
            }
            try
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[5]).IsChecked = false;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// Designates the behaviour of the time.
        /// </summary>
        private void TimeControl()
        {
            //Hora do Dia (analog in 1)

            // -> Persianas (analog out 1)

            // -> Aspressor (digital out 6)

            // -> Água Banho TEMPERATURA (analog out 2)
            // -> Maquina café (digital out 7)
            // -> Radio (digital out 8)

            while (_onOff)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    var time = K8055.ReadAnalogChannel(1);
                    // NUM DEFENIDO POR FUNÇÂO
                    _openBlinds = HorasDia < time && time < HorasNoite
                        ? time
                        : 0;

                    // È TEMPO OU NÂO
                    _hosingTime = Math.Max(HorasNoite - 10, 0) < time && time < Math.Min(HorasNoite + 10, 255);

                    SetWaterTemp();
                    CheckWakeUp();
                }));
                Thread.Sleep(100);
            }
        }

        private void SetWaterTemp()
        {
            var time = K8055.ReadAnalogChannel(1);
            var temp = K8055.ReadAnalogChannel(2);

            int watertemp = Math.Max(HorasDia - 10, 0) < time && time < Math.Min(HorasDia + 10, 255)
                ? 255 - temp
                : 0;
            K8055.OutputAnalogChannel(2, watertemp);
        }

        /// <summary>
        /// Turns on boiler.
        /// Turns on coffemachine.
        /// Turns Radio on.
        /// </summary>
        private void CheckWakeUp()
        {
            var time = K8055.ReadAnalogChannel(1);
            if (Math.Max(HorasDia - 10, 0) < time && time < Math.Min(HorasDia + 10, 255))
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[6]).IsChecked = true;
                ((CheckBox)K8055DigitalOutputCanvas.Children[7]).IsChecked = true;
            }
            else
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[6]).IsChecked = false;
                ((CheckBox)K8055DigitalOutputCanvas.Children[7]).IsChecked = false;
            }
        }

        /// <summary>
        /// Designates the behaviour of the temperature input.
        /// </summary>
        private void TempControl()
        {
            //Temperatura (analog in 2)
            // -> Ar condicionado (digital out 5)
            // -> Persianas (analog out 1)

            while (_onOff)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (!(120 < K8055.ReadAnalogChannel(2) && K8055.ReadAnalogChannel(2) < 150))
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[4]).IsChecked = true;
                        _isolate = true;
                    }
                    else
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[4]).IsChecked = false;
                        _isolate = false;

                    }
                }));
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Randomizes current temrature.
        /// </summary>
        private void RandTemperature()
        {
            var rand = new Random();

            Dispatcher.BeginInvoke(new Action(delegate
            {
                K8055.SetAnalogInputChannel(2, rand.Next(30, 240));
            }));
        }

        /// <summary>
        /// toggles the System
        /// </summary>
        private void OnOffCreation()
        {
            // ON/OFF (digital in 5)

            Dispatcher.BeginInvoke(new Action(delegate
            {
                _onOff = !_onOff;

                if (_onOff)
                {
                    Thread threadTime = new Thread(TimeControl);
                    Thread threadTemp = new Thread(TempControl);

                    threadTime.Start();
                    threadTemp.Start();
                }
                else
                {
                    AllOutToNull();
                }

                K8055.SetDigitalInputChannel(5, _onOff);
            }));
        }

        /// <summary>
        /// Sets all outputs to 0 or false, for analog and digital respectively.
        /// </summary>
        private void AllOutToNull()
        {
            K8055.OutputAnalogChannel(1, 0);
            K8055.OutputAnalogChannel(2, 0);

            ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = false;
            ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = false;
            ((CheckBox)K8055DigitalOutputCanvas.Children[2]).IsChecked = false;
            ((CheckBox)K8055DigitalOutputCanvas.Children[3]).IsChecked = false;
            ((CheckBox)K8055DigitalOutputCanvas.Children[4]).IsChecked = false;
        }


        //SET TIMES OF DAY
        //------------------------------------------------------------------------------------------
        private void AtivarManha_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HorasDia = int.Parse(((TextBox)K8055ManhaHora).Text);
                var MinDia = int.Parse(((TextBox)K8055ManhaMinuto).Text);

                if (HorasDia == 24 && MinDia > 30)
                {
                    HorasDia = 1;
                }
                if (HorasDia < 24 && MinDia > 30)
                {
                    HorasDia += 1;
                }

                if (HorasDia > 24)
                    HorasDia = 9;

                HorasDia = (HorasDia * 255) / 24;
            }
            catch (Exception)
            {
                HorasDia = 85;
            }
        }

        private void AtivarNoite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HorasNoite = int.Parse(((TextBox)K8055NoiteHora).Text);
                var MinNoite = int.Parse(((TextBox)K8055NoiteMinuto).Text);

                if (HorasNoite == 24 && MinNoite > 30)
                {
                    HorasNoite = 1;
                }
                if (HorasNoite < 24 && MinNoite > 30)
                {
                    HorasNoite += 1;
                }
                if (HorasNoite > 24)
                    HorasNoite = 20;

                HorasNoite = (HorasNoite * 255) / 24;
            }
            catch (Exception)
            {
                HorasNoite = 212;
            }
        }
    }
}
