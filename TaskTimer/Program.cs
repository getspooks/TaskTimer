using System;
using System.Linq;
using TaskTimer.Services;

namespace TaskTimer
{
    internal class Program
    {
        /// ***************************************************************** ///
        /// Function:   Main
        /// Summary:    Entrypoint - shows menu, gets reader input, calls taskservice methods and displays output
        /// Returns:    
        /// ***************************************************************** ///
        private static void Main(string[] args)
        {
            // Create the service once and reuse it.
            // It will load existing sessions from disk.
            var service = new TaskService();

            // Show the menu loop.
            RunMenu(service);
        }

        /// ***************************************************************** ///
        /// Function:   RunMenu
        /// Summary:    Main menu loop: show options, handle input, repeat
        /// Returns:    
        /// ***************************************************************** ///
        private static void RunMenu(TaskService service)
        {
            while (true)
            {
                // Program title
                Console.Clear();
                Console.WriteLine("*=--- Task Timer ---=*");
                Console.WriteLine();

                // Show current running task status at the top - Otherwise, share that nothing is running
                if (service.CurrentSession != null)
                {
                    // Show the current task as well as the time elapsed since the start (from current time)
                    var running = service.CurrentSession;
                    var elapsed = DateTime.Now - running.StartTime;
                    Console.WriteLine($"Currently running: {running.TaskName} ({elapsed.TotalMinutes:F1} min)");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("No task is currently running.");
                    Console.WriteLine();
                }

                // Display menu options
                Console.WriteLine("1) Start new task");
                Console.WriteLine("2) Stop current task");
                Console.WriteLine("3) List all sessions");
                Console.WriteLine("4) Show summary by task");
                Console.WriteLine("5) Clear all session data");
                Console.WriteLine("6) Exit");
                Console.WriteLine();
                Console.Write("Choose an option: ");

                // Read user input as a string, avoid crashing on null.
                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            StartNewTask(service);
                            break;
                        case "2":
                            StopTask(service);
                            break;
                        case "3":
                            ListSessions(service);
                            break;
                        case "4":
                            ShowSummary(service);
                            break;
                        case "5":
                            ClearAllSessions(service);
                            break;
                        case "6":
                            // Return exits Main, so the app ends.
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Press any key to continue...");
                            Console.ReadKey();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Catch any exception thrown by TaskService (or other code),
                    // show it to the user, then keep the app running.
                    Console.WriteLine();
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        /// ***************************************************************** ///
        /// Function:   StartNewTask
        /// Summary:    UI Handler for starting a new task
        /// Returns:    
        /// ***************************************************************** ///
        private static void StartNewTask(TaskService service)
        {
            //Asks the user for the task name
            Console.WriteLine();
            Console.Write("Enter task name: ");
            var name = Console.ReadLine() ?? string.Empty;

            //Calls start task from taskservices
            service.StartTask(name);
            Console.WriteLine("Task started. Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   StopTask
        /// Summary:    UI handler for stopping the currently running task
        /// Returns:    
        /// ***************************************************************** ///
        private static void StopTask(TaskService service)
        {
            //calls stop task from taskservices
            Console.WriteLine();
            service.StopCurrentTask();
            Console.WriteLine("Task stopped. Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   ListSessions
        /// Summary:    UI Handler for listing all sessions
        /// Returns:    
        /// ***************************************************************** ///
        private static void ListSessions(TaskService service)
        {
            //asks task services for the sessions
            Console.WriteLine();
            var sessions = service.GetAllSessions()
                                  .OrderByDescending(s => s.StartTime)
                                  .ToList();

            //If there are no sessions, say so - otherwise print each session
            if (!sessions.Any())
            {
                Console.WriteLine("No sessions recorded yet.");
            }
            else
            {
                foreach (var s in sessions)
                {
                    Console.WriteLine(s);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   ShowSummary
        /// Summary:    UI handler for showing the summary per task name
        /// Returns:    
        /// ***************************************************************** ///
        private static void ShowSummary(TaskService service)
        {
            //calls task services to get summary
            Console.WriteLine();
            var summary = service.GetSummaryByTask();

            // If there is a summary, say so - Otherwise print the summary
            if (!summary.Any())
            {
                Console.WriteLine("No completed sessions to summarize.");
            }
            else
            {
                foreach (var kvp in summary.OrderByDescending(kvp => kvp.Value))
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value.TotalMinutes:F1} min ({kvp.Value})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   ClearAllSessions
        /// Summary:    UI handler for clearing all session data
        /// Returns:    
        /// ***************************************************************** ///
        private static void ClearAllSessions(TaskService service)
        {
            // Get confirmation before clearing data 
            Console.WriteLine();
            Console.WriteLine("WARNING: This will permanently delete ALL recorded sessions.");
            Console.Write("Type YES to confirm, or anything else to cancel: ");

            // 
            var input = Console.ReadLine() ?? string.Empty;

            // If the user confirms deletion, clear session data - Otherwise, cancel
            if (input.Equals("YES", StringComparison.OrdinalIgnoreCase) || input.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                service.ClearAllSessions();
                Console.WriteLine("All session data has been cleared.");
            }
            else
            {
                Console.WriteLine("Cancelled. No data was deleted.");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}

