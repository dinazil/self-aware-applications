using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public class DeadlockMonitor
    {
        // TODO Periodically check if there's a deadlock by walking thread wait chains
        //      and looking for cycles. Show the simple version using only CLRMD BlockingObject
        //      here, and explain that msos has support for additional synchronization types.

        public static DeadlockMonitor Start()
        {
            return new DeadlockMonitor();
        }
    }
}
