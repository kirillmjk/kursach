using System;
using System.Windows.Forms;

namespace BoatRent
{
    public class ActivityMessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_KEYDOWN = 0x100;
            if(m.Msg == WM_MOUSEMOVE || m.Msg == WM_KEYDOWN)
            {
                UserActivityMonitor.LastActivity = DateTime.Now;
            }
            return false;
        }
    }
}
