using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Core.Schema;

/// <summary>
/// Registry for global OptionSets that are defined outside individual entity attributes.
/// Use this to provide OptionSet values for attributes that reference global OptionSets
/// which are not embedded in the customizations.xml attribute definition.
/// </summary>
public static class GlobalOptionSetRegistry
{
    /// <summary>
    /// Dictionary of global OptionSets keyed by OptionSet name (e.g., "in_countryos").
    /// </summary>
    private static readonly Dictionary<string, GlobalOptionSetDefinition> OptionSetsByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Dictionary mapping attribute logical names to their global OptionSet name.
    /// Use this when an attribute's OptionSetName property is not populated.
    /// </summary>
    private static readonly Dictionary<string, string> AttributeToOptionSetMapping = new(StringComparer.OrdinalIgnoreCase);

    static GlobalOptionSetRegistry()
    {
        // Register all known global OptionSets
        RegisterBuiltInOptionSets();
    }

    /// <summary>
    /// Register a global OptionSet definition.
    /// </summary>
    public static void Register(GlobalOptionSetDefinition definition)
    {
        OptionSetsByName[definition.Name] = definition;

        // Also register attribute mappings
        foreach (var attributeName in definition.AttributeLogicalNames)
        {
            AttributeToOptionSetMapping[attributeName] = definition.Name;
        }
    }

    /// <summary>
    /// Try to get OptionSet values for an attribute.
    /// First checks by OptionSetName, then by attribute logical name.
    /// </summary>
    public static List<OptionSetValue>? GetOptionSetValues(string? optionSetName, string attributeLogicalName)
    {
        // First, try to find by OptionSet name
        if (!string.IsNullOrEmpty(optionSetName) && OptionSetsByName.TryGetValue(optionSetName, out var definition))
        {
            return definition.Values;
        }

        // Second, try to find by attribute logical name mapping
        if (AttributeToOptionSetMapping.TryGetValue(attributeLogicalName, out var mappedOptionSetName))
        {
            if (OptionSetsByName.TryGetValue(mappedOptionSetName, out definition))
            {
                return definition.Values;
            }
        }

        return null;
    }

