using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public class LeakMonitor
    {
        // TODO Monitor # Bytes in all Heaps counter, when a significant increase over time is detected,
        //      use CLRMD to capture heap snapshots (top types + counts + sizes) over timed intervals,
        //      and then tell the user which types are growing in count/size.
        //      This can be used for more sophisticated investigations like capturing the whole heap snapshot
        //      and then looking for roots that retain a lot of objects, and so on.
    }
}
