using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace TaskTimer.Models
{
    /// ***************************************************************** ///
    /// Function:   TaskSession
    /// Summary:    represents one "session" of work on a given task, holds session data
    /// Returns:    
    /// ***************************************************************** ///
    public class TaskSession
    {

        //Get a Unique ID for the "session"
        public Guid UID { get; set; } = Guid.NewGuid();

        // This will store the human-readable name of the task 
        public string TaskName { get; set; } = string.Empty;

        // Get the start time for the task
        public DateTime StartTime { get; set; }

        // Get the end time for the task
        public DateTime? EndTime { get; set; }

        // Compute the duration - time between start and end times
        public TimeSpan? Duration =>
            EndTime.HasValue ? EndTime.Value - StartTime : null;



        /// ***************************************************************** ///
        /// Function:   ToString
        /// Summary:    Print gathered data to console
        /// Returns:    Task Name, Start Time, End Time, Duration
        /// ***************************************************************** ///
        public override string ToString()
        {
            //If there is a duration, show this - Otherwise show as running
            var durationText = Duration.HasValue
                            ? $"{Duration.Value.TotalMinutes:F1} min"
                            : "RUNNING";

            // If there is an entime, format this - Otherwise use a "-" placeholder
            var endText = EndTime.HasValue
                ? EndTime.Value.ToString("G")
                : "-";

            //Print the "session" task data on one line
            return $"{TaskName} | Start: {StartTime:G} | End: {endText} | {durationText}";
        }
    }
}
