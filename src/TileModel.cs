public class TileModel
{
    public Terrain terrain;
    public MapModel internalMap;
    public TileModel parent;
    public bool zoomable;
    public int scale;
    public string image;
    public TileResources localResources;

    public TileModel(Terrain terrain, TileModel parent, int scale, bool zoomable = false)
    {
        this.terrain = terrain;
        this.parent = parent;
        this.scale = scale;
        this.zoomable = zoomable;
        this.image = terrain.filenameForTileType();
        this.localResources = GetResources(terrain, scale);
    }

    public void SetTerrainType(Terrain.TerrainType terrainType)
    {
        terrain.terrainType = terrainType;
        image = terrain.filenameForTileType();
    }

    public TileResources GetResources(Terrain terrain, int scale) { return new TileResources(); }

    public void CalculateResourcesDelta()
    {
        var buildings = internalMap.Buildings;
        buildings.ForEach((building) =>
        {
            BuildingTemplate.Extraction extraction = building.template.extraction;
            if (extraction != null)
            {
                localResources.AddAmount(TileResources.GetResource(extraction.resource), extraction.rate);
            }
        });

        buildings.ForEach((building) =>
        {
            BuildingTemplate.Process process = building.template.process;
            if (process != null && localResources.GetAmount(TileResources.GetResource(process.input)) >= process.rate)
            {
                localResources.AddAmount(TileResources.GetResource(process.input), -process.rate);
                localResources.AddAmount(TileResources.GetResource(process.output), process.rate * process.amount);
            }
        });
    }
}
