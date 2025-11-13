using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskTimer.Models;
using TaskTimer.Persistence;

namespace TaskTimer.Services
{

    /// ***************************************************************** ///
    /// Function:   SwitchAction
    /// Summary:    Holds switch action types
    /// Returns:    
    /// ***************************************************************** ///
    public enum SwitchAction { PauseCurrent, StopCurrent }

    /// ***************************************************************** ///
    /// Function:   TaskService
    /// Summary:    Starts/Stops tasks, computers summaries, and persist changes
    /// Returns:    
    /// ***************************************************************** ///
    internal class TaskService
    {
        // Private list of task sessions that this class can change
        private readonly List<TaskSession> _sessions;
        private readonly IClock _clock;

        // Current running task session
        public TaskSession? CurrentSession { get; private set; }

        /// ***************************************************************** ///
        /// Function:   TaskService
        /// Summary:    Starts/Stops tasks, computers summaries, and persist changes
        ///             Allow tests to pass a fake clock; default to system clock
        /// Returns:    
        /// ***************************************************************** ///
        public TaskService(IClock? clock = null)
        {
            //Setup the clock
            _clock = clock ?? new SystemClock();

            //Saves task data to sessions from the JSON file
            _sessions = FileStorage.LoadSessions();

            //If a session has no end time, it will be treated as running
            CurrentSession = _sessions.FirstOrDefault(s => s.EndTime == null);
        }

        /// ***************************************************************** ///
        /// Function:   IReadOnlyList
        /// Summary:    Expose sessions as read-only; callers shouldn't mutate the list directly
        /// Returns:    
        /// ********************************************************
        public IReadOnlyList<TaskSession> GetAllSessions() => _sessions;

        /// ***************************************************************** ///
        /// Function:   StartTask
        /// Summary:    Starts a new task with a given name
        /// Returns:    
        /// ***************************************************************** ///
        public void StartTask(string taskName)
        {
            // If there is already a running task, throw an exception - there can't be multiple running tasks
            if (CurrentSession != null)
            {
                throw new InvalidOperationException(
                    $"A task is already running: '{CurrentSession.TaskName}'. Stop it first.");
            }

            // If the user did not type anything, throw exception - we need to know what the task is
            if (string.IsNullOrWhiteSpace(taskName))
            {
                throw new ArgumentException("Task name cannot be empty.");
            }

            // Creates a new session with the task name and the current time
            var session = new TaskSession
            {
                TaskName = taskName.Trim(),
                StartTime = _clock.UtcNow
            };

            // Saves the session information to memory
            _sessions.Add(session);
            CurrentSession = session;

            Save();
        }

        /// ***************************************************************** ///
        /// Function:   StopCurrentTask
        /// Summary:    Stop the currently running task
        /// Returns:    
        /// ***************************************************************** ///
        public void StopCurrentTask()
        {
            // If there is no currently running section, throw an exception
            if (CurrentSession == null)
            {
                throw new InvalidOperationException("No task is currently running.");
            }

            //Set time
            var now = _clock.UtcNow;
            var elapsed = now - CurrentSession.StartTime;

            //If the time duration was 3 seconds, assume it was an accident and discard the accidental tap
            if (elapsed < TimeSpan.FromSeconds(3))
            {
                //Remove the session
                _sessions.Remove(CurrentSession);
                CurrentSession = null;
                Save();
                return;
            }

            // Otherwise - get the current time and "end" the current task
            CurrentSession.EndTime = now;
            CurrentSession.EndKind = SessionEndKind.Stopped;
            CurrentSession = null;

            Save();
        }

        /// ***************************************************************** ///
        /// Function:   GetSummaryByTask
        /// Summary:    Returns a dictionary that maps each TaskName to total time spent
        /// Returns:    Dictionary
        /// ***************************************************************** ///
        public Dictionary<string, TimeSpan> GetSummaryByTask()
        {
            // Filter sessions that have a duration, group by TaskName, and for each group, sum the minutes
            return _sessions
                .Where(s => s.Duration.HasValue)
                .GroupBy(s => s.TaskName)
                .ToDictionary(
                    g => g.Key, // task name (e.g., "Study")
                    g => TimeSpan.FromMinutes(
                        Math.Round(g.Sum(s => s.Duration!.Value.TotalMinutes)))
                );
        }

        /// ***************************************************************** ///
        /// Function:   ClearAllSessions
        /// Summary:    Clears and resets the current session data
        /// Returns:    
        /// ***************************************************************** ///
        public void ClearAllSessions()
        {
            // Wipe the in-memory list
            _sessions.Clear();

            // Make sure we're not pointing at a deleted session
            CurrentSession = null;

            // Save the now-empty list to disk (overwrites the JSON file)
            Save();
        }

        /// ***************************************************************** ///
        /// Function:   PauseCurrentTask
        /// Summary:    Pauses the current session
        /// Returns:    
        /// ***************************************************************** ///
        public void PauseCurrentTask()
        {
            //If there is no currently active session, state so
            if (CurrentSession == null)
            {
                throw new InvalidOperationException("No task is currently running.");
            }

            //Set time
            var now = _clock.UtcNow;
            var elapsed = now - CurrentSession.StartTime;

            //If the time duration was 3 seconds, assume it was an accident and discard the accidental tap
            if (elapsed < TimeSpan.FromSeconds(3))
            {
                //Remove the session
                _sessions.Remove(CurrentSession);
                CurrentSession = null;
                Save();
                return;
            }

            //End the current chunk of time
            CurrentSession.EndTime = now;
            CurrentSession.EndKind = SessionEndKind.Paused;
            CurrentSession = null;

            Save();
        }

