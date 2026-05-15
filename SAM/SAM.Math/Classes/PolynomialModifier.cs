// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Math;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class PolynomialModifier : IndexedSimpleModifier
    {
        public PolynomialEquation PolynomialEquation { get; set; }

        public PolynomialModifier(ArithmeticOperator arithmeticOperator, PolynomialEquation polynomialEquation)
        {
            ArithmeticOperator = arithmeticOperator;
            this.PolynomialEquation = polynomialEquation == null ? null : new PolynomialEquation(polynomialEquation);
        }

        public PolynomialModifier(PolynomialModifier polynomialModifier)
            : base(polynomialModifier)
        {
            if (polynomialModifier != null)
            {
                PolynomialEquation = polynomialModifier?.PolynomialEquation == null ? null : new PolynomialEquation(polynomialModifier.PolynomialEquation);
            }
        }

        public PolynomialModifier(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public PolynomialModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            if (jsonObject["PolynomialEquation"] is JsonObject polynomialEquationJson)
            {
                PolynomialEquation = Query.IJSAMObject<PolynomialEquation>(polynomialEquationJson as JsonObject);
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (PolynomialEquation != null)
            {
                result["PolynomialEquation"] = PolynomialEquation.ToJsonObject()?.DeepClone();
            }

            return result;
        }

        public override bool ContainsIndex(int index)
        {
            return PolynomialEquation != null;
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (PolynomialEquation == null)
            {
                return value;
            }

            return Query.Calculate(ArithmeticOperator, value, PolynomialEquation.Evaluate(index));
        }
    }
}
