using RimWorld;
namespace GeneFoundry.Recipes
{
    // Retain this type for saves made while vats used a custom Thing class.
    // Presentation now runs at map level so older Building_WorkTable vats work too.
    public class Building_GeneVat : Building_WorkTable { }
}
