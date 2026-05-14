// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Category : IJSAMObject
    {
        private string? name;
        private Category? subCategory;

        public Category(string name)
        {
            this.name = name;
        }

        public Category(string? name, Category? subCategory)
        {
            this.name = name;
            this.subCategory = subCategory;
        }

        public Category(Category? category)
        {
            if (category != null)
            {
                name = category.name;
                subCategory = category.subCategory == null ? null : new Category(category.subCategory);
            }
        }

        public Category(JObject? jObject)
        {
            FromJObject(jObject);
        }

        public string? Name
        {
            get
            {
                return name;
            }
        }

        public Category? SubCategory
        {
            get
            {
                return subCategory == null ? null : new Category(subCategory);
            }
        }

        public override string? ToString()
        {
            return name;
        }

        public string ToString(string separator)
        {
            if (separator == null)
            {
                separator = string.Empty;
            }

            List<string> values = [];

            List<Category> categories = this.SubCategories();
            if (categories != null)
            {
                foreach (Category category in categories)
                {
                    string? name_Category = category?.Name;
                    if (name_Category == null)
                    {
                        name_Category = string.Empty;

                    }

                    values.Add(name_Category);
                }
            }

            values.Add(name == null ? string.Empty : name);

            values.Reverse();

            return string.Join(separator, values);
        }

        public bool FromJObject(JObject? jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        private bool FromJsonObject(JsonObject? jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Name"))
            {
                name = jsonObject["Name"]?.GetValue<string>();
            }

            if (jsonObject["SubCategory"] is JsonObject subCategoryObject)
            {
                subCategory = new Category(new JObject((JsonObject)subCategoryObject.DeepClone()));
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (name != null)
            {
                jsonObject["Name"] = name;
            }

            if (subCategory != null)
            {
                if (subCategory.ToJObject()?.Node is JsonObject subCategoryObject)
                    jsonObject["SubCategory"] = subCategoryObject.DeepClone();
            }

            return jsonObject;
        }
    }
}
