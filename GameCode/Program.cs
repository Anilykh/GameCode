using System;
using System.Device.Gpio;
using System.Threading;

namespace ColorSequence
{
    public class Program
    {
        private static GpioController s_GpioController;
        private static GpioPin[] leds = new GpioPin[4];
        private static GpioPin[] buttons = new GpioPin[4];
        private static Color[] sequence;
        private static int sequenceIndex = 0;
        private static int round = 1;
        private static Timer timer;
        private static Timer offTimer;
        private const int offTimeout = 30000; 
        private static Color expectedColor;
        private static int buttonPressCount = 0; 
        private static int[] buttonPressCounts = new int[4];
        private static readonly string logFilePath = "log.txt";



        public static void Main()
        {
            s_GpioController = new GpioController();

            
            int[] ledPins = {16,25, 19, 14 }; 
            int[] buttonPins = { 17, 26, 18, 12 }; 

          
            for (int i = 0; i < 4; i++)
            {
                leds[i] = s_GpioController.OpenPin(ledPins[i], PinMode.Output);
                buttons[i] = s_GpioController.OpenPin(buttonPins[i], PinMode.InputPullUp);

                
                leds[i].Write(PinValue.Low);
            }
           
            ShuffleSequence();
           
            for (int i = 0; i < 4; i++)
            {
                int buttonIndex = i; 
                buttons[i].ValueChanged += (sender, args) => Button_ValueChanged(leds[buttonIndex], args, (Color)buttonIndex);
            }
          
            StartRound();

            Thread.Sleep(Timeout.Infinite);
        }

        private static void Button_ValueChanged(GpioPin led, PinValueChangedEventArgs e, Color color)
        {         
            timer.Change(5000, Timeout.Infinite);
            offTimer.Change(offTimeout, Timeout.Infinite);


            if (round <= 10) 
            {
                if (e.ChangeType == PinEventTypes.Falling && color == expectedColor) 
                {
                    Console.WriteLine($"Button of color {color} pressed.");
                    led.Write(PinValue.Low); 
                    buttonPressCounts[(int)color]++;

                    
                    sequenceIndex++;
                    if (sequenceIndex >= sequence.Length)
                    {                    
                        StartRound();

                        return; 
                    }
                   
                    expectedColor = sequence[sequenceIndex];

                    leds[(int)expectedColor].Write(PinValue.High);
                }
                else if (e.ChangeType == PinEventTypes.Rising && color == expectedColor) 
                {
                    led.Write(PinValue.Low); 
                }
            }
            else 
            {
                if (e.ChangeType == PinEventTypes.Falling)
                {
                    
                    Console.WriteLine($"Button of color {color} pressed. Total presses: {buttonPressCount}");
                }
            }

        }

        private static void TimerCallback(object state)
        {
            leds[(int)expectedColor].Write(PinValue.Low);

            timer.Change(5000, Timeout.Infinite);
        }

        private static void OffTimerCallback(object state)
        {
            foreach (var led in leds)
            {
                led.Write(PinValue.Low);
            }
        }

        private static void ShuffleSequence()
        {
            sequence = new Color[] { Color.Blue, Color.Red, Color.Yellow, Color.Green };
            Random rng = new Random();
            int n = sequence.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Color value = sequence[k];
                sequence[k] = sequence[n];
                sequence[n] = value;
            }
        }
      
        private static void StartRound()
        {
            Console.WriteLine($"Round {round++} Start");

            sequenceIndex = 0;

            ShuffleSequence();

            expectedColor = sequence[0];

            leds[(int)expectedColor].Write(PinValue.High);

            timer = new Timer(TimerCallback, null, 5000, Timeout.Infinite);

            offTimer = new Timer(OffTimerCallback, null, offTimeout, Timeout.Infinite);

            Console.Write("Sequence: ");
            foreach (var color in sequence)
            {
                Console.Write(color.ToString() + " ");
            }
            Console.WriteLine();

            if (round == 11)
            {
                DisplayButtonPressSummary();
            }

        }

        private static void DisplayButtonPressSummary()
        {
            Console.WriteLine("Button press summary after 10 rounds:");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine($"Button {i} pressed: {buttonPressCounts[i]} times");
            }
        }

        
        private enum Color
        {
            Blue,
            Red,
            Yellow,
            Green
        }
    }
}
