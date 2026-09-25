// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Reporting;
using System;

namespace SAM.Analytical.Reporting
{
    /// <summary>
    /// Everything one document run needs: the model snapshot, options, formatter, provenance and a diagnostics log
    /// that collectors add expected-missing-data warnings to. Build it with <see cref="Create.DocumentContext"/>.
    /// </summary>
    public sealed class DocumentContext
    {
        public DocumentContext(AnalyticalModel analyticalModel, DocumentOptions documentOptions, IQuantityFormatter quantityFormatter, DocumentProvenance documentProvenance, Log diagnostics)
        {
            AnalyticalModel = analyticalModel ?? throw new ArgumentNullException(nameof(analyticalModel));
            Options = documentOptions ?? throw new ArgumentNullException(nameof(documentOptions));
            Formatter = quantityFormatter ?? throw new ArgumentNullException(nameof(quantityFormatter));
            Provenance = documentProvenance ?? throw new ArgumentNullException(nameof(documentProvenance));
            Diagnostics = diagnostics ?? new Log();
            AdjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary = analyticalModel.ProfileLibrary;
        }

        public AnalyticalModel AnalyticalModel { get; }

        /// <summary>
        /// The model's adjacency cluster, read once. May be null for an empty model.
        /// </summary>
        public AdjacencyCluster AdjacencyCluster { get; }

        public ProfileLibrary ProfileLibrary { get; }

        public DocumentOptions Options { get; }

        public IQuantityFormatter Formatter { get; }

        public DocumentProvenance Provenance { get; }

        /// <summary>
        /// Warnings about expected missing or invalid engineering data. Software failures are not logged here: they
        /// throw and abort the document.
        /// </summary>
        public Log Diagnostics { get; }
    }
}
