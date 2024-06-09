using System;
using Gtk;
using Cairo;
using System.Data.SqlClient;



public partial class MainWindow : Gtk.Window
{
    private Entry entry;
    private Table table;
    private string connectionString = "Data Source=.;Initial Catalog=CalculatorDB;User ID=SA;Password=tazmii5132@SQL";

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        // Set the window's background color to 
        this.ModifyBg(StateType.Normal, new Gdk.Color(0, 0, 0));

        // Create the table
        table = new Table(5, 5, true);
        Add(table);
        Title = " Calculator";
        // Attach the entry field to the table
        entry = new Entry();
        entry.IsEditable = false;
        table.Attach(entry, 0, 5, 0, 1);

        // Create buttons
        string[] buttons = {
            "7", "8", "9", "/", "sqrt",
            "4", "5", "6", "*", "x^2",
            "1", "2", "3", "-", "C",
            ".", "0", "=", "+", "History",
        };

        int row = 1;
        int col = 0;


        foreach (string buttonText in buttons)
        {
            Button button = new Button(buttonText);
            button.Name = "button";

            // Set background color for "C" and "History" buttons
            if (buttonText == "C")
            {
                button.ModifyBg(StateType.Normal, new Gdk.Color(255, 0, 0)); // Red background
            }
            else if (buttonText == "History")
            {
                button.ModifyBg(StateType.Normal, new Gdk.Color(255, 255, 0)); // Yellow background
            }

            button.Clicked += Button_Clicked;

            table.Attach(button, (uint)col, (uint)col + 1, (uint)row, (uint)row + 1);
            col++;
            if (col > 4)
            {
                col = 0;
                row++;
            }
        }