    /// <summary>
    /// Try to get the description for an OptionSet.
    /// </summary>
    public static string? GetOptionSetDescription(string? optionSetName, string attributeLogicalName)
    {
        if (!string.IsNullOrEmpty(optionSetName) && OptionSetsByName.TryGetValue(optionSetName, out var definition))
        {
            return definition.Description;
        }

        if (AttributeToOptionSetMapping.TryGetValue(attributeLogicalName, out var mappedOptionSetName))
        {
            if (OptionSetsByName.TryGetValue(mappedOptionSetName, out definition))
            {
                return definition.Description;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if an OptionSet is registered.
    /// </summary>
    public static bool IsRegistered(string optionSetName)
    {
        return OptionSetsByName.ContainsKey(optionSetName);
    }

    /// <summary>
    /// Get all registered OptionSet names.
    /// </summary>
    public static IReadOnlyCollection<string> GetRegisteredOptionSetNames()
    {
        return OptionSetsByName.Keys;
    }

    /// <summary>
    /// Clear all registered OptionSets (mainly for testing).
    /// </summary>
    public static void Clear()
    {
        OptionSetsByName.Clear();
        AttributeToOptionSetMapping.Clear();
    }

    /// <summary>
    /// Re-register built-in OptionSets (mainly for testing after Clear).
    /// </summary>
    public static void RegisterBuiltIns()
    {
        RegisterBuiltInOptionSets();
    }

    private static void RegisterBuiltInOptionSets()
    {
        // Register in_countryos (Country OptionSet)
        Register(new GlobalOptionSetDefinition
        {
            Name = "in_countryos",
            Description = "in_countryos enum (194 values)",
            AttributeLogicalNames = ["in_country"],
            Values = CreateCountryOptionSet()
        });

        // Register in_syncstatusos (Sync Status OptionSet)
        Register(new GlobalOptionSetDefinition
        {
            Name = "in_syncstatusos",
            Description = "in_syncstatusos enum (2 values)",
            AttributeLogicalNames = ["in_syncstatus"],
            Values =
            [
                new() { Value = 1, Label = "OK" },
                new() { Value = 2, Label = "Error" }
            ]
        });

        // Add more global OptionSets here as needed
        // Example:
        // Register(new GlobalOptionSetDefinition
        // {
        //     Name = "in_systemtype",
        //     Description = "in_systemtype enum (N values)",
        //     AttributeLogicalNames = ["in_system", "in_systemtype"],
        //     Values = [...]
        // });
    }

    private static List<OptionSetValue> CreateCountryOptionSet()
    {
        // Complete list of 194 countries
        return
        [
            new() { Value = 1, Label = "Afghanistan" },
            new() { Value = 2, Label = "Albania" },
            new() { Value = 3, Label = "Algeria" },
            new() { Value = 4, Label = "Andorra" },
            new() { Value = 5, Label = "Angola" },
            new() { Value = 6, Label = "Argentina" },
            new() { Value = 7, Label = "Armenia" },
            new() { Value = 8, Label = "Australia" },
            new() { Value = 9, Label = "Austria" },
            new() { Value = 10, Label = "Azerbaijan" },
            new() { Value = 11, Label = "Bahamas" },
            new() { Value = 12, Label = "Bahrain" },
            new() { Value = 13, Label = "Bangladesh" },
            new() { Value = 14, Label = "Barbados" },
            new() { Value = 15, Label = "Belarus" },
            new() { Value = 16, Label = "Belgium" },
            new() { Value = 17, Label = "Belize" },
            new() { Value = 18, Label = "Benin" },
            new() { Value = 19, Label = "Bhutan" },
            new() { Value = 20, Label = "Bolivia" },
            new() { Value = 21, Label = "BosniaandHerzegovina" },
            new() { Value = 22, Label = "Botswana" },
            new() { Value = 23, Label = "Brazil" },
            new() { Value = 24, Label = "Brunei" },
            new() { Value = 25, Label = "Bulgaria" },
            new() { Value = 26, Label = "BurkinaFaso" },
            new() { Value = 27, Label = "Burundi" },
            new() { Value = 28, Label = "CaboVerde" },
            new() { Value = 29, Label = "Cambodia" },
            new() { Value = 30, Label = "Cameroon" },
            new() { Value = 31, Label = "Canada" },
            new() { Value = 32, Label = "CentralAfricanRepublic" },
            new() { Value = 33, Label = "Chad" },
            new() { Value = 34, Label = "Chile" },
            new() { Value = 35, Label = "China" },
            new() { Value = 36, Label = "Colombia" },
            new() { Value = 37, Label = "Comoros" },
            new() { Value = 38, Label = "Congo" },
            new() { Value = 39, Label = "CostaRica" },
            new() { Value = 40, Label = "Croatia" },
            new() { Value = 41, Label = "Cuba" },
            new() { Value = 42, Label = "Cyprus" },
            new() { Value = 43, Label = "Czechia" },
            new() { Value = 44, Label = "Denmark" },
            new() { Value = 45, Label = "Djibouti" },
            new() { Value = 46, Label = "Dominica" },
            new() { Value = 47, Label = "DominicanRepublic" },
            new() { Value = 48, Label = "Ecuador" },
            new() { Value = 49, Label = "Egypt" },
            new() { Value = 50, Label = "ElSalvador" },
            new() { Value = 51, Label = "EquatorialGuinea" },
            new() { Value = 52, Label = "Eritrea" },
            new() { Value = 53, Label = "Estonia" },
            new() { Value = 54, Label = "Eswatini" },
            new() { Value = 55, Label = "Ethiopia" },
            new() { Value = 56, Label = "Fiji" },
            new() { Value = 57, Label = "Finland" },
            new() { Value = 58, Label = "France" },
            new() { Value = 59, Label = "Gabon" },
            new() { Value = 60, Label = "Gambia" },
            new() { Value = 61, Label = "Georgia" },
            new() { Value = 62, Label = "Germany" },
            new() { Value = 63, Label = "Ghana" },
            new() { Value = 64, Label = "Greece" },
            new() { Value = 65, Label = "Grenada" },
            new() { Value = 66, Label = "Guatemala" },
            new() { Value = 67, Label = "Guinea" },
            new() { Value = 68, Label = "GuineaBissau" },
            new() { Value = 69, Label = "Guyana" },
            new() { Value = 70, Label = "Haiti" },
            new() { Value = 71, Label = "Honduras" },
            new() { Value = 72, Label = "Hungary" },
            new() { Value = 73, Label = "Iceland" },
            new() { Value = 74, Label = "India" },
            new() { Value = 75, Label = "Indonesia" },
            new() { Value = 76, Label = "Iran" },
            new() { Value = 77, Label = "Iraq" },
            new() { Value = 78, Label = "Ireland" },
            new() { Value = 79, Label = "Israel" },
            new() { Value = 80, Label = "Italy" },
            new() { Value = 81, Label = "Jamaica" },
            new() { Value = 82, Label = "Japan" },
            new() { Value = 83, Label = "Jordan" },
            new() { Value = 84, Label = "Kazakhstan" },
            new() { Value = 85, Label = "Kenya" },
            new() { Value = 86, Label = "Kiribati" },
            new() { Value = 87, Label = "NorthKorea" },
            new() { Value = 88, Label = "SouthKorea" },
            new() { Value = 89, Label = "Kosovo" },
            new() { Value = 90, Label = "Kuwait" },
            new() { Value = 91, Label = "Kyrgyzstan" },
            new() { Value = 92, Label = "Laos" },
            new() { Value = 93, Label = "Latvia" },
            new() { Value = 94, Label = "Lebanon" },
            new() { Value = 95, Label = "Lesotho" },
            new() { Value = 96, Label = "Liberia" },
            new() { Value = 97, Label = "Libya" },
            new() { Value = 98, Label = "Liechtenstein" },
            new() { Value = 99, Label = "Lithuania" },
            new() { Value = 100, Label = "Luxembourg" },
            new() { Value = 101, Label = "Madagascar" },
            new() { Value = 102, Label = "Malawi" },
            new() { Value = 103, Label = "Malaysia" },
            new() { Value = 104, Label = "Maldives" },
            new() { Value = 105, Label = "Mali" },
            new() { Value = 106, Label = "Malta" },
            new() { Value = 107, Label = "MarshallIslands" },
            new() { Value = 108, Label = "Mauritania" },
            new() { Value = 109, Label = "Mauritius" },
            new() { Value = 110, Label = "Mexico" },
            new() { Value = 111, Label = "Micronesia" },
            new() { Value = 112, Label = "Moldova" },
            new() { Value = 113, Label = "Monaco" },
            new() { Value = 114, Label = "Mongolia" },
            new() { Value = 115, Label = "Montenegro" },
            new() { Value = 116, Label = "Morocco" },
            new() { Value = 117, Label = "Mozambique" },
            new() { Value = 118, Label = "Myanmar" },
            new() { Value = 119, Label = "Namibia" },
            new() { Value = 120, Label = "Nauru" },
            new() { Value = 121, Label = "Nepal" },
            new() { Value = 122, Label = "Netherlands" },
            new() { Value = 123, Label = "NewZealand" },
            new() { Value = 124, Label = "Nicaragua" },
            new() { Value = 125, Label = "Niger" },
            new() { Value = 126, Label = "Nigeria" },
            new() { Value = 127, Label = "NorthMacedonia" },
            new() { Value = 128, Label = "Norway" },
            new() { Value = 129, Label = "Oman" },
            new() { Value = 130, Label = "Pakistan" },
            new() { Value = 131, Label = "Palau" },
            new() { Value = 132, Label = "PalestineState" },
            new() { Value = 133, Label = "Panama" },
            new() { Value = 134, Label = "PapuaNewGuinea" },
            new() { Value = 135, Label = "Paraguay" },
            new() { Value = 136, Label = "Peru" },
            new() { Value = 137, Label = "Philippines" },
            new() { Value = 138, Label = "Poland" },
            new() { Value = 139, Label = "Portugal" },
            new() { Value = 140, Label = "Qatar" },
            new() { Value = 141, Label = "Romania" },
            new() { Value = 142, Label = "Russia" },
            new() { Value = 143, Label = "Rwanda" },
            new() { Value = 144, Label = "SaintKittsandNevis" },
            new() { Value = 145, Label = "SaintLucia" },
            new() { Value = 146, Label = "SaintVincentandtheGrenadines" },
            new() { Value = 147, Label = "Samoa" },
            new() { Value = 148, Label = "SanMarino" },
            new() { Value = 149, Label = "SaoTomeandPrincipe" },
            new() { Value = 150, Label = "SaudiArabia" },
            new() { Value = 151, Label = "Senegal" },
            new() { Value = 152, Label = "Serbia" },
            new() { Value = 153, Label = "Seychelles" },
            new() { Value = 154, Label = "SierraLeone" },
            new() { Value = 155, Label = "Singapore" },
            new() { Value = 156, Label = "Slovakia" },
            new() { Value = 157, Label = "Slovenia" },
            new() { Value = 158, Label = "SolomonIslands" },
            new() { Value = 159, Label = "Somalia" },
            new() { Value = 160, Label = "SouthAfrica" },
            new() { Value = 161, Label = "SouthSudan" },
            new() { Value = 162, Label = "Spain" },
            new() { Value = 163, Label = "SriLanka" },
            new() { Value = 164, Label = "Sudan" },
            new() { Value = 165, Label = "Suriname" },
            new() { Value = 166, Label = "Sweden" },
            new() { Value = 167, Label = "Switzerland" },
            new() { Value = 168, Label = "Syria" },
            new() { Value = 169, Label = "Taiwan" },
            new() { Value = 170, Label = "Tajikistan" },
            new() { Value = 171, Label = "Tanzania" },
            new() { Value = 172, Label = "Thailand" },
            new() { Value = 173, Label = "TimorLeste" },
            new() { Value = 174, Label = "Togo" },
            new() { Value = 175, Label = "Tonga" },
            new() { Value = 176, Label = "TrinidadandTobago" },
            new() { Value = 177, Label = "Tunisia" },
            new() { Value = 178, Label = "Turkey" },
            new() { Value = 179, Label = "Turkmenistan" },
            new() { Value = 180, Label = "Tuvalu" },
            new() { Value = 181, Label = "Uganda" },
            new() { Value = 182, Label = "Ukraine" },
            new() { Value = 183, Label = "UnitedArabEmirates" },
            new() { Value = 184, Label = "UnitedKingdom" },
            new() { Value = 185, Label = "UnitedStatesofAmerica" },
            new() { Value = 186, Label = "Uruguay" },
            new() { Value = 187, Label = "Uzbekistan" },
            new() { Value = 188, Label = "Vanuatu" },
            new() { Value = 189, Label = "VaticanCity" },
            new() { Value = 190, Label = "Venezuela" },
            new() { Value = 191, Label = "Vietnam" },
            new() { Value = 192, Label = "Yemen" },
            new() { Value = 193, Label = "Zambia" },
            new() { Value = 194, Label = "Zimbabwe" }
        ];
    }
}

/// <summary>
/// Represents a global OptionSet definition with its values and metadata.
/// </summary>
public sealed class GlobalOptionSetDefinition
{
    /// <summary>
    /// The OptionSet name (e.g., "in_countryos").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description for the OptionSet (used in JSON Schema).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// List of attribute logical names that use this OptionSet.
    /// This enables lookup by attribute name when OptionSetName is not populated.
    /// </summary>
    public List<string> AttributeLogicalNames { get; init; } = [];

    /// <summary>
    /// The OptionSet values (options).
    /// </summary>
    public required List<OptionSetValue> Values { get; init; }
}
