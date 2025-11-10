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
    /// Function:   TaskService
    /// Summary:    Starts/Stops tasks, computers summaries, and persist changes
    /// Returns:    
    /// ***************************************************************** ///
    internal class TaskService
    {
        // Private list of task sessions that this class can change
        private readonly List<TaskSession> _sessions;

        // Current running task session
        public TaskSession? CurrentSession { get; private set; }

        /// ***************************************************************** ///
        /// Function:   TaskService
        /// Summary:    Starts/Stops tasks, computers summaries, and persist changes
        /// Returns:    
        /// ***************************************************************** ///
        public TaskService()
        {
            // Saves task data to sessions from the JSON file
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
                StartTime = DateTime.Now
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

            // Otherwise - get the current time and "end" the current task
            CurrentSession.EndTime = DateTime.Now;
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
                        g.Sum(s => s.Duration!.Value.TotalMinutes))
                );
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

    }
}
