// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    public static partial class Query
    {
        public static string AboutInfoTypeText(this AboutInfoType aboutInfoType)
        {
            switch (aboutInfoType)
            {
                case (AboutInfoType.SAMBIM):
                    return "SAM-BIM\n https://sambim.xyz\n https://github.com/SAM-BIM\n SAM-BIM is the independent open-source organisation that develops and maintains SAM, the Sustainable Analytical Model.";

                case (AboutInfoType.SAM):
                    return "SAM - Sustainable Analytical Model\n Originated by Michal Dengusiak and Jakub Ziolkowski (original development at Hoare Lea); now developed by SAM-BIM.\n https://sambim.xyz | https://github.com/SAM-BIM";

                case (AboutInfoType.Other):
                    return System.DateTime.Now.ToString("dd-MM-yyyy"); ;
            }

            return null;
        }
    }
}
