using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;
public enum Sdg
{
   
    /// <summary>
    /// SDG 1: No Poverty – Ende der Armut in allen Formen überall.
    /// </summary>
    [Display(Name ="No Poverty")]
    NoPoverty = 1,

    /// <summary>
    /// SDG 2: Zero Hunger – Ende des Hungers, Erreichung der Nahrungsmittelsicherheit und einer verbesserten Ernährung.
    /// </summary>
    [Display(Name = "Zero Hunger")]
    ZeroHunger = 2,

    /// <summary>
    /// SDG 3: Good Health and Well-being – Gesundheit und Wohlergehen für alle Altersgruppen gewährleisten.
    /// </summary>
    [Display(Name = "Good Health and Well-being")]
    GoodHealthAndWellBeing = 3,

    /// <summary>
    /// SDG 4: Quality Education – Inklusive und gerechte Qualitätsbildung und lebenslange Lernmöglichkeiten fördern.
    /// </summary>
    [Display(Name = "Quality Education")]
    QualityEducation = 4,

    /// <summary>
    /// SDG 5: Gender Equality – Geschlechtergleichheit und das Empowerment aller Frauen und Mädchen erreichen.
    /// </summary>
    [Display(Name = "Gender Equality")]
    GenderEquality = 5,

    /// <summary>
    /// SDG 6: Clean Water and Sanitation – Nachhaltiges Management und Nutzung von Wasser- und Sanitärversorgung gewährleisten.
    /// </summary>
    [Display(Name = "Clean Water and Sanitation")]
    CleanWaterAndSanitation = 6,

    /// <summary>
    /// SDG 7: Affordable and Clean Energy – Zugang zu bezahlbarer, zuverlässiger, nachhaltiger und moderner Energie für alle sicherstellen.
    /// </summary>
    [Display(Name = "Affordable and Clean Energy")]
    AffordableAndCleanEnergy = 7,

    /// <summary>
    /// SDG 8: Decent Work and Economic Growth – Nachhaltiges Wirtschaftswachstum und produktive Beschäftigung fördern.
    /// </summary>
    [Display(Name = "Decent Work and Economic Growth")]
    DecentWorkAndEconomicGrowth = 8,

    /// <summary>
    /// SDG 9: Industry, Innovation and Infrastructure – Nachhaltige Infrastruktur aufbauen und Industrie, Innovation und Infrastruktur fördern.
    /// </summary>
    [Display(Name = "Industry, Innovation and Infrastructure")]
    IndustryInnovationAndInfrastructure = 9,

    /// <summary>
    /// SDG 10: Reduced Inequalities – Ungleichheiten innerhalb und zwischen Ländern verringern.
    /// </summary>
    [Display(Name = "Reduced Inequalities")]
    ReducedInequalities = 10,

    /// <summary>
    /// SDG 11: Sustainable Cities and Communities – Inklusive, sichere, widerstandsfähige und nachhaltige Städte und Gemeinschaften schaffen.
    /// </summary>
    [Display(Name = "Sustainable Cities and Communities")]
    SustainableCitiesAndCommunities = 11,

    /// <summary>
    /// SDG 12: Responsible Consumption and Production – Nachhaltigen Konsum und Produktion sicherstellen.
    /// </summary>
    [Display(Name = "Responsible Consumption and Production")]
    ResponsibleConsumptionAndProduction = 12,

    /// <summary>
    /// SDG 13: Climate Action – Sofortige Maßnahmen zur Bekämpfung des Klimawandels und seiner Auswirkungen ergreifen.
    /// </summary>
    [Display(Name = "Climate Action")]
    ClimateAction = 13,

    /// <summary>
    /// SDG 14: Life Below Water – Konservierung und nachhaltige Nutzung der Meere, der Meere und der Meeresressourcen fördern.
    /// </summary>
    [Display(Name = "Life Below Water")]
    LifeBelowWater = 14,

    /// <summary>
    /// SDG 15: Life on Land – Schutz, Wiederherstellung und Förderung nachhaltiger Nutzung terrestrischer Ökosysteme.
    /// </summary>
    [Display(Name = "Life on Land")]
    LifeOnLand = 15,

    /// <summary>
    /// SDG 16: Peace, Justice and Strong Institutions – Friedliche und inklusive Gesellschaften fördern, Zugang zur Justiz für alle und wirksame, rechenschaftspflichtige Institutionen aufbauen.
    /// </summary>
    [Display(Name = "Peace, Justice and Strong Institutions")]
    PeaceJusticeAndStrongInstitutions = 16,

    /// <summary>
    /// SDG 17: Partnerships for the Goals – Stärkung der Mittel zur Umsetzung und die Systematisierung globaler Partnerschaften zur Entwicklung.
    /// </summary>
    [Display(Name = "Partnerships for the Goals")]
    PartnershipsForTheGoals = 17
}