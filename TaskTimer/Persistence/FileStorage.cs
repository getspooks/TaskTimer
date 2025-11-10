using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using TaskTimer.Models;

namespace TaskTimer.Persistence
{
    /// ***************************************************************** ///
    /// Function:   FileStorage
    /// Summary:    Handles saving and loading task session objects to/from a JSON file
    /// Returns:    
    /// ***************************************************************** ///

    public static class FileStorage
    {
        //File name used to store the sessions
        private const string FileName = "tasksessions.json";

        /// ***************************************************************** ///
        /// Function:   List<TaskSession> LoadSessions
        /// Summary:    Load all task sessions from the JSON file
        /// Returns:    Task Session Data or new list
        /// ***************************************************************** ///
        public static List<TaskSession> LoadSessions()
        {
            // If there is no file yet, make one
            if (!File.Exists(FileName))
            {
                return new List<TaskSession>();
            }

            try
            {
                // Read the JSON text file into a string 
                var json = File.ReadAllText(FileName);

                // Deserialize the list from JSON text into a TaskSession object
                var sessions = JsonSerializer.Deserialize<List<TaskSession>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                // If somehow deserialization returns null, fall back to empty list.
                return sessions ?? new List<TaskSession>();
            }
            catch (Exception)
            {
                // If something goes wrong, return an empty list
                return new List<TaskSession>();
            }
        }

        /// ***************************************************************** ///
        /// Function:   SaveSessions
        /// Summary:    Save all task sessions to the JSON file
        /// Returns:    
        /// ***************************************************************** ///
        public static void SaveSessions(List<TaskSession> sessions)
        {
            // Serialize the list. WriteIndented to make the JSON nicely formatted.
            var json = JsonSerializer.Serialize(
                sessions,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            // Write to disk in one go.
            File.WriteAllText(FileName, json);
        }
    }
}
