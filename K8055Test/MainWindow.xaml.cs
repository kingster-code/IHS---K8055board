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


        private int? HorasDia, HorasNoite, MinDia, MinNoite;


        private bool _lightSwitch;
        private bool _sofaSwitch;
        private bool _hoseSwitch;
        private bool _onOff;

        private bool _doDayLightCycle;
        private bool _varyTemp;
        private bool _isolate;


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
                        Thread thread = new Thread(ResetTemperature);
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
                case "timeDay":
                    {
                        //Hora do Dia (digital in 4)
                        // -> Persianas (analog out 1)
                        // -> Aspressor (digital out 6)
                        // -> Maquina café (digital out 7)
                        // -> Água Banho TEMPERATURA (analog out 2)
                        // -> Radio (digital out 8)


                        break;
                    }
                case "onOff":
                    {
                        _onOff = !_onOff;
                        if (_onOff)
                        {
                            Thread threadTime = new Thread(TimeControl);
                            Thread threadTemp = new Thread(TempControl);

                            threadTime.Start();
                            threadTemp.Start();
                        }
                        break;
                    }



                //Utilities for demonstration
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
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055.SetAnalogInputChannel(1, 0);
                }));
            }
        }

        private void VaryTemp()
        {
            var rand = new Random();
            K8055.SetAnalogInputChannel(2, 130);

            while (_varyTemp)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    K8055.SetAnalogInputChannel(2,
                                                    K8055.ReadAnalogChannel(2)
                                                        + rand.Next(-3, 4));
                }));
                Thread.Sleep(100);
            }
        }

        private void LightToggle()
        {
            //Luz jantar (digital in 1)
            // -> Liga luzes sala jantar (digital out 1)
            // -> Liga luzes Cozinha (digital out 2)
            _lightSwitch = !_lightSwitch;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = false;
                ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = false;

                if (_lightSwitch)
                {
                    //K8055.OutputAnalogChannel(1, 20);
                    K8055.SetDigitalInputChannel(1, false);

                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = false;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = false;
                    }
                }
                else
                {
                    //K8055.OutputAnalogChannel(1, 0);
                    K8055.SetDigitalInputChannel(1, true);

                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[0]).IsChecked = true;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[1]).IsChecked = true;
                    }
                }
            }));
        }

        private void SofaRemote()
        {
            //Sofá (digital in 2)
            // -> Liga luzes sala jantar (digital out 3)
            // -> Liga luzes Cozinha (digital out 4)
            _sofaSwitch = !_sofaSwitch;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_sofaSwitch)
                {
                    K8055.SetDigitalInputChannel(2, false);

                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[2]).IsChecked = false;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[3]).IsChecked = false;
                    }
                }
                else
                {
                    K8055.SetDigitalInputChannel(2, true);

                    if (_onOff)
                    {
                        ((CheckBox)K8055DigitalOutputCanvas.Children[2]).IsChecked = true;
                        ((CheckBox)K8055DigitalOutputCanvas.Children[3]).IsChecked = true;
                    }
                }
            }));
        }

        private void HomeBlinds()
        {
            //Blinds (digital in 3)
            // -> Persianas (analog out 1)
            Dispatcher.BeginInvoke(new Action(delegate
            {
                BlindsActivation();
            }));
        }

        private void BlindsActivation()
        {
            // Persianas(analog out 1)
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_isolate)
                {
                    K8055.OutputAnalogChannel(1, 50);
                }
                else
                {
                    K8055.OutputAnalogChannel(1, 150);
                }

            }));
        }

        private void HoseRegulation()
        {
            //Rega (digital in 4)
            // -> Aspressor (digital out 6)
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _hoseSwitch = !_hoseSwitch;

                if (_hoseSwitch)
                {
                    K8055.SetDigitalInputChannel(4, false);

                    if (_onOff)
                        HoseActivation(false);
                }
                else
                {
                    K8055.SetDigitalInputChannel(4, true);

                    if (_onOff)
                        HoseActivation(true);
                }
            }));
        }

        private void HoseActivation(bool set)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ((CheckBox)K8055DigitalOutputCanvas.Children[5]).IsChecked = set;
            }));
        }



        private void TimeControl()
        {
            //Hora do Dia (digital in 4)

            // -> Persianas (analog out 1)
            // -> Água Banho TEMPERATURA (analog out 2)

            // -> Aspressor (digital out 6)
            // -> Maquina café (digital out 7)
            // -> Radio (digital out 8)

            while (_onOff)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {

                }));
                Thread.Sleep(100);
            }
        }

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


        private void ResetTemperature()
        {
            var rand = new Random();

            Dispatcher.BeginInvoke(new Action(delegate
            {
                K8055.SetAnalogInputChannel(2, rand.Next(30, 240));
            }));
        }


        //SET TIMES OF DAY
        //------------------------------------------------------------------------------------------
        private void AtivarManha_Click(object sender, RoutedEventArgs e)
        {
            HorasDia = int.Parse(((TextBox)K8055ManhaHora).Text);
            MinDia = int.Parse(((TextBox)K8055ManhaMinuto).Text);

            if (HorasDia==24 && MinDia > 30)
            {
                HorasDia = 1;
            }
            if (HorasDia < 24 && MinDia > 30)
            {
                HorasDia += 1;
            }

            if (HorasDia > 24)
                HorasDia = 9;
        }

        private void AtivarNoite_Click(object sender, RoutedEventArgs e)
        {
            HorasNoite = int.Parse(((TextBox)K8055NoiteHora).Text);
            MinNoite = int.Parse(((TextBox)K8055NoiteMinuto).Text);

            if (HorasNoite == 24 && MinNoite> 30)
            {
                HorasNoite = 1;
            }
            if (HorasNoite < 24 && MinNoite > 30)
            {
                HorasNoite += 1;
            }
            if (HorasNoite > 24)
                HorasNoite = 20;
        }

    }
}
