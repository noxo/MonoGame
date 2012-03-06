using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Runtime;
using OpenTK.Graphics;
using OpenTK.Platform;
using Javax.Microedition.Khronos.Egl;
using Microsoft.Xna.Framework.Graphics;
using Android.Views;
using System.Diagnostics;

namespace Microsoft.Xna.Framework
{
    class AndroidGraphicsContext : IGraphicsContext
    {
        GraphicsMode graphicsMode;
        bool disposed;
        bool current;
        IEGL10 egl;
        internal EGLDisplay eglDisplay;
        EGLSurface eglSurface;
        internal EGLContext eglContext;
        internal EGLConfig eglConfig;

        public AndroidGraphicsContext(GraphicsMode mode, View view)
        {
            graphicsMode = mode;
            Initialize(view);
        }

        public AndroidGraphicsContext(IGraphicsContext master)
        {
            graphicsMode = master.GraphicsMode;
            InitializeShared((AndroidGraphicsContext)master);
        }

        ~AndroidGraphicsContext()
        {
            Dispose(false);
        }

        void Initialize(View view)
        {
            egl = EGLContext.EGL.JavaCast<IEGL10>();
            eglDisplay = egl.EglGetDisplay(EGL10.EglDefaultDisplay);
            if (eglDisplay == EGL10.EglNoDisplay)
                throw new NotSupportedException("Could not get default EGL display");

            if (!egl.EglInitialize(eglDisplay, new int[] { 2, 0 }))
            {
                ThrowEglError("Failed to initialize EGL");
            }

            // This constant appears to be missing from EGL10
            int EglOpenGLES2Bit = 4;
            int[] configSpec = new int[]
            {
                EGL10.EglRenderableType, EglOpenGLES2Bit,
                EGL10.EglRedSize, graphicsMode.ColorFormat.Red,
                EGL10.EglGreenSize, graphicsMode.ColorFormat.Green,
                EGL10.EglBlueSize, graphicsMode.ColorFormat.Blue,
                EGL10.EglDepthSize, graphicsMode.Depth,
                EGL10.EglStencilSize, graphicsMode.Stencil,
                EGL10.EglNone
            };

            // Get the configs supported by this display
            eglConfig = null;
            int[] numConfigs = new int[1];
            if (!egl.EglChooseConfig(eglDisplay, configSpec, null, 0, numConfigs) || (numConfigs[0] == 0))
                ThrowEglError("Failed to get number of matching configs");
            EGLConfig[] configs = new EGLConfig[numConfigs[0]];
            if (!egl.EglChooseConfig(eglDisplay, configSpec, configs, configs.Length, numConfigs) || (numConfigs[0] == 0))
                ThrowEglError("Failed to get matching configs");
            // Iterate through the configs looking for one that works
            int[] value = new int[1];
            int EglContextClientVersion = 0x3098;
            int[] contextSpec = new int[]
            {
                EglContextClientVersion, 2,
                EGL10.EglNone
            };
            for (int i = 0; (i < numConfigs[0]) && (eglContext == null); ++i)
            {
                eglSurface = egl.EglCreateWindowSurface(eglDisplay, configs[i], view, null);
                if (eglSurface == EGL10.EglNoSurface)
                    continue;

                eglContext = egl.EglCreateContext(eglDisplay, configs[i], EGL10.EglNoContext, contextSpec);
                if (eglContext == EGL10.EglNoContext)
                {
                    egl.EglDestroySurface(eglDisplay, eglSurface);
                    eglSurface = null;
                    continue;
                }

                if (!egl.EglMakeCurrent(eglDisplay, eglSurface, eglSurface, eglContext))
                {
                    egl.EglDestroyContext(eglDisplay, eglContext);
                    eglContext = null;
                    egl.EglDestroySurface(eglDisplay, eglSurface);
                    eglSurface = null;
                    continue;
                }

                eglConfig = configs[i];
            }
            if (eglContext == null)
                throw new NotSupportedException("Failed to find a supported config");

#if DEBUG
            if (!egl.EglGetConfigAttrib(eglDisplay, eglConfig, EGL10.EglRedSize, value))
                ThrowEglError("Failed to retrieve EGL_RED_SIZE attribute");
            Debug.WriteLine("EGL_RED_SIZE = {0}", value[0]);
            if (!egl.EglGetConfigAttrib(eglDisplay, eglConfig, EGL10.EglGreenSize, value))
                ThrowEglError("Failed to retrieve EGL_GREEN_SIZE attribute");
            Debug.WriteLine("EGL_GREEN_SIZE = {0}", value[0]);
            if (!egl.EglGetConfigAttrib(eglDisplay, eglConfig, EGL10.EglBlueSize, value))
                ThrowEglError("Failed to retrieve EGL_BLUE_SIZE attribute");
            Debug.WriteLine("EGL_BLUE_SIZE = {0}", value[0]);
            if (!egl.EglGetConfigAttrib(eglDisplay, eglConfig, EGL10.EglDepthSize, value))
                ThrowEglError("Failed to retrieve EGL_DEPTH_SIZE attribute");
            Debug.WriteLine("EGL_DEPTH_SIZE = {0}", value[0]);
            if (!egl.EglGetConfigAttrib(eglDisplay, eglConfig, EGL10.EglStencilSize, value))
                ThrowEglError("Failed to retrieve EGL_STENCIL_SIZE attribute");
            Debug.WriteLine("EGL_STENCIL_SIZE = {0}", value[0]);
#endif
        }