        // Show all UI components
        table.ShowAll();
    }


    private void TestDatabaseConnection()
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Database connection successful.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error connecting to database: " + ex.Message);
        }
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }


    private double Evaluate(string expression)
    {
        return Convert.ToDouble(new System.Data.DataTable().Compute(expression, ""));
    }

    private double ExtractSecondNumber(string expression)
    {
        // Trim any whitespace from the expression
        expression = expression.Trim();

        // Find the position of the operator
        int operatorIndex = -1;
        foreach (char op in "+-*/")
        {
            int index = expression.LastIndexOf(op);
            if (index > operatorIndex)
            {
                operatorIndex = index;
            }
        }

        if (operatorIndex == -1)
        {
            // No operator found, return 0 as the second number
            return 0;
        }

        // Extract the substring after the operator
        string secondNumberString = expression.Substring(operatorIndex + 1);

        // Parse the second number
        double secondNumber;
        if (!double.TryParse(secondNumberString, out secondNumber))
        {
            // If parsing fails, return 0
            return 0;
        }

        return secondNumber;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        Button button = (Button)sender;
        string text = button.Label;

        switch (text)
        {
            case "C":
                entry.Text = "";
                break;
            case "=":
                try
                {
                    // Evaluate the expression
                    double result = Evaluate(entry.Text);
                    double secondNumber = ExtractSecondNumber(entry.Text);
                    entry.Text = result.ToString();

                    // Store calculation in database
                    StoreCalculation(ApplicationState.Operation, ApplicationState.FirstNumber, secondNumber, result);
                }
                catch (Exception)
                {
                    entry.Text = "Error";
                }
                break;
            case "sqrt":
                try
                {
                    double value1 = Convert.ToDouble(entry.Text);
                    double value = Math.Sqrt(Convert.ToDouble(entry.Text));
                    entry.Text = value.ToString();

                    // Store calculation in database
                    StoreCalculation("sqrt", value1, 0, value); // Pass 0 as the secondNumber
                }
                catch (Exception)
                {
                    entry.Text = "Error";
                }
                break;
            case "x^2":
                try
                {
                    double value1 = Convert.ToDouble(entry.Text);
                    double value = Math.Pow(Convert.ToDouble(entry.Text), 2);
                    entry.Text = value.ToString();

                    // Store calculation in database
                    StoreCalculation("x^2", value1, 0, value); // Pass 0 as the secondNumber
                }
                catch (Exception)
                {
                    entry.Text = "Error";
                }
                break;

            case "+":
            case "-":
            case "*":
            case "/":
                // Store operation and first number
                StoreOperation(text, Convert.ToDouble(entry.Text));
                entry.Text += text;
                break;
            case "History":
                // Show calculation history
                ShowHistoryWindow();
                break;
            default:
                entry.Text += text;
                break;
        }
    }

    private void StoreOperation(string operation, double firstNumber)
    {
        // Store operation and first number in the application state
        ApplicationState.Operation = operation;
        ApplicationState.FirstNumber = firstNumber;
    }


    private void StoreCalculation(string operation, double firstNumber, double secondNumber, double result)
    {
        string tableName = "";
        string insertQuery = "";

        switch (operation)
        {
            case "+":
            case "-":
            case "*":
            case "/":
                tableName = operation == "+" ? "Addition_table" :
                            operation == "-" ? "Subtraction_table" :
                            operation == "*" ? "Multiplication_table" : "Division_table";

                insertQuery = $"INSERT INTO {tableName} (FirstNumber, SecondNumber, Result) VALUES (@FirstNumber, @SecondNumber, @Result)";
                break;
            case "x^2":
            case "sqrt":
                tableName = operation == "x^2" ? "Square_table" : "SquareRoot_table";
                insertQuery = $"INSERT INTO {tableName} (Number, Result) VALUES (@FirstNumber, @Result)";
                break;
            default:
                // Unsupported operation
                return;
        }

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@FirstNumber", firstNumber);
                    command.Parameters.AddWithValue("@SecondNumber", secondNumber);
                    command.Parameters.AddWithValue("@Result", result);
                    command.ExecuteNonQuery();
                }
                Console.WriteLine("Calculation stored successfully in " + tableName + " table.");

                string historyQuery = "INSERT INTO CalculationHistory (Operation, FirstNumber, SecondNumber, Result) " +
                                         "VALUES (@Operation, @FirstNumber, @SecondNumber, @Result)";
                using (SqlCommand historyCommand = new SqlCommand(historyQuery, connection))
                {
                    historyCommand.Parameters.AddWithValue("@Operation", operation);
                    historyCommand.Parameters.AddWithValue("@FirstNumber", firstNumber);
                    historyCommand.Parameters.AddWithValue("@SecondNumber", secondNumber);
                    historyCommand.Parameters.AddWithValue("@Result", result);
                    historyCommand.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error storing calculation: " + ex.Message);
            }
        }
    }

    private void UndoLastCalculation()
    {
        string deleteQuery = "DELETE FROM CalculationHistory WHERE CalculationID = (SELECT TOP 1 CalculationID FROM CalculationHistory ORDER BY CalculationID DESC)";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Last calculation undone successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No calculations to undo.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error undoing calculation: " + ex.Message);
            }
        }
    }

    private void ClearHistory()
    {
        string deleteQuery = "DELETE FROM CalculationHistory";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Calculation history cleared successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No calculation history to clear.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error clearing calculation history: " + ex.Message);
            }
        }
    }

    private void DeleteLastHistoryEntry()
    {
        string deleteQuery = "DELETE FROM CalculationHistory WHERE CalculationID = (SELECT TOP 1 CalculationID FROM CalculationHistory ORDER BY CalculationID DESC)";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Last calculation history entry deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No calculation history entries to delete.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting last calculation history entry: " + ex.Message);
            }
        }
    }

    private void ShowHistoryWindow()
    {
        Window historyWindow = new Window("Calculation History");
        VBox vbox = new VBox(false, 5); // VBox to hold the TextView and buttons
        TextView textView = new TextView();

        int entryNumber = 1;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                string selectQuery = "SELECT * FROM CalculationHistory";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string operation = reader.GetString(1);
                            double firstNumber = reader.GetDouble(2);
                            double secondNumber = reader.GetDouble(3);
                            double result = reader.GetDouble(4);

                            textView.Buffer.Text += $"{entryNumber}. {firstNumber} {operation} {secondNumber} = {result}\n";
                            entryNumber++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving calculation history: " + ex.Message);
            }
        }

        Button clearHistoryButton = new Button("Clear History");
        Button deleteLastEntryButton = new Button("Delete Last Entry");

        clearHistoryButton.Clicked += (sender, args) =>
        {
            ClearHistory();
            textView.Buffer.Text = ""; // Clear the TextView
        };

        deleteLastEntryButton.Clicked += (sender, args) =>
        {
            DeleteLastHistoryEntry();
            // Refresh the TextView to reflect changes
            textView.Buffer.Text = "";
            int refreshedEntryNumber = 1;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string selectQuery = "SELECT * FROM CalculationHistory";
                    using (SqlCommand command = new SqlCommand(selectQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string operation = reader.GetString(1);
                                double firstNumber = reader.GetDouble(2);
                                double secondNumber = reader.GetDouble(3);
                                double result = reader.GetDouble(4);

                                textView.Buffer.Text += $"{refreshedEntryNumber}. {firstNumber} {operation} {secondNumber} = {result}\n";
                                refreshedEntryNumber++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving calculation history: " + ex.Message);
                }
            }
        };

        vbox.PackStart(textView, true, true, 0);
        vbox.PackStart(clearHistoryButton, false, false, 0);
        vbox.PackStart(deleteLastEntryButton, false, false, 0);

        historyWindow.Add(vbox);
        historyWindow.SetDefaultSize(400, 300);
        historyWindow.ShowAll();
    }



}

public static class ApplicationState
{
    public static string Operation { get; set; }
    public static double FirstNumber { get; set; }
    public static double SecondNumber { get; set; }
}

