using NonInvasiveKeyboardHookLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CognitiveLoadMeasure
{
    class ButtonHook
    {
        KeyboardHookManager keyboardHookManager;
        public ButtonHook()
        {
            keyboardHookManager = new KeyboardHookManager();
            keyboardHookManager.Start();
        }

        public void RegisterHotKey()
        {
            keyboardHookManager.RegisterHotkey(0x20, () =>
            {
                Debug.WriteLine("Space detected");
                OnKeyPressed(EventArgs.Empty);
            });
        }

        /// <summary>
        /// event that notifies that the hittest was detected
        /// </summary>
        public event EventHandler KeyPressed;
        protected virtual void OnKeyPressed(EventArgs e)
        {
            EventHandler handler = KeyPressed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
