using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace final_prog_part2
{
    //==================================================
    // USERTASK CLASS FOR DATABASE
    //==================================================
    public class UserTask
    {
        public int TaskId { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    //==================================================
    // QUESTION CLASS FOR QUIZ
    //==================================================
    public class Question
    {
        public string QuestionText { get; set; }
        public string[] Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public string Explanation { get; set; }
        public bool IsTrueFalse { get; set; }
    }

    //==================================================
    // MAIN WINDOW
    //==================================================
    public partial class MainWindow : Window
    {
        //==================================================
        // GLOBAL VARIABLES
        //==================================================

        string userName = "";
        string memoryFile = "memory.txt";
        string usersFile = "users.txt";
        string chatHistoryFile = "chatHistory.txt";
        string currentTopic = "";
        Random random = new Random();

        // SQL Server Connection String
        string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=cybersecurity_bot;Integrated Security=True;";

        // Quiz Variables
        List<Question> quizQuestions = new List<Question>();
        int currentQuestionIndex = 0;
        int quizScore = 0;
        bool quizActive = false;

        // Activity Log
        List<string> activityLog = new List<string>();

        //==================================================
        // CYBERSECURITY RESPONSES
        //==================================================

        Dictionary<string, string[]> cyberResponses = new Dictionary<string, string[]>()
        {
            {
                "phishing",
                new string[]
                {
                    "Be careful of fake emails asking for passwords or banking details.",
                    "Always verify suspicious links before clicking them.",
                    "Phishing scams often pretend to be trusted organisations.",
                    "Never share sensitive information through email links."
                }
            },
            {
                "malware",
                new string[]
                {
                    "Install antivirus software to protect your device from malware.",
                    "Avoid downloading files from unknown websites.",
                    "Keep your software updated to reduce malware attacks.",
                    "Malware can damage files and steal personal information."
                }
            },
            {
                "password",
                new string[]
                {
                    "Use strong passwords with symbols and numbers.",
                    "Never reuse passwords across multiple accounts.",
                    "Enable two-factor authentication for extra security.",
                    "Avoid using personal information in passwords."
                }
            },
            {
                "privacy",
                new string[]
                {
                    "Review your social media privacy settings regularly.",
                    "Do not share personal information publicly online.",
                    "Use secure websites that begin with HTTPS.",
                    "Protect your accounts using multi-factor authentication."
                }
            },
            {
                "scam",
                new string[]
                {
                    "Scammers often create urgency to pressure victims.",
                    "Never send money to unknown people online.",
                    "Avoid clicking suspicious advertisements.",
                    "Verify online giveaways before participating."
                }
            },
            {
                "firewall",
                new string[]
                {
                    "A firewall helps block unauthorised access to your computer.",
                    "Always keep your firewall enabled for better protection.",
                    "Firewalls monitor incoming and outgoing traffic.",
                    "A firewall acts as a barrier between your device and threats."
                }
            },
            {
                "vpn",
                new string[]
                {
                    "A VPN helps protect your privacy online.",
                    "VPNs encrypt your internet connection for security.",
                    "Using a VPN on public Wi-Fi is highly recommended.",
                    "VPNs help hide your IP address from attackers."
                }
            }
        };

        //==================================================
        // KEYWORDS
        //==================================================

        Dictionary<string, string[]> topicKeywords = new Dictionary<string, string[]>()
        {
            { "phishing", new string[] { "phishing", "fake email", "suspicious email", "email scam" } },
            { "malware", new string[] { "virus", "trojan", "spyware", "adware", "malware" } },
            { "password", new string[] { "password", "login", "credentials", "passcode" } },
            { "privacy", new string[] { "privacy", "private", "security settings", "personal information" } },
            { "scam", new string[] { "scam", "fraud", "fake prize", "online scam" } },
            { "firewall", new string[] { "firewall", "network security", "protection" } },
            { "vpn", new string[] { "vpn", "secure connection", "public wifi" } }
        };

        //==================================================
        // NLP INTENT KEYWORDS
        //==================================================

        Dictionary<string, string[]> intentKeywords = new Dictionary<string, string[]>()
        {
            {"add_task", new[] { "add task", "new task", "create task", "add reminder", "remind me to", "set reminder" }},
            {"view_tasks", new[] { "show tasks", "view tasks", "list tasks", "my tasks", "display tasks" }},
            {"complete_task", new[] { "complete task", "mark complete", "task done", "finish task" }},
            {"delete_task", new[] { "delete task", "remove task", "clear task" }},
            {"start_quiz", new[] { "start quiz", "play quiz", "take quiz", "begin quiz" }},
            {"show_log", new[] { "activity log", "show log", "what have you done", "show activity" }}
        };

        //==================================================
        // CONSTRUCTOR
        //==================================================

        public MainWindow()
        {
            InitializeComponent();
            PlayVoice();
            InitializeQuizQuestions();
            TestDatabaseConnection();
        }

        //==================================================
        // START BUTTON
        //==================================================

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            WelcomeGrid.Visibility = Visibility.Collapsed;
            NameGrid.Visibility = Visibility.Visible;
        }

        //==================================================
        // SUBMIT USERNAME
        //==================================================

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Name cannot be empty.";
                return;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z]+$"))
            {
                ErrorText.Text = "Please enter a valid name.";
                return;
            }

            userName = name;
            NameGrid.Visibility = Visibility.Collapsed;
            ChatGrid.Visibility = Visibility.Visible;

            bool existingUser = false;
            if (File.Exists(usersFile))
            {
                string[] users = File.ReadAllLines(usersFile);
                foreach (string user in users)
                {
                    if (user.ToLower() == name.ToLower())
                    {
                        existingUser = true;
                    }
                }
            }

            if (existingUser)
            {
                AddBotMessage($"Welcome back {userName}! How may I assist you today?");
                AddBotMessage("💡 Try: 'add task', 'show tasks', 'start quiz', or 'show log'");
            }
            else
            {
                File.AppendAllText(usersFile, userName + "\n");
                AddBotMessage($"Welcome {userName}! How may I assist you today?");
                AddBotMessage("💡 Try: 'add task', 'show tasks', 'start quiz', or 'show log'");
            }
        }

        //==================================================
        // QUICK ACTION BUTTONS
        //==================================================

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string command = btn.Content.ToString();

            if (command.Contains("Show Tasks"))
                MessageTextBox.Text = "show tasks";
            else if (command.Contains("Start Quiz"))
                MessageTextBox.Text = "start quiz";
            else if (command.Contains("Show Log"))
                MessageTextBox.Text = "show log";
            else if (command.Contains("Help"))
                MessageTextBox.Text = "help";

            Send_Click(sender, e);
        }

        //==================================================
        // SEND BUTTON
        //==================================================

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                AddBotMessage("Please enter a message.");
                return;
            }

            AddUserMessage(message);
            string lowerMessage = message.ToLower();

            //--------------------------------------------------
            // MEMORY FEATURE
            //--------------------------------------------------
            if (lowerMessage.Contains("interested in"))
            {
                SaveMemory(lowerMessage);
                MessageTextBox.Clear();
                return;
            }

            if (lowerMessage.Contains("favorite topic"))
            {
                RecallMemory();
                MessageTextBox.Clear();
                return;
            }

            //--------------------------------------------------
            // BOT TYPING EFFECT
            //--------------------------------------------------
            await System.Threading.Tasks.Task.Delay(800);

            //--------------------------------------------------
            // GET CHATBOT RESPONSE
            //--------------------------------------------------
            string response = ChatBotResponse(lowerMessage);
            AddBotMessage(response);
            MessageTextBox.Clear();
        }

        //==================================================
        // PRESS ENTER TO SEND
        //==================================================

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_Click(sender, e);
            }
        }

        //==================================================
        // CHATBOT LOGIC (UPDATED WITH NLP)
        //==================================================

        private string ChatBotResponse(string message)
        {
            //--------------------------------------------------
            // HELP COMMAND
            //--------------------------------------------------
            if (message.Contains("help"))
            {
                return GetHelpMessage();
            }

            //--------------------------------------------------
            // NLP: DETECT INTENT FIRST
            //--------------------------------------------------
            string intent = DetectIntent(message);
            if (!string.IsNullOrEmpty(intent))
            {
                switch (intent)
                {
                    case "add_task": return AddNewTask(message);
                    case "view_tasks": return ShowAllTasks();
                    case "complete_task": return CompleteTask(message);
                    case "delete_task": return DeleteTask(message);
                    case "start_quiz": return StartQuiz();
                    case "show_log": return ShowActivityLog();
                }
            }

            //--------------------------------------------------
            // CHECK FOR QUIZ ANSWER
            //--------------------------------------------------
            if (quizActive)
            {
                string answer = message.Trim();
                if (answer == "a" || answer == "b" || answer == "c" || answer == "d" ||
                    answer == "true" || answer == "false")
                {
                    return ProcessQuizAnswer(answer);
                }
            }

            //--------------------------------------------------
            // EXISTING TOPIC DETECTION
            //--------------------------------------------------
            string sentiment = DetectSentiment(message);
            bool followUp = IsFollowUp(message);
            string topic = DetectTopic(message);

            if (string.IsNullOrEmpty(topic) && followUp && !string.IsNullOrEmpty(currentTopic))
            {
                topic = currentTopic;
            }

            if (!string.IsNullOrEmpty(topic))
            {
                currentTopic = topic;
                return BuildResponse(topic, sentiment);
            }

            if (!string.IsNullOrEmpty(sentiment))
            {
                return GetSentimentSupport(sentiment) + " Tell me which cybersecurity topic is bothering you.";
            }

            //--------------------------------------------------
            // DEFAULT RESPONSE
            //--------------------------------------------------
            return "I'm not sure I understand. Please try rephrasing your question. Type 'help' for options.";
        }

        //==================================================
        // NLP: DETECT INTENT
        //==================================================

        private string DetectIntent(string message)
        {
            foreach (var intent in intentKeywords)
            {
                foreach (string keyword in intent.Value)
                {
                    if (message.Contains(keyword))
                        return intent.Key;
                }
            }
            return "";
        }

        //==================================================
        // HELP MESSAGE
        //==================================================

        private string GetHelpMessage()
        {
            return @"📚 **Available Commands:**

🔐 **Cybersecurity Topics:**
• phishing, malware, password, privacy, scam, firewall, vpn

📋 **Task Assistant:**
• 'add task [title]' - Add a new task
• 'remind me to [task]' - Add task with reminder
• 'show tasks' - View all your tasks
• 'complete task [id]' - Mark task as done
• 'delete task [id]' - Remove a task

🎮 **Quiz:**
• 'start quiz' - Begin cybersecurity quiz
• Answer with: a, b, c, d, true, or false

📊 **Activity Log:**
• 'show log' - View recent activities

🧠 **Memory:**
• 'interested in [topic]' - Save your interest
• 'favorite topic' - Recall saved interest

💡 **Pro Tip:** Try different phrasings like 'remind me to update password tomorrow'";
        }

        //==================================================
        // TASK ASSISTANT METHODS (SQL SERVER VERSION)
        //==================================================

        private string AddNewTask(string message)
        {
            try
            {
                string[] phrases = { "add task", "new task", "create task", "add reminder",
                                    "remind me to", "set reminder" };
                string taskText = message;

                foreach (string phrase in phrases)
                {
                    if (message.Contains(phrase))
                    {
                        taskText = message.Substring(message.IndexOf(phrase) + phrase.Length).Trim();
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(taskText))
                {
                    return "Please specify what task you want to add. Example: 'add task - Review privacy settings'";
                }

                DateTime? reminderDate = ParseReminderDate(taskText);
                string title = taskText;
                string description = "";

                if (taskText.Contains("-"))
                {
                    string[] parts = taskText.Split(new[] { '-' }, 2);
                    title = parts[0].Trim();
                    description = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else if (taskText.Contains(":"))
                {
                    string[] parts = taskText.Split(new[] { ':' }, 2);
                    title = parts[0].Trim();
                    description = parts.Length > 1 ? parts[1].Trim() : "";
                }

                string[] datePhrases = { "tomorrow", "today", "next week", "in ", "days", "day" };
                foreach (string phrase in datePhrases)
                {
                    if (title.Contains(phrase))
                    {
                        int index = title.IndexOf(phrase);
                        title = title.Substring(0, index).Trim();
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(title))
                    title = taskText;

                // SQL Server version
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO tasks (username, title, description, reminder_date) 
                                   VALUES (@username, @title, @description, @reminderDate)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", userName);
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@description", description);
                        cmd.Parameters.AddWithValue("@reminderDate",
                            reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAction($"Task added: '{title}'");

                string response = $"✅ Task added: '{title}'";
                if (!string.IsNullOrEmpty(description))
                    response += $"\n📝 Description: {description}";
                if (reminderDate.HasValue)
                {
                    response += $"\n⏰ Reminder set for: {reminderDate.Value.ToString("dd MMM yyyy HH:mm")}";
                    LogAction($"Reminder set for '{title}' on {reminderDate.Value.ToString("dd MMM yyyy HH:mm")}");
                }
                return response + "\n📋 Type 'show tasks' to view all tasks.";
            }
            catch (Exception ex)
            {
                return $"❌ Error adding task: {ex.Message}";
            }
        }

        private DateTime? ParseReminderDate(string text)
        {
            DateTime now = DateTime.Now;

            if (text.Contains("tomorrow"))
            {
                return now.AddDays(1);
            }
            else if (text.Contains("next week"))
            {
                return now.AddDays(7);
            }
            else if (text.Contains("in "))
            {
                var match = Regex.Match(text, @"in (\d+) days?");
                if (match.Success)
                {
                    int days = int.Parse(match.Groups[1].Value);
                    return now.AddDays(days);
                }
            }
            return null;
        }

        private string ShowAllTasks()
        {
            try
            {
                List<UserTask> tasks = new List<UserTask>();

                // SQL Server version
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT * FROM tasks WHERE username = @username 
                                   ORDER BY is_completed ASC, reminder_date ASC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", userName);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tasks.Add(new UserTask
                                {
                                    TaskId = reader.GetInt32(reader.GetOrdinal("task_id")),
                                    Username = reader.GetString(reader.GetOrdinal("username")),
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                                        ? "" : reader.GetString(reader.GetOrdinal("description")),
                                    ReminderDate = reader.IsDBNull(reader.GetOrdinal("reminder_date"))
                                        ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("reminder_date")),
                                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("created_date"))
                                });
                            }
                        }
                    }
                }

                if (tasks.Count == 0)
                {
                    return "📋 You have no tasks. Add one with 'add task [title]'";
                }

                string result = "📋 **Your Tasks:**\n";
                int completedCount = tasks.Count(t => t.IsCompleted);
                int pendingCount = tasks.Count - completedCount;
                result += $"📊 {pendingCount} pending, {completedCount} completed\n\n";

                foreach (UserTask task in tasks)
                {
                    string status = task.IsCompleted ? "✅ COMPLETED" : "⏳ PENDING";
                    string reminder = task.ReminderDate.HasValue
                        ? $" ⏰ {task.ReminderDate.Value.ToString("dd MMM")}"
                        : "";
                    result += $"ID:{task.TaskId} {status} - {task.Title}{reminder}\n";
                    if (!string.IsNullOrEmpty(task.Description))
                        result += $"   📝 {task.Description}\n";
                }

                result += "\n💡 To complete: 'complete task [id]' or delete: 'delete task [id]'";
                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving tasks: {ex.Message}";
            }
        }

        private string CompleteTask(string message)
        {
            try
            {
                var match = Regex.Match(message, @"(\d+)");
                if (!match.Success)
                    return "Please specify the task ID. Example: 'complete task 1'";

                int taskId = int.Parse(match.Groups[1].Value);
                string title = "";

                // SQL Server version
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string selectQuery = "SELECT title FROM tasks WHERE task_id = @id AND username = @username";
                    using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@id", taskId);
                        selectCmd.Parameters.AddWithValue("@username", userName);
                        using (SqlDataReader reader = selectCmd.ExecuteReader())
                        {
                            if (reader.Read())
                                title = reader.GetString(0);
                            else
                                return $"❌ Task ID {taskId} not found.";
                        }
                    }

                    string query = "UPDATE tasks SET is_completed = 1 WHERE task_id = @id AND username = @username";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);
                        cmd.Parameters.AddWithValue("@username", userName);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAction($"Task completed: '{title}'");
                return $"✅ Task '{title}' marked as completed! Great job! 🎉";
            }
            catch (Exception ex)
            {
                return $"❌ Error completing task: {ex.Message}";
            }
        }

        private string DeleteTask(string message)
        {
            try
            {
                var match = Regex.Match(message, @"(\d+)");
                if (!match.Success)
                    return "Please specify the task ID. Example: 'delete task 1'";

                int taskId = int.Parse(match.Groups[1].Value);
                string title = "";

                // SQL Server version
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string selectQuery = "SELECT title FROM tasks WHERE task_id = @id AND username = @username";
                    using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@id", taskId);
                        selectCmd.Parameters.AddWithValue("@username", userName);
                        using (SqlDataReader reader = selectCmd.ExecuteReader())
                        {
                            if (reader.Read())
                                title = reader.GetString(0);
                            else
                                return $"❌ Task ID {taskId} not found.";
                        }
                    }

                    string query = "DELETE FROM tasks WHERE task_id = @id AND username = @username";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);
                        cmd.Parameters.AddWithValue("@username", userName);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAction($"Task deleted: '{title}'");
                return $"🗑️ Task '{title}' has been deleted.";
            }
            catch (Exception ex)
            {
                return $"❌ Error deleting task: {ex.Message}";
            }
        }

        //==================================================
        // QUIZ METHODS
        //==================================================

        private void InitializeQuizQuestions()
        {
            quizQuestions = new List<Question>
            {
                new Question
                {
                    QuestionText = "What should you do if you receive an email asking for your password?",
                    Options = new[] { "A) Reply with your password", "B) Delete the email",
                                      "C) Report the email as phishing", "D) Ignore it" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Reporting phishing emails helps prevent scams and protects others."
                },
                new Question
                {
                    QuestionText = "True or False: Using the same password for multiple accounts is safe.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Using the same password puts all your accounts at risk if one is compromised.",
                    IsTrueFalse = true
                },
                new Question
                {
                    QuestionText = "What is the best way to create a strong password?",
                    Options = new[] { "A) Use your birthdate", "B) Use a combination of letters, numbers, and symbols",
                                      "C) Use a common word", "D) Use your pet's name" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Strong passwords use a mix of uppercase, lowercase, numbers, and special characters."
                },
                new Question
                {
                    QuestionText = "True or False: You should only download software from official websites.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 0,
                    Explanation = "Official websites are safer as they verify their software for malware.",
                    IsTrueFalse = true
                },
                new Question
                {
                    QuestionText = "What is two-factor authentication (2FA)?",
                    Options = new[] { "A) A password with two letters", "B) Using two different passwords",
                                      "C) A security method requiring two verification steps", "D) A type of malware" },
                    CorrectAnswerIndex = 2,
                    Explanation = "2FA adds an extra layer of security by requiring a second verification method."
                },
                new Question
                {
                    QuestionText = "True or False: Public Wi-Fi is always safe to use without protection.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Public Wi-Fi can be insecure. Always use a VPN on public networks.",
                    IsTrueFalse = true
                },
                new Question
                {
                    QuestionText = "What should you do if you suspect a scam call?",
                    Options = new[] { "A) Share your personal information", "B) Hang up and report the number",
                                      "C) Call them back", "D) Follow their instructions" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Always hang up and report suspicious calls to the authorities."
                },
                new Question
                {
                    QuestionText = "True or False: It's safe to click on links from unknown senders.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Links from unknown senders could lead to phishing or malware sites.",
                    IsTrueFalse = true
                },
                new Question
                {
                    QuestionText = "What is a firewall?",
                    Options = new[] { "A) A physical wall", "B) Software that blocks unauthorized access",
                                      "C) A type of virus", "D) A web browser" },
                    CorrectAnswerIndex = 1,
                    Explanation = "A firewall monitors and controls incoming and outgoing network traffic."
                },
                new Question
                {
                    QuestionText = "True or False: You should update your software regularly.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 0,
                    Explanation = "Regular updates fix security vulnerabilities and protect against threats.",
                    IsTrueFalse = true
                },
                new Question
                {
                    QuestionText = "What is social engineering in cybersecurity?",
                    Options = new[] { "A) Building social networks", "B) Manipulating people to reveal information",
                                      "C) Engineering social media", "D) A type of software" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Social engineering tricks people into giving up confidential information."
                },
                new Question
                {
                    QuestionText = "True or False: HTTPS websites are always completely safe.",
                    Options = new[] { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "HTTPS encrypts data but doesn't guarantee the website is legitimate.",
                    IsTrueFalse = true
                }
            };
        }

        private string StartQuiz()
        {
            if (quizQuestions.Count == 0)
                return "❌ No quiz questions available. Please restart the application.";

            quizActive = true;
            currentQuestionIndex = 0;
            quizScore = 0;
            LogAction($"Quiz started - {quizQuestions.Count} questions");

            return "🎮 **Cybersecurity Quiz Started!**\n\n" + GetCurrentQuestion();
        }

        private string GetCurrentQuestion()
        {
            if (currentQuestionIndex >= quizQuestions.Count)
                return EndQuiz();

            Question q = quizQuestions[currentQuestionIndex];
            string questionNumber = $"**Question {currentQuestionIndex + 1}/{quizQuestions.Count}**";

            if (q.IsTrueFalse)
                return $"{questionNumber}\n{q.QuestionText}\n\nType 'true' or 'false' to answer:";
            else
                return $"{questionNumber}\n{q.QuestionText}\n\n{string.Join("\n", q.Options)}";
        }

        private string ProcessQuizAnswer(string answer)
        {
            if (!quizActive || currentQuestionIndex >= quizQuestions.Count)
                return "The quiz is not active. Type 'start quiz' to begin.";

            Question q = quizQuestions[currentQuestionIndex];
            bool isCorrect = false;
            int userAnswerIndex = -1;

            if (q.IsTrueFalse)
            {
                userAnswerIndex = (answer == "true") ? 0 : 1;
                isCorrect = userAnswerIndex == q.CorrectAnswerIndex;
            }
            else
            {
                string[] letters = { "a", "b", "c", "d" };
                for (int i = 0; i < letters.Length; i++)
                {
                    if (answer == letters[i])
                    {
                        userAnswerIndex = i;
                        isCorrect = userAnswerIndex == q.CorrectAnswerIndex;
                        break;
                    }
                }
            }

            if (userAnswerIndex == -1)
                return "❌ Invalid answer. Please type a, b, c, or d (or true/false).";

            if (isCorrect)
            {
                quizScore++;
                string result = $"✅ **Correct!** 🎉\n\n{q.Explanation}";
                currentQuestionIndex++;
                if (currentQuestionIndex >= quizQuestions.Count)
                    return result + "\n\n" + EndQuiz();
                return result + "\n\n" + GetCurrentQuestion();
            }
            else
            {
                string correctAnswer = q.IsTrueFalse
                    ? (q.CorrectAnswerIndex == 0 ? "True" : "False")
                    : q.Options[q.CorrectAnswerIndex];
                string result = $"❌ **Incorrect.**\n\nCorrect answer: {correctAnswer}\n\n{q.Explanation}";
                currentQuestionIndex++;
                if (currentQuestionIndex >= quizQuestions.Count)
                    return result + "\n\n" + EndQuiz();
                return result + "\n\n" + GetCurrentQuestion();
            }
        }

        private string EndQuiz()
        {
            quizActive = false;
            int total = quizQuestions.Count;
            double percentage = (double)quizScore / total * 100;

            LogAction($"Quiz completed - Score: {quizScore}/{total}");

            string feedback;
            if (percentage >= 90)
                feedback = "🌟 Outstanding! You're a cybersecurity expert!";
            else if (percentage >= 70)
                feedback = "👏 Great job! You have strong cybersecurity knowledge!";
            else if (percentage >= 50)
                feedback = "📚 Good effort! Keep learning to improve your cybersecurity knowledge!";
            else
                feedback = "💪 Keep practicing! Cybersecurity is important for everyone!";

            return $@"🏁 **Quiz Complete!**

📊 Final Score: {quizScore}/{total} ({percentage:F0}%)

{feedback}

Type 'start quiz' to try again!";
        }

        //==================================================
        // ACTIVITY LOG METHODS
        //==================================================

        private void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm")}: {action}";
            activityLog.Add(logEntry);

            if (activityLog.Count > 50)
                activityLog.RemoveAt(0);
        }

        private string ShowActivityLog()
        {
            if (activityLog.Count == 0)
                return "📊 No activities logged yet. Start using the bot to see actions here!";

            string log = "📊 **Recent Activity Log:**\n\n";
            int count = 0;

            for (int i = activityLog.Count - 1; i >= 0 && count < 10; i--, count++)
            {
                log += $"{count + 1}. {activityLog[i]}\n";
            }

            if (activityLog.Count > 10)
                log += $"\n... and {activityLog.Count - 10} more activities (total: {activityLog.Count})";

            return log;
        }

        //==================================================
        // TEST DATABASE CONNECTION
        //==================================================

        private void TestDatabaseConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    LogAction("Database connection successful");
                }
            }
            catch (Exception)
            {
                // Silent fail - will show error when user tries to use tasks
            }
        }

        //==================================================
        // EXISTING METHODS (Unchanged)
        //==================================================

        private string DetectTopic(string message)
        {
            foreach (var topic in topicKeywords)
            {
                if (topic.Value.Any(keyword => message.Contains(keyword)))
                {
                    return topic.Key;
                }
            }
            return "";
        }

        private string BuildResponse(string topic, string sentiment)
        {
            string[] responses = cyberResponses[topic];
            int index = random.Next(responses.Length);
            string randomResponse = responses[index];
            string support = GetSentimentSupport(sentiment);
            return support + " " + randomResponse;
        }

        private string GetSentimentSupport(string sentiment)
        {
            if (sentiment == "worried") return $"Hey {userName}, it is understandable to feel worried.";
            if (sentiment == "frustrated") return $"Hey {userName}, I understand your frustration.";
            if (sentiment == "confused") return $"No worries {userName}, I will explain it clearly.";
            if (sentiment == "curious") return $"Great {userName}! Curiosity helps you learn.";
            if (sentiment == "happy") return $"That is wonderful to hear {userName}!";
            if (sentiment == "sad") return $"I am sorry you are feeling sad {userName}.";
            if (sentiment == "scared") return $"Do not panic {userName}. Cybersecurity problems can be solved.";
            return "";
        }

        private string DetectSentiment(string message)
        {
            if (message.Contains("worried") || message.Contains("nervous") || message.Contains("afraid") || message.Contains("anxious"))
                return "worried";
            if (message.Contains("frustrated") || message.Contains("angry") || message.Contains("annoyed"))
                return "frustrated";
            if (message.Contains("confused") || message.Contains("stuck"))
                return "confused";
            if (message.Contains("curious") || message.Contains("interested"))
                return "curious";
            if (message.Contains("happy") || message.Contains("excited") || message.Contains("great"))
                return "happy";
            if (message.Contains("sad") || message.Contains("upset"))
                return "sad";
            if (message.Contains("scared") || message.Contains("panic"))
                return "scared";
            return "";
        }

        private bool IsFollowUp(string message)
        {
            return message.Contains("tell me more") || message.Contains("more details") ||
                   message.Contains("explain more") || message.Contains("another tip");
        }

        private void SaveMemory(string message)
        {
            string topic = message.Replace("interested in", "").Trim();
            File.WriteAllText(memoryFile, topic);
            AddBotMessage($"I will remember that you are interested in {topic}.");
        }

        private void RecallMemory()
        {
            if (File.Exists(memoryFile))
            {
                string topic = File.ReadAllText(memoryFile);
                AddBotMessage($"Your favorite topic is {topic}.");
            }
            else
            {
                AddBotMessage("I do not know your favorite topic yet.");
            }
        }

        private void AddBotMessage(string message)
        {
            string time = DateTime.Now.ToShortTimeString();
            ChatListBox.AppendText($"[{time}] ChatBot: {message}\n\n");
            SaveChatHistory($"[{time}] ChatBot: {message}");
        }

        private void AddUserMessage(string message)
        {
            string time = DateTime.Now.ToShortTimeString();
            ChatListBox.AppendText($"[{time}] {userName}: {message}\n");
            SaveChatHistory($"[{time}] {userName}: {message}");
        }

        private void SaveChatHistory(string text)
        {
            File.AppendAllText(chatHistoryFile, text + Environment.NewLine);
        }

        private void PlayVoice()
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.wav");
                if (File.Exists(fullPath))
                {
                    SoundPlayer player = new SoundPlayer(fullPath);
                    player.LoadAsync();
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error playing audio:\n" + ex.Message);
            }
        }
    }
}