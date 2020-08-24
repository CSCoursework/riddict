using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
//Packages I had to install
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace rididct
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //Opening Window
            InitializeComponent();
        }

        //Global variables
        //So functions that should only be excecuted when game is playing are blocked
        public bool gameOver = false;
        //So variables know weather the answerbox is in focus
        bool AnswerBox_Focused = false;
        //Answer
        public string word;
        //Serialised JSON of the API response
        public string definitionUnformatted;
        //definition of the word
        public string def;
        //Points
        public int points;
        /// <summary>
        /// Combo Based On Amount of correct answers in a sucssesion that the player has gotten
        /// correct within 10 seconds. This is used to award extra lives to the player so they
        /// can get bigger scores, additionally giving the player an incentive to rush when
        /// answering. This helps the game to be more fast paced and fun.
        /// </summary>
        public double combo;
        public int comboCounter = 0;
        public int comboCounter_highest;
        public bool comboTimer_NeedReset = false;
        //Timer to determine weather combo needs to reset
        int timerProgress;
        public async void Timer()
        {
            for (timerProgress = 100; timerProgress > 0; timerProgress--)
            {
                await Task.Delay(100);
                ComboTimer_Bar.Value = timerProgress;
                if (timerProgress == 1)
                {
                    timerProgress += 1;
                    ComboTimer_Bar.Value = 0;
                    combo = 0;
                    comboCounter = 0;
                    ComboCounter_Text.Content = comboCounter + "x";
                }
            }
        }





        public bool comboTimer_done;
        public bool lifeOwed;
        //Amount of remaning lives
        public int lives = 3;
        public bool lives_NeedUpdate = false;
        //Stars for blocking out word in defintion and example
        public string stars;

        //Flashes the answer box so the player can see how their answer has changed
        public async void FlashButton()
        {
            //if the answerboc has focus excecute this
            if (AnswerBox_Focused == true)
            {
                //Giving visual feedback so the user can see that the enter key/ button has been pressed.

                EnterButton_Yellow.Visibility = Visibility.Hidden;
                EnterButton.Visibility = Visibility.Visible;
                AnswerBox_Line.Fill = new SolidColorBrush(Colors.Black);
                await Task.Delay(250);
                EnterButton.Visibility = Visibility.Hidden;
                EnterButton_Yellow.Visibility = Visibility.Visible;
                var bc = new BrushConverter();
                AnswerBox_Line.Fill = (Brush)bc.ConvertFrom("#ffbc00");
            }

        }


        //MAIN CODE FOR GAME

        //JSON stuff
        public class Definition
        {
            public string type { get; set; }
            public string definition { get; set; }
            public string example { get; set; }
            public string image_url { get; set; }
            public object emoji { get; set; }

        }

        //JSON stuff
        public class Root
        {
            public List<Definition> definitions { get; set; }
            public string word { get; set; }
            public string pronunciation { get; set; }
        }



        /// <summary>
        /// NOTE: had to get help from stack overflow with some of this function becasue i forgot that this is not python lol
        /// 
        /// Gets random word
        /// Sends Random Word to Define Class
        /// Checks If API response is valid
        ///     If (its valid) {it continues} else {it tries again}
        ///     Sets variables for definition and example.
        ///     Checks if def will not throw an exeption when replacing any instances of the word with stars
        ///         If (it will not throw an exception){It Continues}
        ///         Replaces any instances of the word with stars
        ///     Checks if example will not throw an exeption when replacing any instances of the word with stars
        ///         If (it will not throw an exception){It Continues}
        ///         Replaces any instances of the word with stars
        ///     sets definition line and big letter
        ///     catches any exceptions
        ///     returns word
        ///       
        /// </summary>
        /// <returns></returns>
        public async Task<string> SetWord()
        {
            

            bool tryAgain = true;
            while (tryAgain)
            {
                try
                {
                    word = RandomWord();
                    definitionUnformatted = await Define(word);
                    Root DeserializedJSON = JsonConvert.DeserializeObject<Root>(definitionUnformatted);
                    tryAgain = false;
                    if (DeserializedJSON.definitions != null && DeserializedJSON.definitions.Count > 0)
                    {
                        //Thank god for stack overflow!
                        //https://stackoverflow.com/q/63356979/14086472
                        def = DeserializedJSON.definitions[0].definition;
                        string ex = DeserializedJSON.definitions[0].example;

                        stars = String.Concat(Enumerable.Repeat("*", word.Length));
                        if (def != null) { def = def.Replace(word, stars); }

                        else { tryAgain = true; }

                        if (ex != null ) {ex = ex.Replace(word, stars); }

                        //got a little help from stack overflow
                        //https://stackoverflow.com/a/18154152/14086472
                        HtmlDocument htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(@def);
                        def = htmlDoc.DocumentNode.InnerText;
                        htmlDoc.LoadHtml(@ex);
                        ex = htmlDoc.DocumentNode.InnerText;

                        if (tryAgain == false && def != null || def != "")
                        {
                            DefinitionBox.Text = def + "\n\n" + ex;
                            FirstLetter.Content = word.Substring(0, 1);
                        }
                        else { tryAgain = true; }
                    }



                    else 
                    { 
                        def = "No definition found"; 
                        tryAgain = true; 
                    }
                    
                }
                catch (Exception)
                {
                    tryAgain = true;
                }
                
                
            }
            
            return word;
            

        }

        //chooses random word from "/bin/debug/Words.txt"
        string RandomWord()
        {
            string[] allLines = File.ReadAllLines(@"Words.txt");
            Random rnd1 = new Random();
            string temp_word = (allLines[rnd1.Next(allLines.Length)]);
            return temp_word;
        }

       
        
        
        //Logic for is user answer is correct
        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task AnswerCorrect()
        {
            points++;
            Points.Content = "Points: " + points;
            //possible future bug if the API is more efficient/ ping is lower, the correct may not show up
            DefinitionBox.Text = "Correct";
            comboCounter++;
            
            if (combo <= 0.9)
            {
                combo += 0.1;

                if (combo == 1)
                {
                    if (lives == 3)
                    {
                        //do nothing
                    }
                    else
                    {
                        lives++;
                        lives_NeedUpdate = true;
                        combo = 0;
                    }
                }
            }
           
            
            //combo = Math.Clamp(0, 1); - doesn't work for some reason even though  it says is does here
            //https://docs.microsoft.com/en-us/dotnet/api/system.math.clamp?view=netcore-3.1     

            }

        //Logic for is user answer is incorrect
        public async Task AnswerIncorrect()
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //possible future bug if the API is more efficient/ ping is lower, the correct answer may not show up
            DefinitionBox.Text = "Inorrect, answer was " + word;
            if (comboCounter > comboCounter_highest) { comboCounter_highest = comboCounter; }
            lives--;
            lives_NeedUpdate = true;
            combo = 0;
            comboCounter = 0;
        }

        //when enterkey down or button clicked runs main logic of game.
        public async void AnswerSubmit()
        {
            timerProgress = 100;
            if (gameOver == false)
            {
                //Gives visual feedback of button click
                FlashButton();

                if (AnswerBox.Text.Equals(word , StringComparison.OrdinalIgnoreCase))
                {
                    await AnswerCorrect();
                }
                else
                {
                    await AnswerIncorrect();
                }
                comboTimer_NeedReset = true;

                //no point doing all the if statements if the player has no lives
                if (lives > 0)
                {
                    await SetWord();
                    if (lives_NeedUpdate)
                    {
                        if (lives == 3)
                        {
                            Live1.Visibility = Visibility.Visible;
                            Live2.Visibility = Visibility.Visible;
                            Live3.Visibility = Visibility.Visible;
                        }
                        if (lives == 2)
                        {
                            Live1.Visibility = Visibility.Visible;
                            Live2.Visibility = Visibility.Visible;
                            Live3.Visibility = Visibility.Hidden;
                        }
                        if (lives == 1)
                        {
                            Live1.Visibility = Visibility.Visible;
                            Live2.Visibility = Visibility.Hidden;
                            Live3.Visibility = Visibility.Hidden;
                        }
                    }
                }
                else
                {
                    gameOver = true;
                    Live1.Visibility = Visibility.Hidden;
                    Live2.Visibility = Visibility.Hidden;
                    Live3.Visibility = Visibility.Hidden;

                    GameOverScreen.Visibility = Visibility.Visible;
                    GameOverScreen.BeginAnimation(
                    UIElement.OpacityProperty,
                    new DoubleAnimation(0d, 0.97d, TimeSpan.FromSeconds(0.5)));
                    AnswerBox.IsEnabled = false;
                    AnswerBox.IsEnabled = true;
                    ComboCounter_Text.Visibility = Visibility.Hidden;
                    ComboTimer_Bar.Visibility = Visibility.Hidden;

                    GameOverPoints.Content = points;
                    GameOverCombo.Content = comboCounter_highest;
                }
                AnswerBox.Text = "";
                ComboCounter_Text.Content = comboCounter + "x";
                
                
            }

        }



        //calls owlbot.info api
        public async Task<string> Define(string temp_word)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://owlbot.info/api/v4/dictionary/" + temp_word))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Token 2d86b5614989db20db61b5dd33811298a5863a48");

                    var response = await httpClient.SendAsync(request);
                    HttpContent content = response.Content;

                    // ... Check Status Code                                
                    Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                    //Read the string.
                    string result = await content.ReadAsStringAsync();
                    //Return the result.
                    return result;

                }
            }
        }


        //START SCREEN CODE

        //Click PlayButton event
        public async void PlayButton_Click(object sender, MouseButtonEventArgs e)
        {
            //Changing From Start Screen Grid To Quiz Screen Grid
            QuizScreen.Visibility = Visibility.Visible;
            StartScreen.Visibility = Visibility.Collapsed;
            //Running game logic once to start game up

            word = await SetWord();
            DefinitionBox.Text = def;
            FirstLetter.Content = word.Substring(0, 1);
            Timer();

        }



        //Mouseover PlayButton event
        private void PlayButton_MouseEnter(object sender, MouseEventArgs e)
        {
            //Hover effect
            PlayButtonRect.Opacity = 0.25;
        }

        //Mouseoff PlayButton event
        private void PlayButton_MouseLeave(object sender, MouseEventArgs e)
        {
            //Removing hover effect
            PlayButtonRect.Opacity = 1;
        }

        //QUIZ SCREEN CODE

        private void AnswerBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AnswerBox_Focused = false;
            //Deselecting Answer Box
            AnswerBox_Line.Fill = new SolidColorBrush(Colors.Black);
            AnswerBox_Text.Visibility = Visibility.Visible;
            EnterButton_Yellow.Visibility = Visibility.Hidden;
            EnterButton.Visibility = Visibility.Visible;
        }

        private void AnswerBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AnswerBox_Focused = true;
            //Selecting Answer Box
            var bc = new BrushConverter();
            AnswerBox_Line.Fill = (Brush)bc.ConvertFrom("#ffbc00");
            AnswerBox_Text.Visibility = Visibility.Hidden;
            EnterButton.Visibility = Visibility.Hidden;
            EnterButton_Yellow.Visibility = Visibility.Visible;
        }

        private void EnterClick(object sender, MouseButtonEventArgs e)
        {
            AnswerSubmit();
        }


        private void AnswerBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                AnswerSubmit();
            }
        }

        private void BackgroundClick(object sender, MouseButtonEventArgs e)
        {
            AnswerBox.IsEnabled = false;
            AnswerBox_Focused = false;
            AnswerBox.IsEnabled = true;
        }

        private void PlayAgainButton_MouseEnter(object sender, MouseEventArgs e)
        {
            //Hover effect
            PlayAgainButtonRect.Opacity = 0.25;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task fadeOut(Grid screen)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            screen.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0.97d, 0d, TimeSpan.FromSeconds(0.5)));
        }
        private async void PlayAgainButton_Click(object sender, MouseButtonEventArgs e)
        {
            gameOver = false;
            Live1.Visibility = Visibility.Visible;
            Live2.Visibility = Visibility.Visible;
            Live3.Visibility = Visibility.Visible;


            await fadeOut(GameOverScreen);
            await Task.Delay(500);
            GameOverScreen.Visibility = Visibility.Hidden;

            AnswerBox.IsEnabled = true;
            AnswerBox.IsEnabled = false;
            ComboCounter_Text.Visibility = Visibility.Visible;
            ComboTimer_Bar.Visibility = Visibility.Visible;

            points = 0;
            comboCounter_highest = 0;
            lives = 3;

            word = await SetWord();
            DefinitionBox.Text = def;
            FirstLetter.Content = word.Substring(0, 1);
            timerProgress = 100;

            AnswerBox_Focused = false;
            //Deselecting Answer Box
            AnswerBox_Line.Fill = new SolidColorBrush(Colors.Black);
            AnswerBox_Text.Visibility = Visibility.Visible;
            EnterButton_Yellow.Visibility = Visibility.Hidden;
            EnterButton.Visibility = Visibility.Visible;
        }


        private void PlayAgainButton_MouseLeave(object sender, MouseEventArgs e)
        {
            //Hover effect
            PlayAgainButtonRect.Opacity = 1;
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseButtonRect.Opacity = 0.25;
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseButtonRect.Opacity = 1;
        }

        private async void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            await fadeOut(AboutScreen);
            await Task.Delay(500);
            AboutScreen.Visibility = Visibility.Hidden;
        }

        private void AboutButton_Click(object sender, MouseButtonEventArgs e)
        {
            AboutScreen.Visibility = Visibility.Visible;
            AboutScreen.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0d, 0.97d, TimeSpan.FromSeconds(0.5)));
        }

        private void GitHub_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/boogalewie/riddict/releases/");
        }
    }
}

