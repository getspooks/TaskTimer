using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTimer.Services
{
    /// ***************************************************************** ///
    /// Function:   IClock
    /// Summary:    Switches standard time to UTC
    /// Returns:    
    /// ***************************************************************** ///
    public interface IClock
    {
        //Set the date time to UTC standard
        DateTime UtcNow { get; }
    }

    //Built in clock
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
