using System;
using System.IO;
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
                    var duration = DateTime.UtcNow - service.CurrentSession.StartTime;
                    Console.WriteLine($"Currently running: {running.TaskName} ({duration.TotalMinutes:F1} min)");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("No task is currently running.");
                    Console.WriteLine();
                }

                // Display menu options
                Console.WriteLine("1) Start new task");
                Console.WriteLine("2) Pause current task");
                Console.WriteLine("3) Stop current task");
                Console.WriteLine("4) Resume task");
                Console.WriteLine("5) List all sessions");
                Console.WriteLine("6) Show summary by task");
                Console.WriteLine("7) Clear all session data");
                Console.WriteLine("8) Export Sessions to CSV");
                Console.WriteLine("9) Exit");
                Console.WriteLine();
                Console.Write("Choose an option: ");

                // Read user input as a string, avoid crashing on null.
                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": //Start a new task
                            StartNewTask(service);
                            break;
                        case "2": //Pause current task 
                            PauseTask(service);
                            break;
                        case "3": //Stop current task
                            StopTask(service);
                            break;
                        case "4": //Resume task
                            ResumeTask(service);
                            break;
                        case "5": //List all sessions
                            ListSessions(service);
                            break;
                        case "6": //Show summary by task
                            ShowSummary(service);
                            break;
                        case "7": //Clear all session data
                            ClearAllSessions(service);
                            break;
                        case "8": //Export Sessions to CSV
                            ExportCsv(service);
                            break;
                        case "9": //Exit (main)
                            return;
                        default: //Invalid
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

            //If there is a currently running task -
            if (service.CurrentSession != null)
            {
                //Prompt the user for how they would like to handle this
                Console.WriteLine();
                Console.WriteLine($"A task is currently running: '{service.CurrentSession.TaskName}'.");
                Console.WriteLine("What do you want to do with it?");
                Console.WriteLine("1) Pause it");
                Console.WriteLine("2) Stop it");
                Console.WriteLine("3) Cancel");
                Console.Write("Choose: ");

                var choice = Console.ReadLine() ?? string.Empty;

                //Cancel the task
                if (choice == "3")
                {
                    Console.WriteLine("Cancelled. Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                //Proceed based on user input (pause/stop)
                try
                {
                    var action = choice == "1" ? SwitchAction.PauseCurrent : SwitchAction.StopCurrent;
                    service.SwitchToTask(name, action);  
                    Console.WriteLine($"Switched to '{name}'. Press any key to continue...");
                    Console.ReadKey();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ReadKey();
                    return;
                }
            }

            //If nothing is running, start clean
            try
            {
                //Calls start task from taskservices
                service.StartTask(name);
                Console.WriteLine("Task started. Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
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

            var summary = service.GetSummaryByTask(includeRunning: true);

            // If there is a summary, say so - Otherwise print the summary
            if(!summary.Any())
    {
                Console.WriteLine("No sessions to summarize.");
            }
            else
            {
                foreach (var kvp in summary.OrderBy(k => k.Key))
                    Console.WriteLine($"{kvp.Key}: {kvp.Value.TotalMinutes:F1} min ({kvp.Value:hh\\:mm\\:ss})");
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

        /// ***************************************************************** ///
        /// Function:   PauseTask
        /// Summary:    UI handler for pausing a task
        /// Returns:    
        /// ***************************************************************** ///
        private static void PauseTask(TaskService service)
        {
            //calls task services to pause the currently running task
            Console.WriteLine();
            service.PauseCurrentTask();
            Console.WriteLine("Task paused. Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   ResumeTask
        /// Summary:    UI handler for resuming a task
        /// Returns:    
        /// ***************************************************************** ///
        private static void ResumeTask(TaskService service)
        {
            Console.WriteLine();

            //Get the list of known distinct task names
            var names = service.GetResumableTaskNames();

            //If there are no distinct names, state so, and go back to menu
            if (!names.Any())
            {
                Console.WriteLine("No past tasks to resume yet.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            //Otherwise, for each distinct task name - show tasks which could be resumed with a number value for selection
            Console.WriteLine("Select a task to resume:");
            for (int i = 0; i < names.Count; i++)
            {
                Console.WriteLine($"{i + 1}) {names[i]}");
            }

            //Promt the user for the task number
            Console.WriteLine();
            Console.Write("Enter number: ");
            var input = Console.ReadLine() ?? string.Empty;

            //If the number is outside of the given, state so and exit to menu
            if (!int.TryParse(input, out int index) || index < 1 || index > names.Count)
            {
                Console.WriteLine("Invalid selection. Press any key to continue...");
                Console.ReadKey();
                return;
            }

            //Get the task name based on the number provided 
            var taskName = names[index - 1];

            //If there is a currently running task -
            if (service.CurrentSession != null)
            {
                //Prompt the user for how they would like to handle this
                Console.WriteLine();
                Console.WriteLine($"A task is currently running: '{service.CurrentSession.TaskName}'.");
                Console.WriteLine("What do you want to do with the current task?");
                Console.WriteLine("1) Pause it");
                Console.WriteLine("2) Stop it");
                Console.WriteLine("3) Cancel");
                Console.Write("Choose: ");
                var choice = Console.ReadLine() ?? string.Empty;

                //Cancel the action
                if (choice == "3")
                {
                    Console.WriteLine("Cancelled. Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                //If the user selects pause, then pause the current running task - Otherwise, stop the task
                try
                {
                    var action = choice == "1" ? SwitchAction.PauseCurrent : SwitchAction.StopCurrent;
                    service.SwitchToTask(taskName, action);
                    Console.WriteLine($"Switched to '{taskName}'. Press any key to continue...");
                    Console.ReadKey();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                service.ResumeTask(taskName);
            }

            Console.WriteLine($"Now running: {service.CurrentSession?.TaskName}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// ***************************************************************** ///
        /// Function:   ExportCsv
        /// Summary:    UI handler for exporting to CSV
        /// Returns:    
        /// ***************************************************************** ///
        private static void ExportCsv(TaskService service)
        {
            //Create the file and export to a dedicated folder
            var dir = Path.Combine(Environment.CurrentDirectory, "Exports");
            Directory.CreateDirectory(dir); // auto-creates folder if missing
            var result = service.ExportCsv(dir);

            // Run the export logic in task service
            var path = service.ExportCsv(dir);

            Console.WriteLine();
            Console.WriteLine("Export successful!");
            Console.WriteLine($"File saved to: {path}");

            // Autoopen the CSV file
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if user doesn't have Excel or file associations
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}

