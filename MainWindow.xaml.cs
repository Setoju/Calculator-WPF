using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Reflection;

namespace Calculator
{    
    // Command interface
    interface ICommand
    {
        void Execute();
    }

    class ClearEntryCommand : ICommand
    {
        private TextBox _outputTextBox;
        private List<string> _previousExpression;

        public ClearEntryCommand(TextBox outputTextBox, ref List<string> previousExpression)
        {
            _outputTextBox = outputTextBox;
            _previousExpression = previousExpression;
        }

        public void Execute()
        {
            if (_previousExpression.Count > 0 && !string.IsNullOrEmpty(_previousExpression[_previousExpression.Count - 1]))
            {
                _outputTextBox.Text = _previousExpression[_previousExpression.Count - 1];
                _previousExpression.Remove(_previousExpression[_previousExpression.Count - 1]);
            }
        }
    }

    class SquareRootCommand : ICommand
    {
        private TextBox _outputTextBox;

        public SquareRootCommand(TextBox outputTextBox)
        {
            _outputTextBox = outputTextBox;
        }

        public void Execute()
        {
            if (double.TryParse(_outputTextBox.Text, out double value))
            {
                if (value >= 0)
                {
                    _outputTextBox.Text = Math.Sqrt(value).ToString();
                }
                else
                {
                    _outputTextBox.Text = "Invalid Input";
                }
            }
            else
            {
                _outputTextBox.Text = "Error";
            }
        }
    }

    class LogCommand : ICommand
    {
        private TextBox _outputTextBox;

        public LogCommand(TextBox outputTextBox)
        {
            _outputTextBox = outputTextBox;
        }

        public void Execute()
        {
            if (double.TryParse(_outputTextBox.Text, out double value))
            {
                if (value > 0)
                {
                    _outputTextBox.Text = Math.Log10(value).ToString();
                }
                else
                {
                    _outputTextBox.Text = "Invalid Input";
                }
            }
            else
            {
                _outputTextBox.Text = "Error";
            }
        }
    }

    // Concrete command for adding a character
    class AddCharacterCommand : ICommand
    {
        private string _character;
        private TextBox _outputTextBox; 
        private string _computingPattern;

        public AddCharacterCommand(TextBox outputTextBox, string character, string computingPattern)
        {
            _outputTextBox = outputTextBox;
            _character = character;
            _computingPattern = computingPattern;
        }

        public void Execute()
        {
            string currentExpression = _outputTextBox.Text;

            if (_character == ".")
            {
                if (currentExpression.Length > 0 && char.IsDigit(currentExpression[^1]))
                {
                    char[] operators = { '+', '-', '*', '/' };                    
                    if (!currentExpression.Split(operators).Last().Contains('.'))
                    {
                        _outputTextBox.Text += _character;
                    }
                    return;
                }
                else
                {
                    return;
                }
            }

            if (currentExpression.Length > 1)
            {
                string currentInput = currentExpression[^1].ToString();
                string[] operators = { "+", "-", "*", "/" };                

                if (operators.Contains(_character) && operators.Contains(currentInput))
                {
                    _outputTextBox.Text = currentExpression[..^1] + _character;
                    return;
                }
            }
            
            _outputTextBox.Text += _character;
            if (Regex.IsMatch(_outputTextBox.Text, _computingPattern))
            {
                _outputTextBox.Text = _outputTextBox.Text.Substring(0, _outputTextBox.Text.Length - 1);
                new EvaluateExpressionCommand(_outputTextBox).Execute();
                _outputTextBox.Text += _character;
            }            
        }
    }

    // Concrete command for evaluating expression
    class EvaluateExpressionCommand : ICommand
    {
        private TextBox _outputTextBox;

        public EvaluateExpressionCommand(TextBox outputTextBox)
        {
            _outputTextBox = outputTextBox;
            char[] operators = { '+', '-', '*', '/' };
            MainWindow.previousExpression.Add(outputTextBox.Text.Split(operators)[0]);
        }

        public void Execute()
        {
            try
            {
                DataTable dt = new DataTable();
                var result = dt.Compute(_outputTextBox.Text, "");
                _outputTextBox.Text = result.ToString();
            }
            catch (Exception)
            {
                _outputTextBox.Text = "Error";
            }
        }
    }

    // Concrete command for clearing the output
    class ClearOutputCommand : ICommand
    {
        private TextBox _outputTextBox;

        public ClearOutputCommand(TextBox outputTextBox)
        {
            _outputTextBox = outputTextBox;
        }

        public void Execute()
        {
            _outputTextBox.Text = "";            
        }
    }

    // Concrete command for deleting the last character
    class DeleteLastCharacterCommand : ICommand
    {
        private TextBox _outputTextBox;

        public DeleteLastCharacterCommand(TextBox outputTextBox)
        {
            _outputTextBox = outputTextBox;
        }

        public void Execute()
        {
            if (_outputTextBox.Text.Length > 0)
            {
                _outputTextBox.Text = _outputTextBox.Text.Substring(0, _outputTextBox.Text.Length - 1);
            }
        }
    }

    public partial class MainWindow : Window
    {
        private string computingPattern = @"(?<!\d\s*[-+*/])\d+\s*[+\-/*]\s*\d+\s*[+\-/*]";
        private ICommand? _command;        
        public static List<string> previousExpression = new List<string>();
        private bool isFunctionalityExpanded = false;

