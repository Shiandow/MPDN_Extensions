﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mpdn.Extensions.Framework.Config;
using Mpdn.RenderScript;
using SharpDX;
using Point = System.Drawing.Point;

namespace Mpdn.Extensions.Framework
{
    namespace ScriptEngineUtilities
    {
        public static class Debug
        {
            public static void Output(object text)
            {
                Trace.WriteLine(text.ToString());
            }

            public static void Assert(bool condition)
            {
                if (condition)
                    return;

                throw new Exception("Assertion failed in script");
            }
        }

        public class InternalHostFunctions
        {
            public void AssignProp(object obj, string propName, dynamic value)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }

                var propInfo = obj.GetType().GetProperty(propName);
                if (propInfo == null)
                {
                    throw new ArgumentException(string.Format("Invalid property name '{0}'", propName), "propName");
                }

                var propType = propInfo.PropertyType;
                if (propType.IsArray)
                {
                    var length = value.length;
                    var arr = Array.CreateInstance(propType.GetElementType(), length);
                    for (int i = 0; i < length; i++)
                    {
                        arr[i] = value[i];
                    }
                    propInfo.SetValue(obj, arr, null);
                }
                else
                {
                    propInfo.SetValue(obj, value, null);
                }
            }
        }

        public class Host
        {
            public string ExePath
            {
                get { return PathHelper.GetDirectoryName(Application.ExecutablePath); }
            }

            public string ExeName
            {
                get { return Path.GetFileName(Application.ExecutablePath); }
            }

            public string StartupPath
            {
                get { return Application.StartupPath; }
            }

            public string ConfigFile
            {
                get { return PlayerControl.ConfigRootPath; }
            }

            public string ConfigFilePath
            {
                get { return PathHelper.GetDirectoryName(PlayerControl.ConfigRootPath); }
            }

            public string Version
            {
                get { return Application.ProductVersion; }
            }

            public string Name
            {
                get { return Application.ProductName; }
            }
        }

        public class Script
        {
            private static IEnumerable<IRenderChainUi> s_RenderScripts;

            public dynamic Load(string name)
            {
                var chainUi = GetRenderScripts().FirstOrDefault(script => script.Descriptor.Name == name);
                if (chainUi == null)
                {
                    throw new ArgumentException(string.Format("script.Load() error: Script '{0}' not found", name));
                }

                return chainUi.Chain;
            }

            public dynamic LoadByClassName(string className)
            {
                var chainUi = GetRenderScripts().FirstOrDefault(script => script.Chain.GetType().Name == className);
                if (chainUi == null)
                {
                    throw new ArgumentException(
                        string.Format("script.Load() error: Script with class name '{0}' not found", className));
                }

                return chainUi.Chain;
            }

            private static IEnumerable<IRenderChainUi> GetRenderScripts()
            {
                return s_RenderScripts ??
                       (s_RenderScripts = PlayerControl.RenderScripts
                           .Where(script => script is IRenderChainUi)
                           .Select(x => (x as IRenderChainUi))).ToArray();
            }
        }

        public class MockClip : IClip
        {
            public string FileName
            {
                get { return "C:\\MyVideoFolder\\AnotherSubFolder\\MyVideoFile.mkv"; }
            }

            public bool Interlaced
            {
                get { return true; }
            }

            public bool NeedsUpscaling
            {
                get { return true; }
            }

            public bool NeedsDownscaling
            {
                get { return true; }
            }

            public Size TargetSize
            {
                get { return new Size(1920, 1080); }
            }

            public Size SourceSize
            {
                get { return new Size(320, 180); }
            }

            public Size LumaSize
            {
                get { return new Size(320, 180); }
            }

            public Size ChromaSize
            {
                get { return new Size(160, 90); }
            }

            public Vector2 ChromaOffset
            {
                get { return Vector2.Zero; }
            }

            public Point AspectRatio
            {
                get { return new Point(16, 9); }
            }

            public YuvColorimetric Colorimetric
            {
                get { return YuvColorimetric.ItuBt601; }
            }

            public FrameBufferInputFormat InputFormat
            {
                get { return FrameBufferInputFormat.Nv12; }
            }

            public double FrameRateHz
            {
                get { return 24/1.001; }
            }
        }

        public class Clip : IClip
        {
            private readonly RenderChain m_Chain;
            public IFilter Filter { get; private set; }

            public string FileName
            {
                get { return Renderer.VideoFileName; }
            }

            public bool Interlaced
            {
                get { return Renderer.InterlaceFlags.HasFlag(InterlaceFlags.IsInterlaced); }
            }

            public bool NeedsUpscaling
            {
                get { return m_Chain.IsUpscalingFrom(Filter); }
            }

            public bool NeedsDownscaling
            {
                get { return m_Chain.IsDownscalingFrom(Filter); }
            }

            public Size TargetSize
            {
                get { return Renderer.TargetSize; }
            }

            public Size SourceSize
            {
                get { return Renderer.VideoSize; }
            }

            public Size LumaSize
            {
                get { return Renderer.LumaSize; }
            }

            public Size ChromaSize
            {
                get { return Renderer.ChromaSize; }
            }

            public Vector2 ChromaOffset
            {
                get { return Renderer.ChromaOffset; }
            }

            public Point AspectRatio
            {
                get { return Renderer.AspectRatio; }
            }

            public YuvColorimetric Colorimetric
            {
                get { return Renderer.Colorimetric; }
            }

            public FrameBufferInputFormat InputFormat
            {
                get { return Renderer.InputFormat; }
            }

            public double FrameRateHz
            {
                get { return Renderer.FrameRateHz; }
            }

            public Clip(RenderChain chain, IFilter input)
            {
                m_Chain = chain;
                Filter = input;
            }

            public Clip Add()
            {
                Filter += m_Chain;
                return this;
            }

            public Clip Add(RenderChain filter)
            {
                if (filter == null)
                {
                    throw new ArgumentNullException("filter");
                }
                Filter += filter;
                return this;
            }

            public Clip Apply(RenderChain filter)
            {
                return Add(filter);
            }
        }
    }
}