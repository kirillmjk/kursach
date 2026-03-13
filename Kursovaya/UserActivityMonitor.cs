using Kursovaya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BoatRent
{
    class UserActivityMonitor
    {
        public static DateTime LastActivity = DateTime.Now;
        public static int TimeoutSeconds = 30;
        private static Timer timer;

        public static void Start()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();

            Application.AddMessageFilter(new ActivityMessageFilter());
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
            if((DateTime.Now - LastActivity).TotalSeconds > TimeoutSeconds)
            {
                timer.Stop();
                MessageBox.Show("Сессия истекла из-за неактивности", "Сессия", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                foreach(Form f in Application.OpenForms)
                {
                    if (!(f is Auth)) f.Hide();
                }
                new Auth().ShowDialog();
            }
        }
    }
}
