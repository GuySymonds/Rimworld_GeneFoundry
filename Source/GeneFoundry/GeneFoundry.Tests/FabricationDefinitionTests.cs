using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
namespace GeneFoundry.Tests
{
    public class FabricationDefinitionTests
    {
        private static XElement Def(string name) => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Fixtures","Defs"),"*.xml",SearchOption.AllDirectories)
            .SelectMany(p=>XDocument.Load(p).Root.Elements()).Single(e=>(string)e.Element("defName")==name);
        [Fact] public void FabricatorUsesExistingArtAndManualPoweredOneOffWorkflow()
        {
            var bench=Def("GF_GeneFabricator");
            Assert.Equal("GeneFoundry.Fabrication.Building_GeneFabricator",(string)bench.Element("thingClass"));
            Assert.Equal("(5,2)",(string)bench.Element("size"));
            Assert.Equal("true",(string)bench.Element("rotatable"));
            Assert.Equal("GeneFoundry/Buildings/GeneFabricator",(string)bench.Element("graphicData").Element("texPath"));
            Assert.Equal("GeneFoundry.Fabrication.Graphic_GeneFabricator",(string)bench.Element("graphicData").Element("graphicClass"));
            Assert.Equal("Biotech",(string)bench.Element("designationCategory"));
            Assert.Equal("GF_GeneFabrication",(string)bench.Element("researchPrerequisites").Element("li"));
            Assert.Equal("(0,0,-1)",(string)bench.Element("interactionCellOffset"));
            Assert.Contains(bench.Element("comps").Elements(),e=>(string)e.Element("compClass")=="CompPowerTrader");
            Assert.Null(bench.Element("recipes"));
            Assert.Equal("GeneFoundry.Fabrication.ITab_GeneFabricationBills", Assert.Single(bench.Element("inspectorTabs").Elements()).Value);
            Assert.Equal("RealtimeOnly", (string)bench.Element("drawerType"));
            Assert.Equal("Research",(string)Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Fixtures","Defs","WorkGiverDefs"),"*.xml").SelectMany(p=>XDocument.Load(p).Root.Elements()).Single(e=>(string)e.Element("defName")=="GF_FabricateGenePack").Element("workType"));
        }
        [Fact] public void UnfinishedItemIsOneCustomJobWithExistingArtAndNoVanillaRecipeCasts()
        {
            var item=Def("GF_UnfinishedGenePack");
            Assert.Equal("1",(string)item.Element("stackLimit"));
            Assert.Equal("GeneFoundry.Fabrication.UnfinishedGenePack",(string)item.Element("thingClass"));
            Assert.Equal("false",(string)item.Element("isUnfinishedThing"));
            Assert.Equal("GeneFoundry/UnfinishedGenePack",(string)item.Element("graphicData").Element("texPath"));
        }
        [Fact] public void GeneVatPreservesTheVanillaBuildingClassUsedByExistingSaves()
        {
            Assert.Equal("Building_WorkTable", (string)Def("GF_GeneVat").Element("thingClass"));
        }
        [Fact] public void FabricationUsesNativeRightClickPrioritisationThroughAllJobPhases()
        {
            var giver = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Fixtures","Defs","WorkGiverDefs"),"*.xml")
                .SelectMany(p=>XDocument.Load(p).Root.Elements()).Single(e=>(string)e.Element("defName")=="GF_FabricateGenePack");
            Assert.Equal("true", (string)giver.Element("directOrderable"));
            Assert.Equal("true", (string)giver.Element("prioritizeSustains"));
        }
    }
}