        /// ***************************************************************** ///
        /// Function:   GetKnownTaskNames
        /// Summary:    Provides a list of known and distinct task names
        /// Returns:    A list of know task names (distinct)
        /// ***************************************************************** ///
        public List<string> GetKnownTaskNames()
        {
            //Provide the distinct task names from existing sessions, sorted
            return _sessions
                .Select(s => s.TaskName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        /// ***************************************************************** ///
        /// Function:   GetResumableTaskNames
        /// Summary:    Provides a list of paused distinct task names
        /// Returns:    A list of task names
        /// ***************************************************************** ///
        public List<string> GetResumableTaskNames()
        {

            // Any task currently running (EndTime == null) should NOT appear in the resume list
            var runningNames = _sessions
                .Where(s => s.EndTime == null)
                .Select(s => s.TaskName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            //For each task, look at its latest session - include only if it ended with Paused
            var latestEnded = _sessions
                .Where(s => s.EndTime != null)
                .GroupBy(s => s.TaskName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(s => s.EndTime!.Value).First());

            //Provide the distinct task names from paused sessions, sorted
            return latestEnded
                .Where(s => s.EndKind == SessionEndKind.Paused && !runningNames.Contains(s.TaskName))
                .Select(s => s.TaskName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }

        /// ***************************************************************** ///
        /// Function:   ResumeTask
        /// Summary:    Call start task to resume another task
        /// Returns:    
        /// ***************************************************************** ///
        public void ResumeTask(string taskName)
        {
            // Reuse StartTask logic to enforce "no task already running"
            StartTask(taskName);
        }


        /// ***************************************************************** ///
        /// Function:   SwitchToTask
        /// Summary:    Switches the current task to stop or pause based on users choice
        /// Returns:    
        /// ***************************************************************** ///
        public void SwitchToTask(string taskName, SwitchAction action)
        {
            //If the task name is empty, throw exception
            if (string.IsNullOrWhiteSpace(taskName))
                throw new ArgumentException("Task name cannot be empty.", nameof(taskName));

            //If something is already running -
            if (CurrentSession != null)
            {
                //If the user picked the same task that's already running, do nothing
                if (string.Equals(CurrentSession.TaskName, taskName, StringComparison.OrdinalIgnoreCase))
                    return;

                //Change the running task according to the user's choice
                if (action == SwitchAction.PauseCurrent)
                {
                    PauseCurrentTask();  //sets EndKind = Paused
                }
                else //StopCurrent
                {
                    StopCurrentTask();   //sets EndKind = Stopped
                }
            }

            //Now start the requested task
            StartTask(taskName);
        }

        /// ***************************************************************** ///
        /// Function:   Save
        /// Summary:    Saves data to storage
        /// Returns:    
        /// ***************************************************************** ///
        private void Save()
        {
            // Take everything from memory and place it into storage
            FileStorage.SaveSessions(_sessions);
        }

        /// ***************************************************************** ///
        /// Function:   ExportCsv
        /// Summary:    Takes session data and transfers to a CSV File
        /// Returns:    
        /// ***************************************************************** ///
        public string ExportCsv(string directory)
        {
            //Create the name for the file
            var fileName = $"sessions_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            var path = Path.Combine(directory, fileName);

            var lines = new List<string> { "Date,Task,Start(UTC),Start(Local),End(UTC),End(Local),Duration(min),Status" };

            //Include a human friendly header
            lines.Insert(0, $"Task Timer Export,Generated: {DateTime.Now:G}");
            lines.Insert(1, "");

            //For each session, add a line for each column 
            foreach (var s in _sessions)
            {
                var dur = s.Duration?.TotalMinutes.ToString("F1") ?? "";
                var status = s.EndKind == SessionEndKind.Paused ? "PAUSED"
                           : s.EndTime.HasValue ? "STOPPED" : "RUNNING";
                var startLocal = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Utc).ToLocalTime();
                var endLocal = s.EndTime.HasValue
                    ? DateTime.SpecifyKind(s.EndTime.Value, DateTimeKind.Utc).ToLocalTime()
                    : (DateTime?)null;
                var date = DateOnly.FromDateTime(startLocal);

                lines.Add($"{date}," +
                    $"{Escape(s.TaskName)}," +
                    $"{s.StartTime:o}," +
                    $"{startLocal:G}," +
                    $"{s.EndTime?.ToString("o")}," +
                    $"{endLocal?.ToString("G")}," +
                    $"{dur}," +
                    $"{status}");
            }

            //Get the total for display
            var total = _sessions.Where(s => s.Duration.HasValue)
                     .Sum(s => s.Duration!.Value.TotalMinutes);
            lines.Add("");
            lines.Add($",,,,,Total Active Minutes,{total:F1}");

            //Write the lines to the file
            System.IO.File.WriteAllLines(path, lines);
            return path;

            //Remove commas and slashes to make the lines safe for CSV transfer
            static string Escape(string v) =>
                v.Contains(',') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;

        }
    }
}
