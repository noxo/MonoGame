#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
#if ANDROID
using OpenTK.Graphics;
using OpenTK.Platform;
#endif
#if IPHONE
using MonoTouch.Foundation;
using MonoTouch.OpenGLES;
#if ES11
using OpenTK.Graphics.ES11;
#else
using OpenTK.Graphics.ES20;
#endif
#endif

namespace Microsoft.Xna.Framework
{
    internal class Threading
    {
        static int mainThreadId;
#if MONOMAC || ANDROID || LINUX || WINDOWS
        static List<Action> actions = new List<Action>();
#elif IPHONE
        public static EAGLContext BackgroundContext;
#endif

        internal static void Initialize()
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Runs the given action on the UI thread and blocks the current thread while the action is running.
        /// If the current thread is the UI thread, the action will run immediately.
        /// </summary>
        /// <param name="action">The action to be run on the UI thread</param>
        internal static void BlockOnUIThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action cannot be null");

            // If we are on the main thread, just call the action and return
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (mainThreadId == threadId)
            {
                action();
                return;
            }

#if IPHONE
            lock (BackgroundContext)
            {
                if (EAGLContext.CurrentContext != BackgroundContext)
                    EAGLContext.SetCurrentContext(BackgroundContext);
                action();
            }
#else
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            try
            {
                Add(() =>
                {
                    action();
                    resetEvent.Set();
                });
                resetEvent.Wait();
            }
            finally
            {
                resetEvent.Dispose();
            }
#endif
        }

#if MONOMAC || ANDROID || LINUX || WINDOWS
        static void Add(Action action)
        {
            lock (actions)
            {
                actions.Add(action);
            }
        }

        /// <summary>
        /// Runs all pending actions.  Must be called from the UI thread.
        /// </summary>
        internal static void Run()
        {
            Action action;
            lock (actions)
            {
                action = actions.Count > 0 ? actions[0] : null;
            }
            while (action != null)
            {
                action();
                lock (actions)
                {
                    actions.RemoveAt(0);
                    action = actions.Count > 0 ? actions[0] : null;
                }
            }
        }
#endif
    }
}
