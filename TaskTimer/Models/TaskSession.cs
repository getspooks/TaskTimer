using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace TaskTimer.Models
{
    /// ***************************************************************** ///
    /// Function:   SessionEndKind
    /// Summary:    Categorizes whether tasks were stopped or paused
    /// Returns:    
    /// ***************************************************************** ///
    public enum SessionEndKind { Stopped = 0, Paused = 1 }

    /// ***************************************************************** ///
    /// Function:   TaskSession
    /// Summary:    represents one "session" of work on a given task, holds session data
    /// Returns:    
    /// ***************************************************************** ///
    public class TaskSession
    {

        //Get a Unique ID for the "session"
        public Guid UID { get; set; } = Guid.NewGuid();

        //This will store the human-readable name of the task 
        public string TaskName { get; set; } = string.Empty;


        //Store date times 
        public DateTime StartTime { get; set; }             //Get the start time for the task - UTC
        public DateTime? EndTime { get; set; }              //Get the end time for the task - UTC or null

        //Compute the duration - time between start and end times
        public TimeSpan? Duration =>
            EndTime.HasValue ? EndTime.Value - StartTime : null;

        //Determine whether a task was stopped or paused
        public SessionEndKind? EndKind { get; set; }  // null for old data


        /// ***************************************************************** ///
        /// Function:   ToString
        /// Summary:    Print gathered data to console, for user
        /// Returns:    Task Name, Start Time, End Time, Duration
        /// ***************************************************************** ///
        public override string ToString()
        {
            //Get the local start time for the user
            var localStart = DateTime.SpecifyKind(StartTime, DateTimeKind.Utc).ToLocalTime();

            string endText;

            //If there is an entime, format this - Otherwise use a "-" placeholder
            if (EndTime.HasValue)
            {
                var localEnd = DateTime.SpecifyKind(EndTime.Value, DateTimeKind.Utc).ToLocalTime();
                endText = localEnd.ToString("G");
            }
            else
            {
                endText = "-";
            }

            var status = EndKind == SessionEndKind.Paused ? "[PAUSED]"
                       : EndTime.HasValue ? "[STOPPED]"
                       : "[RUNNING]";

            //If there is a duration, show this - Otherwise show as running
            var durText = Duration.HasValue ? $"{Duration.Value.TotalMinutes:F1} min" : "RUNNING";

            //Print the "session" task data on one line
            return $"{status} {TaskName} | Start: {localStart:G} | End: {endText} | {durText}";
        }
    }
}
