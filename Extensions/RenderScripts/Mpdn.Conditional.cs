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
// 

using System;
using Mpdn.Extensions.Framework.Chain;
using Mpdn.Extensions.Framework.RenderChain;
using Mpdn.Extensions.Framework.Scripting;
using Mpdn.RenderScript;

namespace Mpdn.Extensions.RenderScripts
{
    namespace Mpdn.Conditional
    {
        public class Conditional : RenderChain
        {
            private const string RESULT_VAR = "__$result";

            #region Settings

            public string Condition { get; set; }
            public Preset<ITextureFilter, IRenderScript> Preset { get; set; }

            #endregion

            protected override ITextureFilter CreateFilter(ITextureFilter input)
            {
                if (string.IsNullOrWhiteSpace(Condition) || Preset == null)
                    return input;

                if (!RenderScriptEngine.Eval(input, GetScript(), GetType().Name))
                    return input;

                return input + Preset;
            }

            private string GetScript()
            {
                return string.Format("var {0} = {1}; {0}", RESULT_VAR, Parser.BuildCondition(Condition));
            }
        }

        public class ConditionalUi : RenderChainUi<Conditional, ConditionalConfigDialog>
        {
            public override string Category
            {
                get { return "Meta"; }
            }

            public override ExtensionUiDescriptor Descriptor
            {
                get
                {
                    return new ExtensionUiDescriptor
                    {
                        Name = "Conditional",
                        Description = GetDescription(),
                        Guid = new Guid("8ADCC25B-BAB0-4C88-AE37-695182048815"),
                        Copyright = "" // Optional field
                    };
                }
            }

            private string GetDescription()
            {
                var preset = Settings.Preset;
                if (!string.IsNullOrWhiteSpace(Settings.Condition) && preset != null)
                {
                    return string.Format("If {0}, use '{1}' ({2})", Settings.Condition.Trim(), preset.Name,
                        preset.Description);
                }
                return "Conditionally activates a render script";
            }
        }
    }
}
