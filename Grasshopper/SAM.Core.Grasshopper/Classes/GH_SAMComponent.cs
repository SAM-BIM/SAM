// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Grasshopper
{
    public abstract class GH_SAMComponent : GH_Component, IGH_SAMComponent
    {
        public GH_SAMComponent(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
            SetValue("SAM_SAMVersion", Core.Query.CurrentVersion());
            SetValue("SAM_ComponentVersion", LatestComponentVersion);
        }

        /// <summary>
        /// <b>The published parameter renames this component has made, oldest name and current name.</b>
        /// <para>
        /// A Grasshopper definition stores a component's parameters in its own archive and restores them
        /// from there, so a definition saved before a rename keeps carrying the OLD name. Declaring the
        /// rename here lets <c>Modify.DuplicateComponent</c> carry the old name's connections and
        /// persistent values onto the new parameter, and lets <c>SolveInstance</c> — through
        /// <see cref="IndexOfInputParameter"/> / <see cref="IndexOfOutputParameter"/> — still find the
        /// parameter on a definition that has not been updated.
        /// </para>
        /// <para>
        /// This is deliberately a declaration rather than a heuristic: a rename is only migrated when it is
        /// stated, and an ambiguous declaration is refused and reported rather than guessed.
        /// </para>
        /// </summary>
        public virtual IReadOnlyList<ParameterRename> ParameterRenames => null;

        /// <summary>
        /// <c>Params.IndexOfInputParam(name)</c> that also finds the parameter on a definition saved under
        /// a legacy name this component has declared a <see cref="ParameterRenames"/> entry for.
        /// </summary>
        public int IndexOfInputParameter(string name)
        {
            return IndexOfParameter(GH_ParameterSide.Input, name, x => Params == null ? -1 : Params.IndexOfInputParam(x));
        }

        /// <summary>
        /// <c>Params.IndexOfOutputParam(name)</c> that also finds the parameter on a definition saved under
        /// a legacy name this component has declared a <see cref="ParameterRenames"/> entry for.
        /// </summary>
        public int IndexOfOutputParameter(string name)
        {
            return IndexOfParameter(GH_ParameterSide.Output, name, x => Params == null ? -1 : Params.IndexOfOutputParam(x));
        }

        private int IndexOfParameter(GH_ParameterSide side, string name, Func<string, int> lookup)
        {
            if (string.IsNullOrEmpty(name) || lookup == null)
            {
                return -1;
            }

            int result = lookup(name);
            if (result != -1)
            {
                return result;
            }

            IReadOnlyList<ParameterRename> parameterRenames = ParameterRenames;
            if (parameterRenames == null)
            {
                return -1;
            }

            foreach (ParameterRename parameterRename in parameterRenames)
            {
                if (parameterRename == null || parameterRename.Side != side)
                {
                    continue;
                }

                if (!string.Equals(parameterRename.Name, name, StringComparison.Ordinal) || string.IsNullOrEmpty(parameterRename.LegacyName))
                {
                    continue;
                }

                result = lookup(parameterRename.LegacyName);
                if (result != -1)
                {
                    return result;
                }
            }

            return -1;
        }

        public override bool Obsolete
        {
            get
            {
                return Query.Obsolete(this);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            //Menu_AppendItem(menu, "Source code", OnSourceCodeClick, Properties.Resources.SAM_Small);

            Modify.AppendVersionMenuItem(this, menu);
            Modify.AppendSourceCodeAdditionalMenuItem(this, menu);
            Modify.AppendNewComponentAdditionalMenuItem(this, menu);
        }

        public string ComponentVersion
        {
            get
            {
                return GetValue("SAM_ComponentVersion", null);
            }
        }

        public string SAMVersion
        {
            get
            {
                return GetValue("SAM_SAMVersion", null);
            }
        }

        public abstract string LatestComponentVersion { get; }

        public virtual string MinCompatibleVersion => LatestComponentVersion;

        public ObsoleteSeverity ObsoleteSeverity
        {
            get
            {
                return Query.GetObsoleteSeverity(this);
            }
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            Message = ComponentVersion;
        }
    }
}
