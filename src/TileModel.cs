using System.Linq;
using System;
using System.Collections.Generic;
public class TileModel
{
    public Terrain terrain;
    public MapModel internalMap;
    public TileModel parent;
    public bool zoomable;
    public int scale;
    public string image;
    public TileResources localResources;

    // recursive
    public TileResources totalChildResources;
    public int highestTransportInside = int.MinValue;

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

    public void BuildingResourcesTick()
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
            if (process != null && totalChildResources.GetAmount(TileResources.GetResource(process.input)) >= process.rate)
            {
                SubtractResource(TileResources.GetResource(process.input), process.rate);
                localResources.AddAmount(TileResources.GetResource(process.output), process.rate * process.amount);
            }
        });
    }

    public int UpdateHighestTransportInside()
    {
        int transportRange = int.MinValue;
        if (internalMap != null) // this weird check makes me think it binds to maps instead
        {
            transportRange = internalMap.Buildings
               .Where((building) => building.template.transport != null)
               .Select((building) => building.template.transport.range)
               .Append(int.MinValue)
               .Max();

            foreach (var tile in internalMap.Tiles)
            {
                transportRange = Math.Max(tile.UpdateHighestTransportInside(), transportRange);
            }
        }

        highestTransportInside = transportRange;
        return transportRange;
    }

    public int CalculateHighestTransportNeigbouring()
    {
        var tileConsidered = parent;
        var highestTransportNeighboring = highestTransportInside;
        while (tileConsidered != null)
        {
            if (tileConsidered.highestTransportInside >= tileConsidered.scale)
            {
                highestTransportNeighboring = Math.Max(highestTransportNeighboring, tileConsidered.highestTransportInside);
            }
            tileConsidered = tileConsidered.parent;
        }

        return highestTransportNeighboring;
    }

    public TileResources CalculateTotalChildResources()
    {
        var resources = new TileResources();
        resources.AddTileResources(localResources);
        if (internalMap != null)
        {
            foreach (var tile in internalMap.Tiles)
            {
                resources.AddTileResources(tile.CalculateTotalChildResources());
            }
        }

        totalChildResources = resources;
        return resources;
    }

    public TileModel GetParentAtScale(int scale)
    {
        if (scale < this.scale) { return null; }
        if (scale == this.scale || parent == null) { return this; }
        return parent.GetParentAtScale(scale);
    }

    public TileResources GetAvailableResources()
    {
        var parent = GetParentAtScale(CalculateHighestTransportNeigbouring());
        if (parent == null)
        {
            return new TileResources();
        }
        else
        {
            return parent.totalChildResources;
        }
    }

    public void SubtractResource(Resource resource, double amount)
    {
        var localChange = Math.Min(amount, localResources.GetAmount(resource));
        amount -= localChange;
        localResources.AddAmount(resource, -localChange);
        SubtractFromAllParents(resource, localChange);

        var childrenWithResource = GetChildrenWithResource(resource);
        var totalAmountInChildren = totalChildResources.GetAmount(resource) - localChange;
        childrenWithResource.ForEach((it) =>
        {
            it.Item1.SubtractResource(resource, amount * (it.Item2 / totalAmountInChildren));
        });

    }

    private void SubtractFromAllParents(Resource resource, double amount)
    {
        var tile = this;
        while (tile != null)
        {
            tile.totalChildResources.AddAmount(resource, -amount);
            tile = tile.parent;
        }
    }

    public List<(TileModel, double)> GetChildrenWithResource(Resource resource)
    {
        var childrenWithResource = new List<(TileModel, double)>();
        if (internalMap != null)
        {
            foreach (var tile in internalMap.Tiles)
            {
                var resourcesInTile = tile.totalChildResources.GetAmount(resource);
                if (resourcesInTile > 0)
                {
                    childrenWithResource.Add((tile, resourcesInTile));
                }
            }
        }
        return childrenWithResource;
    }
}