        public MainWindow()
        {
            InitializeComponent();

            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            this.MinHeight = 200;
            this.MaxHeight = 500;
            this.MinWidth = 100;
            this.MaxWidth = 400;

            PreviewTextInput += KeyboardTextInput;
        }        

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (isFunctionalityExpanded)
            {
                Grid.SetColumnSpan(OutputTextBlock, 4);
                HideAdditionalButtons();
                RemoveAdditionalGridColumn();
            }
            else
            {
                AddAdditionalGridColumn();
                AddAdditionalButtons();
                Grid.SetColumnSpan(OutputTextBlock, 5);
            }
            
            isFunctionalityExpanded = !isFunctionalityExpanded;
        }

        private void AddAdditionalGridColumn()
        {            
            ColumnDefinition column = new ColumnDefinition();
            column.Width = new GridLength(0.75, GridUnitType.Star);
            ExpandGrid.ColumnDefinitions.Add(column);
        }

        private void RemoveAdditionalGridColumn()
        {            
            if (ExpandGrid.ColumnDefinitions.Count > 1)
            {
                ExpandGrid.ColumnDefinitions.RemoveAt(ExpandGrid.ColumnDefinitions.Count - 1);
            }
        }

        private void HideAdditionalButtons()
        {
            List<Button> buttonsToRemove = new List<Button>();

            // Find buttons with content "√" and add them to the list
            foreach (Button button in ExpandGrid.Children.OfType<Button>())
            {
                if (button.Content.ToString() == "√")
                {
                    buttonsToRemove.Add(button);
                }
            }

            // Remove the buttons from the ExpandGrid
            foreach (Button button in buttonsToRemove)
            {
                ExpandGrid.Children.Remove(button);
            }
        }

        private void AddAdditionalButtons()
        {
            Button sqrtButton = new Button();
            sqrtButton.Content = "√";
            sqrtButton.Click += SqrtButton_Click;

            Button logButton = new Button();
            logButton.Content = "log";
            logButton.Click += LogButton_Click;

            Grid.SetColumn(sqrtButton, ExpandGrid.ColumnDefinitions.Count - 1);
            Grid.SetRow(sqrtButton, 1);
            ExpandGrid.Children.Add(sqrtButton);

            Grid.SetColumn(logButton, ExpandGrid.ColumnDefinitions.Count - 1);
            Grid.SetRow(logButton, 2);
            ExpandGrid.Children.Add(logButton);
        }

        private void SqrtButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutputTextBlock.Text.Length > 0)
            {
                _command = new EvaluateExpressionCommand(OutputTextBlock);
                _command.Execute();

                _command = new SquareRootCommand(OutputTextBlock);
                _command.Execute();
            }            
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutputTextBlock.Text.Length > 0)
            {
                _command = new EvaluateExpressionCommand(OutputTextBlock);
                _command.Execute();

                _command = new LogCommand(OutputTextBlock);
                _command.Execute();
            }
        }        

        private void Error_Clear()
        {
            if (OutputTextBlock.Text.Contains("Error"))
            {
                OutputTextBlock.Text = "";
            }
        }

        private void KeyboardTextInput(object sender, TextCompositionEventArgs e)
        {
            Error_Clear();

            if (char.IsDigit(e.Text, 0) || "+-*/".Contains(e.Text) || e.Text == "\b" || e.Text == "\r")
            {
                if (e.Text == "\b")
                {                    
                    _command = new DeleteLastCharacterCommand(OutputTextBlock);
                    _command.Execute();
                }
                else if (e.Text == "\r")
                {                    
                    _command = new EvaluateExpressionCommand(OutputTextBlock);
                    _command.Execute();
                }
                else
                {
                    // Append the input
                    _command = new AddCharacterCommand(OutputTextBlock, e.Text, computingPattern);
                    _command.Execute();

                    if (Regex.IsMatch(OutputTextBlock.Text, computingPattern))
                    {
                        _command = new EvaluateExpressionCommand(OutputTextBlock);
                        _command.Execute();
                        _command = new AddCharacterCommand(OutputTextBlock, e.Text, computingPattern);
                        _command.Execute();
                    }
                }
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {            
            e.Handled = true;
        }

        private void Operation_Click(object sender, RoutedEventArgs e)
        {
            Error_Clear();

            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                string content = clickedButton.Content.ToString();

                switch (content)
                {
                    case "=":
                        _command = new EvaluateExpressionCommand(OutputTextBlock);
                        _command.Execute();
                        break;
                    case "C":
                        _command = new ClearOutputCommand(OutputTextBlock);
                        _command.Execute();
                        break;
                    case "CE": 
                        _command = new ClearEntryCommand(OutputTextBlock, ref previousExpression);
                        _command.Execute();
                        break;
                    case "⌫":
                        _command = new DeleteLastCharacterCommand(OutputTextBlock);
                        _command.Execute();
                        break;
                    default:
                        _command = new AddCharacterCommand(OutputTextBlock, content, computingPattern);
                        _command.Execute();
                        if (Regex.IsMatch(OutputTextBlock.Text, computingPattern))
                        {
                            _command = new EvaluateExpressionCommand(OutputTextBlock);
                            _command.Execute();
                            _command = new AddCharacterCommand(OutputTextBlock, content, computingPattern);
                            _command.Execute();
                        }
                        break;
                }
            }
        }
    }
}
