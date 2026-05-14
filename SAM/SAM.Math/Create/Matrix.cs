// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

namespace SAM.Math
{
    public static partial class Create
    {
        public static Matrix Matrix(Matrix matrix)
        {
            if (matrix == null)
                return null;

            if (matrix.IsSquare())
            {
                int count = matrix.RowCount();

                if (count == 2)
                    return Matrix2D(matrix);

                if (count == 3)
                    return Matrix3D(matrix);

                if (count == 4)
                    return Matrix4D(matrix);
            }

            return new Matrix(matrix);
        }

        public static Matrix Matrix(JObject jObject)
        {
            if (jObject == null)
                return null;

            return Core.Query.IJSAMObject(jObject) as Matrix;
        }
    }
}
