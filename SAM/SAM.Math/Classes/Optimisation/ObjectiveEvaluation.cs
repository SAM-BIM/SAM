// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Math
{
    /// <summary>The outcome of one evaluation: the outputs (objective first), or a failure.</summary>
    public sealed class ObjectiveEvaluation
    {
        private ObjectiveEvaluation(bool succeeded, double[] outputs, string message)
        {
            Succeeded = succeeded;
            Outputs = Array.AsReadOnly(outputs);
            Message = message;
        }

        public bool Succeeded { get; }

        /// <summary>The outputs; the first is the objective. Empty for a failure.</summary>
        public IReadOnlyList<double> Outputs { get; }

        /// <summary>Failure description, or null.</summary>
        public string Message { get; }

        public static ObjectiveEvaluation Success(IEnumerable<double> outputs)
        {
            if (outputs == null)
            {
                throw new ArgumentNullException(nameof(outputs));
            }

            return new ObjectiveEvaluation(true, outputs.ToArray(), null);
        }

        public static ObjectiveEvaluation Success(params double[] outputs)
        {
            return Success((IEnumerable<double>)outputs);
        }

        public static ObjectiveEvaluation Failure(string message)
        {
            return new ObjectiveEvaluation(false, new double[0], message);
        }
    }
}
