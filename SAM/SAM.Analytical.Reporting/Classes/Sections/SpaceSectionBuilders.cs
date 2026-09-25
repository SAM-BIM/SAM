// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Units;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Reporting
{
    /// <summary>
    /// Section 1: space identity.
    /// </summary>
    public sealed class SpaceIdentitySectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "identity";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceIdentityData spaceIdentityData = data.Identity;

            KeyValueBlock keyValueBlock = new KeyValueBlock("identity", null, new[]
            {
                SectionFormat.Row("Space", quantityFormatter.Format(spaceIdentityData.Name)),
                SectionFormat.Row("Level", quantityFormatter.Format(spaceIdentityData.LevelName)),
                SectionFormat.Row("Internal condition", quantityFormatter.Format(spaceIdentityData.InternalConditionName)),
            });

            return new DocumentSection(Id, "Space", new DocumentBlock[] { keyValueBlock });
        }
    }

    /// <summary>
    /// Section 2: floor area, volume and average height.
    /// </summary>
    public sealed class SpaceGeometrySectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "geometry";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceGeometryData spaceGeometryData = data.Geometry;

            if (SectionFormat.AllMissing(spaceGeometryData.Area, spaceGeometryData.Volume))
            {
                return new DocumentSection(Id, "Geometry", new DocumentBlock[] { new NoticeBlock("geometry-missing", "No floor area or volume in model", NoticeLevel.Warning) }, SectionWidth.Half);
            }

            KeyValueBlock keyValueBlock = new KeyValueBlock("geometry", null, new[]
            {
                SectionFormat.Row("Floor area", quantityFormatter.Format(spaceGeometryData.Area)),
                SectionFormat.Row("Volume", quantityFormatter.Format(spaceGeometryData.Volume)),
                SectionFormat.Row("Average height", quantityFormatter.Format(spaceGeometryData.AverageHeight)),
            });

            return new DocumentSection(Id, "Geometry", new DocumentBlock[] { keyValueBlock }, SectionWidth.Half);
        }
    }

    /// <summary>
    /// Section 3: internal condition - occupancy, the gains table (one shared unit for the total column), lighting
    /// level and infiltration.
    /// </summary>
    public sealed class SpaceInternalConditionSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "internal-condition";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            const string title = "Internal Condition";

            if (!data.Identity.InternalConditionName.HasValue && SectionFormat.AllMissing(data.Occupancy.People, data.Lighting.GainPerArea, data.EquipmentSensible.GainPerArea))
            {
                return new DocumentSection(Id, title, new DocumentBlock[] { new NoticeBlock("internal-condition-missing", data.Identity.InternalConditionName.Note, NoticeLevel.Warning) });
            }

            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceOccupancyData spaceOccupancyData = data.Occupancy;

            KeyValueBlock keyValueBlock_Occupancy = new KeyValueBlock("occupancy", "Occupancy", new[]
            {
                SectionFormat.Row("People", quantityFormatter.Format(spaceOccupancyData.People)),
                SectionFormat.Row("Area per person", quantityFormatter.Format(spaceOccupancyData.AreaPerPerson)),
                SectionFormat.Row("Profile", quantityFormatter.Format(spaceOccupancyData.Profile)),
                SectionFormat.Row("Occupied hours per year", quantityFormatter.Format(spaceOccupancyData.OccupiedHoursPerYear)),
            });

            // Totals are comparable powers: one display unit for the whole column. The specific column mixes W/person
            // and W/m², so each cell keeps its own unit; each category still shares one unit.
            List<(string Label, ReportValue<string> Profile, ReportValue<Quantity> Specific, ReportValue<Quantity> Total)> gains = new List<(string, ReportValue<string>, ReportValue<Quantity>, ReportValue<Quantity>)>()
            {
                ("Occupancy sensible", spaceOccupancyData.Profile, spaceOccupancyData.SensibleGainPerPerson, spaceOccupancyData.SensibleGain),
                ("Occupancy latent", spaceOccupancyData.Profile, spaceOccupancyData.LatentGainPerPerson, spaceOccupancyData.LatentGain),
                ("Lighting", data.Lighting.Profile, data.Lighting.GainPerArea, data.Lighting.Gain),
                ("Equipment sensible", data.EquipmentSensible.Profile, data.EquipmentSensible.GainPerArea, data.EquipmentSensible.Gain),
                ("Equipment latent", data.EquipmentLatent.Profile, data.EquipmentLatent.GainPerArea, data.EquipmentLatent.Gain),
            };

            DisplayUnit displayUnit_Total = SectionFormat.Unit(quantityFormatter, UnitCategory.Power, gains.Select(x => x.Total));
            DisplayUnit displayUnit_PerPerson = SectionFormat.Unit(quantityFormatter, UnitCategory.PowerPerPerson, gains.Select(x => x.Specific));
            DisplayUnit displayUnit_PerArea = SectionFormat.Unit(quantityFormatter, UnitCategory.SpecificPower, gains.Select(x => x.Specific));

            List<TableRow> tableRows = new List<TableRow>();
            for (int i = 0; i < gains.Count; i++)
            {
                DisplayUnit displayUnit_Specific = i < 2 ? displayUnit_PerPerson : displayUnit_PerArea;

                tableRows.Add(new TableRow(
                    SectionFormat.Label(gains[i].Label),
                    quantityFormatter.Format(gains[i].Profile),
                    quantityFormatter.Format(gains[i].Specific, displayUnit_Specific),
                    quantityFormatter.Format(gains[i].Total, displayUnit_Total)));
            }

            TableBlock tableBlock_Gains = new TableBlock("gains", "Internal gains", new[]
            {
                new TableColumn("Gain", alignment: ColumnAlignment.Left),
                new TableColumn("Profile", alignment: ColumnAlignment.Left),
                new TableColumn("Specific"),
                new TableColumn("Total", displayUnit_Total.Symbol),
            }, tableRows);

            KeyValueBlock keyValueBlock_Lighting = new KeyValueBlock("lighting", "Lighting", new[]
            {
                SectionFormat.Row("Design illuminance", quantityFormatter.Format(data.Lighting.Illuminance)),
            });

            SpaceInfiltrationData spaceInfiltrationData = data.Infiltration;
            KeyValueBlock keyValueBlock_Infiltration = new KeyValueBlock("infiltration", "Infiltration", new[]
            {
                SectionFormat.Row("Air change rate", quantityFormatter.Format(spaceInfiltrationData.AirChangeRate)),
                SectionFormat.Row("Air flow", quantityFormatter.Format(spaceInfiltrationData.AirFlow)),
                SectionFormat.Row("Profile", quantityFormatter.Format(spaceInfiltrationData.Profile)),
            });

            return new DocumentSection(Id, title, new DocumentBlock[] { keyValueBlock_Occupancy, tableBlock_Gains, keyValueBlock_Lighting, keyValueBlock_Infiltration });
        }
    }

    /// <summary>
    /// Section 4: room set points and outdoor design conditions, heating vs cooling.
    /// </summary>
    public sealed class SpaceDesignCriteriaSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "design-criteria";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceDesignCriteriaData spaceDesignCriteriaData = data.DesignCriteria;

            if (SectionFormat.AllMissing(spaceDesignCriteriaData.HeatingSetPoint, spaceDesignCriteriaData.CoolingSetPoint, spaceDesignCriteriaData.HumidificationSetPoint, spaceDesignCriteriaData.DehumidificationSetPoint, spaceDesignCriteriaData.OutdoorHeatingDryBulb, spaceDesignCriteriaData.OutdoorCoolingDryBulb))
            {
                return new DocumentSection(Id, "Design Criteria", new DocumentBlock[] { new NoticeBlock("design-criteria-missing", "No set point profiles and no design days in model", NoticeLevel.Warning) }, SectionWidth.Half);
            }

            List<TableRow> tableRows = new List<TableRow>()
            {
                Pair(quantityFormatter, "Room set point", UnitCategory.Temperature, spaceDesignCriteriaData.HeatingSetPoint, spaceDesignCriteriaData.CoolingSetPoint),
                Pair(quantityFormatter, "Outdoor dry bulb", UnitCategory.Temperature, spaceDesignCriteriaData.OutdoorHeatingDryBulb, spaceDesignCriteriaData.OutdoorCoolingDryBulb),
                Pair(quantityFormatter, "Outdoor RH (coincident)", UnitCategory.Ratio, spaceDesignCriteriaData.OutdoorHeatingRelativeHumidity, spaceDesignCriteriaData.OutdoorCoolingRelativeHumidity),
            };

            TableBlock tableBlock = new TableBlock("design-criteria", null, new[]
            {
                new TableColumn(string.Empty, alignment: ColumnAlignment.Left),
                new TableColumn("Heating"),
                new TableColumn("Cooling"),
            }, tableRows);

            // Humidity control is not tied to heating or cooling: Tas holds a lower RH limit (humidification) and an
            // upper RH limit (dehumidification), so they are listed by what they are, outside the heating/cooling table.
            FormattedValue[] humidity = SectionFormat.Group(quantityFormatter, UnitCategory.Ratio, spaceDesignCriteriaData.HumidificationSetPoint, spaceDesignCriteriaData.DehumidificationSetPoint);
            KeyValueBlock keyValueBlock = new KeyValueBlock("room-humidity", "Room humidity", new[]
            {
                SectionFormat.Row("Humidification set point (lower RH limit)", humidity[0]),
                SectionFormat.Row("Dehumidification set point (upper RH limit)", humidity[1]),
            });

            return new DocumentSection(Id, "Design Criteria", new DocumentBlock[] { tableBlock, keyValueBlock }, SectionWidth.Half);
        }

        internal static TableRow Pair(IQuantityFormatter quantityFormatter, string label, UnitCategory unitCategory, ReportValue<Quantity> heating, ReportValue<Quantity> cooling)
        {
            FormattedValue[] formattedValues = SectionFormat.Group(quantityFormatter, unitCategory, heating, cooling);
            return new TableRow(SectionFormat.Label(label), formattedValues[0], formattedValues[1]);
        }
    }

    /// <summary>
    /// Section 5: ventilation air flows (one shared unit) and the ventilation system.
    /// </summary>
    public sealed class SpaceVentilationSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "ventilation";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceVentilationData spaceVentilationData = data.Ventilation;

            if (SectionFormat.AllMissing(spaceVentilationData.SupplyAirFlow, spaceVentilationData.ExtractAirFlow, spaceVentilationData.OutsideAirFlow, spaceVentilationData.SystemType))
            {
                return new DocumentSection(Id, "Ventilation", new DocumentBlock[] { new NoticeBlock("ventilation-missing", "No ventilation air flows and no ventilation system assigned") }, SectionWidth.Half);
            }

            FormattedValue[] airFlows = SectionFormat.Group(quantityFormatter, UnitCategory.AirFlow, spaceVentilationData.SupplyAirFlow, spaceVentilationData.ExtractAirFlow, spaceVentilationData.OutsideAirFlow);

            KeyValueBlock keyValueBlock = new KeyValueBlock("ventilation", null, new[]
            {
                SectionFormat.Row("Supply air", airFlows[0]),
                SectionFormat.Row("Extract air", airFlows[1]),
                SectionFormat.Row("Outside air", airFlows[2]),
                SectionFormat.Row("Supply air changes", quantityFormatter.Format(spaceVentilationData.SupplyAirChangeRate)),
                SectionFormat.Row("System", quantityFormatter.Format(spaceVentilationData.SystemType)),
                SectionFormat.Row("Supply unit", quantityFormatter.Format(spaceVentilationData.SupplyUnit)),
                SectionFormat.Row("Extract unit", quantityFormatter.Format(spaceVentilationData.ExtractUnit)),
            });

            return new DocumentSection(Id, "Ventilation", new DocumentBlock[] { keyValueBlock }, SectionWidth.Half);
        }
    }

    /// <summary>
    /// Section 6: heating / cooling systems, and risers when any is assigned.
    /// </summary>
    public sealed class SpaceSystemsSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "systems";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceSystemsData spaceSystemsData = data.Systems;

            if (SectionFormat.AllMissing(spaceSystemsData.HeatingSystem, spaceSystemsData.CoolingSystem, spaceSystemsData.VentilationRiser, spaceSystemsData.HeatingRiser, spaceSystemsData.CoolingRiser))
            {
                return new DocumentSection(Id, "Systems", new DocumentBlock[] { new NoticeBlock("systems-missing", "No heating or cooling system assigned") }, SectionWidth.Half);
            }

            List<KeyValueRow> keyValueRows = new List<KeyValueRow>()
            {
                SectionFormat.Row("Heating system", quantityFormatter.Format(spaceSystemsData.HeatingSystem)),
                SectionFormat.Row("Cooling system", quantityFormatter.Format(spaceSystemsData.CoolingSystem)),
            };

            // Risers are shown only when present.
            foreach ((string label, ReportValue<string> riser) in new[] { ("Ventilation riser", spaceSystemsData.VentilationRiser), ("Heating riser", spaceSystemsData.HeatingRiser), ("Cooling riser", spaceSystemsData.CoolingRiser) })
            {
                if (riser.HasValue)
                {
                    keyValueRows.Add(SectionFormat.Row(label, quantityFormatter.Format(riser)));
                }
            }

            return new DocumentSection(Id, "Systems", new DocumentBlock[] { new KeyValueBlock("systems", null, keyValueRows) }, SectionWidth.Half);
        }
    }

    /// <summary>
    /// Section 7: fabric areas by exposure, all in one shared area unit.
    /// </summary>
    public sealed class SpaceFabricSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public string Id => "fabric";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            const string title = "Fabric / Exposure";

            IReadOnlyList<FabricAreaRow> fabricAreaRows = data.Fabric.Rows;
            if (fabricAreaRows == null || fabricAreaRows.Count == 0)
            {
                return new DocumentSection(Id, title, new DocumentBlock[] { new NoticeBlock("fabric-missing", "No panels bound this space", NoticeLevel.Warning) });
            }

            IQuantityFormatter quantityFormatter = documentContext.Formatter;

            DisplayUnit displayUnit = SectionFormat.Unit(quantityFormatter, UnitCategory.Area, fabricAreaRows.SelectMany(x => new[] { x.ExternalArea, x.InternalArea, x.ExternalFrameArea, x.InternalFrameArea }));

            List<TableRow> tableRows = new List<TableRow>();
            foreach (FabricAreaRow fabricAreaRow in fabricAreaRows)
            {
                bool opening = fabricAreaRow.Category == FabricCategory.Windows || fabricAreaRow.Category == FabricCategory.Doors;
                string label = Label(fabricAreaRow.Category);

                tableRows.Add(new TableRow(
                    SectionFormat.Label(opening ? label + " (pane)" : label),
                    quantityFormatter.Format(fabricAreaRow.ExternalArea, displayUnit),
                    quantityFormatter.Format(fabricAreaRow.InternalArea, displayUnit)));

                if (opening)
                {
                    tableRows.Add(new TableRow(
                        SectionFormat.Label(label + " (frame)"),
                        quantityFormatter.Format(fabricAreaRow.ExternalFrameArea, displayUnit),
                        quantityFormatter.Format(fabricAreaRow.InternalFrameArea, displayUnit)));
                }
            }

            TableBlock tableBlock = new TableBlock("fabric", null, new[]
            {
                new TableColumn("Element", alignment: ColumnAlignment.Left),
                new TableColumn("External", displayUnit.Symbol),
                new TableColumn("Internal", displayUnit.Symbol),
            }, tableRows, true);

            return new DocumentSection(Id, title, new DocumentBlock[] { tableBlock });
        }

        private static string Label(FabricCategory fabricCategory)
        {
            switch (fabricCategory)
            {
                case FabricCategory.Walls:
                    return "Walls";

                case FabricCategory.Windows:
                    return "Windows";

                case FabricCategory.Doors:
                    return "Doors";

                case FabricCategory.RoofsAndCeilings:
                    return "Roofs / ceilings";

                case FabricCategory.GroundFloors:
                    return "Ground floors";

                case FabricCategory.OtherFloors:
                    return "Other floors";
            }

            return fabricCategory.ToString();
        }
    }

    /// <summary>
    /// Section 8: persisted Tas design loads (heating/cooling pairs share one unit) and sizing factors.
    /// </summary>
    public sealed class SpaceSizingSectionBuilder : ISectionBuilder<SpaceDocumentData>
    {
        public const string DesignLoadsUnknownNotice = "Design loads: from Tas sizing — date/currency not recorded";
        public const string DesignLoadsNoneNotice = "No Tas design loads in model";
        public const string SizingMultiplierNotice = "Whether the design loads above already include the sizing multiplier is not recorded";

        /// <summary>
        /// A dimensionless multiplier shown as a plain number with two decimals ("1.20").
        /// </summary>
        private static readonly DisplayUnit Multiplier = new DisplayUnit(UnitType.Unitless, null, 2);

        public string Id => "sizing";

        public DocumentSection Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            IQuantityFormatter quantityFormatter = documentContext.Formatter;
            SpaceSizingData spaceSizingData = data.Sizing;

            if (SectionFormat.AllMissing(spaceSizingData.DesignHeatingLoad, spaceSizingData.DesignCoolingLoad, spaceSizingData.HeatingSizingFactor, spaceSizingData.CoolingSizingFactor))
            {
                return new DocumentSection(Id, "Sizing (Tas design loads)", new DocumentBlock[] { new NoticeBlock("sizing-status", DesignLoadsNoneNotice) });
            }

            List<TableRow> tableRows = new List<TableRow>()
            {
                SpaceDesignCriteriaSectionBuilder.Pair(quantityFormatter, "Design load", UnitCategory.Power, spaceSizingData.DesignHeatingLoad, spaceSizingData.DesignCoolingLoad),
                SpaceDesignCriteriaSectionBuilder.Pair(quantityFormatter, "Design load per area", UnitCategory.SpecificPower, spaceSizingData.DesignHeatingLoadPerArea, spaceSizingData.DesignCoolingLoadPerArea),
                // The stored factor multiplies the Tas design load (1.2 = ×1.20), so it is shown as a plain multiplier.
                new TableRow(SectionFormat.Label("Sizing multiplier"), quantityFormatter.Format(spaceSizingData.HeatingSizingFactor, Multiplier), quantityFormatter.Format(spaceSizingData.CoolingSizingFactor, Multiplier)),
            };

            TableBlock tableBlock = new TableBlock("sizing", null, new[]
            {
                new TableColumn(string.Empty, alignment: ColumnAlignment.Left),
                new TableColumn("Heating"),
                new TableColumn("Cooling"),
            }, tableRows);

            NoticeBlock noticeBlock = spaceSizingData.DesignLoadStatus == DesignLoadStatus.None
                ? new NoticeBlock("sizing-status", DesignLoadsNoneNotice)
                : new NoticeBlock("sizing-status", DesignLoadsUnknownNotice);

            List<DocumentBlock> documentBlocks = new List<DocumentBlock>() { tableBlock, noticeBlock };
            if (spaceSizingData.DesignLoadStatus != DesignLoadStatus.None && (spaceSizingData.HeatingSizingFactor.HasValue || spaceSizingData.CoolingSizingFactor.HasValue))
            {
                // SAM_Tas multiplies the TBD design load by the factor only on some sizing paths, and the persisted
                // load does not record whether that happened.
                documentBlocks.Add(new NoticeBlock("sizing-multiplier", SizingMultiplierNotice));
            }

            return new DocumentSection(Id, "Sizing (Tas design loads)", documentBlocks);
        }
    }

    /// <summary>
    /// Footer lines: model, SAM version, design-load status, generation time and the space GUID.
    /// </summary>
    public sealed class SpaceFooterBuilder : IFooterBuilder<SpaceDocumentData>
    {
        public IEnumerable<string> Build(SpaceDocumentData data, DocumentContext documentContext)
        {
            DocumentProvenance documentProvenance = data.Provenance ?? documentContext.Provenance;

            string designLoads = data.Sizing.DesignLoadStatus == DesignLoadStatus.None ? "Design loads: none in model" : SpaceSizingSectionBuilder.DesignLoadsUnknownNotice;

            yield return string.Format("Model: {0} · SAM {1} · {2}", documentProvenance.ModelName ?? "—", documentProvenance.SoftwareVersion ?? "—", designLoads);
            yield return string.Format("Generated {0} · Space {1}", QuantityFormatter.FormatDateTime(documentProvenance.GeneratedAt), data.Identity.Guid.ToString("D"));
        }
    }
}