        void InitializeShared(AndroidGraphicsContext master)
        {
            egl = EGLContext.EGL.JavaCast<IEGL10>();
            eglDisplay = master.eglDisplay;
            eglConfig = master.eglConfig;

            eglSurface = egl.EglCreatePbufferSurface(eglDisplay, eglConfig, null);
            if (eglSurface == EGL10.EglNoSurface)
                ThrowEglError("Failed to create EGL window surface");

            int EglContextClientVersion = 0x3098;
            int[] contextSpec = new int[]
            {
                EglContextClientVersion, 2,
                EGL10.EglNone
            };
            eglContext = egl.EglCreateContext(eglDisplay, eglConfig, master.eglContext, contextSpec);
            if (eglContext == EGL10.EglNoContext)
                ThrowEglError("Failed to create background EGL context");
            Debug.WriteLine("Created shared context");
        }

        void ThrowEglError(string message)
        {
            ThrowEglError(message, egl.EglGetError());
        }

        void ThrowEglError(string message, int error)
        {
            string errorDesc;
            switch (error)
            {
                case EGL10.EglBadDisplay:
                    errorDesc = "EGL_BAD_DISPLAY";
                    break;
                case EGL10.EglNotInitialized:
                    errorDesc = "EGL_NOT_INITIALIZED";
                    break;
                case EGL10.EglBadNativePixmap:
                    errorDesc = "EGL_BAD_NATIVE_PIXMAP";
                    break;
                case EGL10.EglBadNativeWindow:
                    errorDesc = "EGL_BAD_NATIVE_WINDOW";
                    break;
                case EGL10.EglBadMatch:
                    errorDesc = "EGL_BAD_MATCH";
                    break;
                case EGL10.EglBadConfig:
                    errorDesc = "EGL_BAD_CONFIG";
                    break;
                case EGL10.EglBadAlloc:
                    errorDesc = "EGL_BAD_ALLOC";
                    break;
                case EGL10.EglBadParameter:
                    errorDesc = "EGL_BAD_PARAMETER";
                    break;
                case EGL10.EglBadAttribute:
                    errorDesc = "EGL_BAD_ATTRIBUTE";
                    break;
                default:
                    errorDesc = String.Format("0x{0:08x}", error);
                    break;
            }
            throw new InvalidOperationException(String.Format("{0} ({1})", message, errorDesc));
        }

        public bool ErrorChecking
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public GraphicsMode GraphicsMode
        {
            get { return graphicsMode; }
        }

        public bool IsCurrent
        {
            get { return current; }
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }

        public void MakeCurrent(IWindowInfo window)
        {
            if (!egl.EglMakeCurrent(eglDisplay, EGL10.EglNoSurface, EGL10.EglNoSurface, EGL10.EglNoContext))
                ThrowEglError("Failed to clear the context current");
            current = false;
            if (window != null)
            {
                if (!egl.EglMakeCurrent(eglDisplay, eglSurface, eglSurface, eglContext))
                    ThrowEglError("Failed to make the context current");
                current = true;
            }
        }

        public void SwapBuffers()
        {
            if (!egl.EglSwapBuffers(eglDisplay, eglSurface))
                ThrowEglError("Failed to swap buffers");
        }

        public void Update(IWindowInfo window)
        {
        }

        public bool VSync
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // If disposing is true, it was called explicitly.
        // If disposing is false, it was called by the finalizer.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                if (eglContext != null)
                    egl.EglDestroyContext(eglDisplay, eglContext);
                eglContext = null;
                if (eglSurface != null)
                    egl.EglDestroySurface(eglDisplay, eglSurface);
                eglSurface = null;
                disposed = true;
            }
        }
    }
}