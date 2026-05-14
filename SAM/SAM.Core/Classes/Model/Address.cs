// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Address : SAMObject
    {
        private string street;
        private string city;
        private string postalCode;
        private CountryCode countryCode;

        public Address(Address address)
            : base(address)
        {
            street = address.street;
            city = address.city;
            postalCode = address.postalCode;
            countryCode = address.countryCode;
        }

        public Address(string street, string city, string postalCode, CountryCode countryCode)
            : base()
        {
            this.street = street;
            this.city = city;
            this.postalCode = postalCode;
            this.countryCode = countryCode;
        }

        public Address(Guid guid, string name, string street, string city, string postalCode, CountryCode countryCode)
            : base(guid, name)
        {
            this.street = street;
            this.city = city;
            this.postalCode = postalCode;
            this.countryCode = countryCode;
        }

        public Address(JObject jObject)
            : base(jObject)
        {
        }

        public string Street
        {
            get
            {
                return street;
            }
        }

        public string City
        {
            get
            {
                return city;
            }
        }

        public string PostalCode
        {
            get
            {
                return postalCode;
            }
        }

        public CountryCode CountryCode
        {
            get
            {
                return countryCode;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("Street"))
                street = jsonObject["Street"]?.GetValue<string>();

            if (jsonObject.ContainsKey("City"))
                city = jsonObject["City"]?.GetValue<string>();

            if (jsonObject.ContainsKey("PostalCode"))
                postalCode = jsonObject["PostalCode"]?.GetValue<string>();

            if (jsonObject.ContainsKey("CountryCode"))
                Enum.TryParse(jsonObject["CountryCode"]?.GetValue<string>(), out countryCode);

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (street != null)
                jsonObject["Street"] = street;

            if (city != null)
                jsonObject["City"] = city;

            if (postalCode != null)
                jsonObject["PostalCode"] = postalCode;

            if (countryCode != CountryCode.Undefined)
                jsonObject["CountryCode"] = countryCode.ToString();

            return jsonObject;
        }
    }
}
