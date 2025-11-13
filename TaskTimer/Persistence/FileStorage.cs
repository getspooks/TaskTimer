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
            var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
            var tmp = FileName + ".tmp";
            var bak = FileName + ".bak";

            //Write to the temp file first
            File.WriteAllText(tmp, json);

            try
            {
                //If the file exists, atomically replace and create/overwrite backup if platform supports it
                if (File.Exists(FileName))
                {
                    File.Replace(tmp, FileName, bak, ignoreMetadataErrors: true);
                }
                else //Otherwise, rename the temp save file to the main file name 
                {
                    File.Move(tmp, FileName, overwrite: true);
                }
            }
            catch
            {
                //Fallback path if Replace fails on some FS
                try
                {
                    if (File.Exists(FileName))
                        File.Copy(FileName, bak, overwrite: true);
                }
                catch { /* best effort */ }

                //Rename the temp save file to the main file name
                File.Move(tmp, FileName, overwrite: true);
            }
            finally
            {
                //If the temp file exists
                if (File.Exists(tmp))
                {
                    //delete the temp file
                    try { File.Delete(tmp); } catch { /* ignore */ }
                }

            }
        }
    }
}
