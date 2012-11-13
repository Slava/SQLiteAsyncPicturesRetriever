using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ControlHelper
{
    public static class ControlHelper
    {
        delegate void UniversalVoidDelegate();

        /// <summary>
        /// Call form controll action from different thread
        /// </summary>
        public static void ControlInvoke(Control control, Action function)
        {
            try
            {
                if (control.IsDisposed || control.Disposing)
                    return;

                if (control.InvokeRequired)
                {
                    control.Invoke(new UniversalVoidDelegate(() => ControlInvoke(control, function)));
                    return;
                }
                function();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
