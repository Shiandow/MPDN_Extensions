// This file is a part of MPDN Extensions.
// https://github.com/zachsaw/MPDN_Extensions
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

using System;
using System.Diagnostics;
using Mpdn.Extensions.Framework.Chain;
using Mpdn.Extensions.Framework.Filter;
using Mpdn.RenderScript;

namespace Mpdn.Extensions.Framework.RenderChain
{
    public class RenderChainScript : IRenderScript, IDisposable
    {
        private VideoSourceFilter m_SourceFilter;
        private IFilter<ITextureOutput<ITexture2D>> m_Filter;
        private FilterTag m_Tag;

        protected readonly Chain<ITextureFilter> Chain;

        public RenderChainScript(Chain<ITextureFilter> chain)
        {
            Chain = chain;
            Chain.Initialize();
            Status = string.Empty;
        }

        public ScriptInterfaceDescriptor Descriptor
        {
            get { return m_SourceFilter == null ? null : m_SourceFilter.Descriptor; }
        }

        public string Status { get; private set; }

        public void Update()
        {
            var oldFilter = m_Filter;
            try
            {
                DisposeHelper.Dispose(ref m_SourceFilter);

                m_Filter = CreateOutputFilter();

                UpdateStatus();
            }
            finally
            {
                DisposeHelper.Dispose(ref oldFilter);
            }
        }

        private void UpdateStatus()
        {
            Status = m_Tag != null ? m_Tag.CreateString() : "Status Invalid";
        }

        public bool Execute()
        {
            try
            {
                if (Renderer.InputRenderTarget != Renderer.OutputRenderTarget)
                    TexturePool.PutTempTexture(Renderer.OutputRenderTarget);

                m_Filter.Render();

                if (Renderer.OutputRenderTarget != m_Filter.Output.Texture)
                    Scale(Renderer.OutputRenderTarget, m_Filter.Output.Texture);

                m_Filter.Reset();
                TexturePool.FlushTextures();

                return true;
            }
            catch (Exception e)
            {
                var message = ErrorMessage(e);
                Trace.WriteLine(message);
                return false;
            }
        }

        private IResizeableFilter MakeInitialFilter()
        {
            m_SourceFilter = new VideoSourceFilter();

            if (Renderer.InputFormat.IsRgb())
                return m_SourceFilter;

            if (Renderer.ChromaSize.Width < Renderer.LumaSize.Width || Renderer.ChromaSize.Height < Renderer.LumaSize.Height)
                return new ChromaFilter(new YSourceFilter(), new ChromaSourceFilter(), chromaScaler: new InternalChromaScaler(m_SourceFilter));

            return m_SourceFilter;
        }

        private static void Scale(ITargetTexture output, ITexture2D input)
        {
            Renderer.Scale(output, input, Renderer.LumaUpscaler, Renderer.LumaDownscaler);
        }

        #region Error Handling

        public IFilter<ITextureOutput<ITexture2D>> CreateOutputFilter()
        {
            try
            {
                var input = MakeInitialFilter()
                    .MakeTagged();

                return Chain
                    .Process(input)
                    .SetSize(Renderer.TargetSize)
                    .GetTag(out m_Tag)
                    .Compile()
                    .InitializeFilter();
            }
            catch (Exception ex)
            {
                return DisplayError(ex);
            }
        }

        private TextFilter DisplayError(Exception e)
        {
            var message = ErrorMessage(e);
            Trace.WriteLine(message);
            return new TextFilter(message);
        }

        protected static Exception InnerMostException(Exception e)
        {
            while (e.InnerException != null)
            {
                e = e.InnerException;
            }

            return e;
        }

        private string ErrorMessage(Exception e)
        {
            var ex = InnerMostException(e);
            return string.Format("Error in {0}:\r\n\r\n{1}\r\n\r\n~\r\nStack Trace:\r\n{2}",
                    GetType().Name, ex.Message, ex.StackTrace);
        }

        #endregion

        #region Resource Management

        ~RenderChainScript()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Chain.Reset();

            DisposeHelper.Dispose(m_Filter);
            DisposeHelper.Dispose(ref m_SourceFilter);
        }

        #endregion
    }
}
