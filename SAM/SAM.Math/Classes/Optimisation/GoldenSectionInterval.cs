// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>The final uncertainty interval of a golden-section run (GenOpt's result overview, spec §4).</summary>
    public sealed class GoldenSectionInterval
    {
        internal GoldenSectionInterval(double lower, double upper, double minimum, double maximum)
        {
            Lower = lower;
            Upper = upper;
            MidPoint = (upper + lower) / 2;
            Length = upper - lower;
            NormalisedLength = (upper - lower) / (maximum - minimum);
        }

        public double Lower { get; }

        public double Upper { get; }

        public double MidPoint { get; }

        public double Length { get; }

        /// <summary><see cref="Length"/> divided by the length of the original bounds.</summary>
        public double NormalisedLength { get; }
    }
}
