// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Math
{

    /// <summary>
    /// Equation in format y = Ax + B
    /// </summary>
    public class LinearEquation : IEquation
    {
        private double a; // coefficient A in the equation
        private double b; // coefficient B in the equation

        public LinearEquation(JObject jObject)
        {
            FromJObject(jObject);
        }

        public LinearEquation(double a, double b)
        {
            this.a = a;
            this.b = b;
        }

        public LinearEquation(LinearEquation linearEquation)
        {
            if (linearEquation != null)
            {
                a = linearEquation.a;
                b = linearEquation.b;
            }
        }

        /// <summary>
        /// Evaluates the equation for a given value of x
        /// </summary>
        /// <param name="value">The value of x</param>
        /// <returns>The value of y for the given value of x</returns>
        public double Evaluate(double value)
        {
            if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(value))
            {
                return double.NaN;
            }

            return b + a * value;
        }

        /// <summary>
        /// Evaluates the equation for a collection of values of x
        /// </summary>
        /// <param name="values">The collection of values of x</param>
        /// <returns>A list of corresponding values of y</returns>
        public List<double> Evaluate(IEnumerable<double> values)
        {
            if (values == null)
                return null;

            List<double> result = new List<double>();
            foreach (double value in values)
            {
                result.Add(Evaluate(value));
            }

            return result;
        }

        /// <summary>
        /// Initializes the equation from a JSON object
        /// </summary>
        /// <param name="jObject">The JSON object to initialize from</param>
        /// <returns>True if initialization was successful, otherwise false</returns>
        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject.ContainsKey("A"))
            {
                a = jsonObject["A"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("B"))
            {
                b = jsonObject["B"]?.GetValue<double>() ?? double.NaN;
            }

            return true;
        }

        /// <summary>
        /// Returns a JSON representation of the equation
        /// </summary>
        /// <returns>A JSON object representing the equation</returns>
        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (!double.IsNaN(a))
            {
                jsonObject["A"] = a;
            }

            if (!double.IsNaN(b))
            {
                jsonObject["B"] = b;
            }

            return jsonObject;
        }

        /// <summary>
        /// Gets the coefficients of the equation
        /// </summary>
        public List<double> Coefficients
        {
            get
            {
                return new List<double>() { b, a };
            }
        }
    }

}
