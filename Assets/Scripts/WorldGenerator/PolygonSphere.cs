using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public struct VertexContext
{
    public Vector3 Vertex;
    public int Polygon1;
    public int Polygon2;
    public int Polygon3;
}

public class PolygonSphere
{
    public enum PolygonType { Pole, Band, Zone }

    public enum Direction { Clockwise, Counterclockwise }

    // deformation effects relative size of pentagons and hexagons of different types [SerializeField, Range(0.5f, 2f)]
    const float deformation = 1f;

    List<Polygon> polygons;
    List<Edge> edges;
    PolygonData[] polygonData;
    EdgeData[] edgeData;

    public int BandSize { get; }

    public PolygonSphere(int bandSize)
    {
        polygons = new List<Polygon>();
        edges = new List<Edge>();
        BandSize = bandSize;

        generatePolygons();
        generateEdges();
        polygonData = new PolygonData[polygons.Count];
        edgeData = new EdgeData[edges.Count];
    }

    public PolygonData GetPolygonData(int index) => polygonData[index];

    public EdgeData GetEdgeData(int index) => edgeData[index];

    public int GetEdge(int p1, int p2)
    {
        for (int direction = 0; direction < polygons[p1].Sides; direction++)
        {
            if (getNeighbor(p1, direction) == p2)
            {
                return polygons[p1].GetEdge(direction);
            }
        }
        return -1;
    }

    public int PolygonCount => polygons.Count;

    public int EdgeCount => edges.Count;

    public PolygonType GetPolygonType(int polygonIndex)
    {
        if (polygonIndex < 12)
        {
            return PolygonType.Pole;
        }
        else
        {
            return polygonIndex - 12 < BandSize * 30 ? PolygonType.Band : PolygonType.Zone;
        }
    }

    public int GetSides(int polygonIndex) => polygons[polygonIndex].Sides;

    public Vector3 GetCenter(int polygonIndex) => polygons[polygonIndex].Center;

    public int GetEdgePolygon1(int edgeIndex) => edges[edgeIndex].Polygon1;

    public int GetEdgePolygon2(int edgeIndex) => edges[edgeIndex].Polygon2;

    public IEnumerable<int> GetNeighbors(int polygonIndex)
    {
        Polygon p = polygons[polygonIndex];
        for (int direction = 0; direction < p.Sides; direction++)
        {
            Edge edge = edges[p.GetEdge(direction)];
            yield return edge.Polygon1 == polygonIndex ? edge.Polygon2 : edge.Polygon1;
        }
    }


    public Vector3[] GetEdgeVertices(int index)
    {
        Edge edge = edges[index];
        Polygon p1 = polygons[edge.Polygon1];
        int direction = p1.GetDirectionByEdge(index);
        Vector3[] vertices = new Vector3[] { p1.GetVertex(direction), p1.GetVertex((direction + 1) % p1.Sides) };

        return vertices;
    }

    public VertexContext[] GetEdgeVerticesContext(int index)
    {
        Edge edge = edges[index];
        Polygon p1 = polygons[edge.Polygon1];
        int direction = p1.GetDirectionByEdge(index);

        VertexContext[] context = new VertexContext[2];

        context[0] = new VertexContext()
        {
            Vertex = p1.GetVertex(direction),
            Polygon1 = edge.Polygon1,
            Polygon2 = edge.Polygon2,
            Polygon3 = getNeighbor(edge.Polygon1, (direction + p1.Sides - 1) % p1.Sides),
        };

        context[1] = new VertexContext()
        {
            Vertex = p1.GetVertex((direction + 1) % p1.Sides),
            Polygon1 = edge.Polygon1,
            Polygon2 = edge.Polygon2,
            Polygon3 = getNeighbor(edge.Polygon1, (direction + 1) % p1.Sides)
        };

        return context;
    }

    public IEnumerable<Vector3> GetPolygonVertices(int polygonIndex) => polygons[polygonIndex].Vertices;

    public IEnumerable<VertexContext> GetPolygonVerticesContext(int index)
    {
        Polygon p = polygons[index];

        for (int direction = 0, prevDirection = p.Sides - 1; direction < p.Sides; direction++)
        {
            yield return new VertexContext()
            {
                Vertex = p.GetVertex(direction),
                Polygon1 = getNeighbor(index, prevDirection),
                Polygon2 = getNeighbor(index, direction),
                Polygon3 = index
            };
            prevDirection = direction;
        }
    }

    public Vector3 GetNextVertex (int edgeIndex, int polygonIndex, Direction direction)
    {
        Polygon polygon = polygons[polygonIndex];
        int d1 = polygon.GetDirectionByEdge(edgeIndex);

        if (d1 == -1)
        {
            throw new System.Exception("The specified edge doesn't belong to the specified polygon.");
        }
        else
        {
            int delta = direction == Direction.Clockwise ? 2 : polygon.Sides - 1;
            int d2 = (d1 + delta) % polygon.Sides;
            return polygon.GetVertex(d2);
        }        
    }

    public VertexContext GetNextVertexContext(int edgeIndex, int polygonIndex, Direction direction)
    {
        Polygon polygon = polygons[polygonIndex];
        int d1 = polygon.GetDirectionByEdge(edgeIndex);

        if (d1 == -1)
        {
            throw new System.Exception("The specified edge doesn't belong to the specified polygon.");
        }
        else
        {
            int delta = direction == Direction.Clockwise ? 2 : polygon.Sides - 1;
            int d2 = (d1 + delta) % polygon.Sides;

            VertexContext context = new VertexContext()
            {
                Vertex = polygon.GetVertex(d2),
                Polygon1 = getNeighbor(polygonIndex, d2),
                Polygon2 = getNeighbor(polygonIndex, (d2 + polygon.Sides - 1) % polygon.Sides),
                Polygon3 = polygonIndex
            };

            return context;
        }
    }

    public void RegenerateData(IHeightGenerator heightGenerator)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            polygonData[i] = generatePolygonData(i, heightGenerator);
        }

        for (int i = 0; i < edges.Count; i++)
        {
            edgeData[i] = generateEdgeData(heightGenerator);
        }
    }

    public string GetPolygonInfo(int polygonIndex)
    {
        if (BandSize == 0)
        {
            return "Hexagon: " + polygonIndex;
        }

        int bandIndex = (polygonIndex - 12) / BandSize;
        int zoneIndex = BandSize == 1 ? -1 : (polygonIndex - 12 - 30 * BandSize) / (BandSize * (BandSize - 1) / 2);

        string parentInfo = ", Parent: " + polygons[polygonIndex].Parent;

        if (polygonIndex < 12)
        {
            return "Pentagon: " + polygonIndex + parentInfo;
        }
        else if (bandIndex < 30)
        {
            return "Hexagon: " + polygonIndex + ", Band: " + bandIndex + parentInfo;
        }
        else
        {
            return "Hexagon: " + polygonIndex + ", Zone: " + zoneIndex + parentInfo;
        }
    }

    int getNeighbor(int polygonIndex, int direction)
    {
        Polygon polygon = polygons[polygonIndex];
        Edge edge = edges[polygon.GetEdge(direction)];
        return edge.Polygon1 == polygonIndex ? edge.Polygon2 : edge.Polygon1;
    }

    private void generatePolygons()
    {
        Vector3[][] zoneVectors = new Vector3[20][];

        for (int i = 0; i < 20; i++)
        {
            zoneVectors[i] = generateZoneVectors(i);
        }

        for (int i = 0; i < 12; i++)
        {
            polygons.Add(generatePentagon(zoneVectors, i));
        }

        for (int i = 0; i < 30; i++)
        {
            polygons.AddRange(generateBorderHexagons(zoneVectors, i));
        }

        for (int i = 0; i < 20; i++)
        {
            polygons.AddRange(generateZoneHexagons(zoneVectors, i));
        }
    }

    Vector3[] generateZoneVectors(int zoneIndex)
    {
        Vector3[] vertices = new Vector3[4];
        vertices[0] = GlobeTopology.GetVectorFromZone(zoneIndex, 0);
        vertices[1] = GlobeTopology.GetVectorFromZone(zoneIndex, 1);
        vertices[2] = GlobeTopology.GetVectorFromZone(zoneIndex, 2);
        vertices[3] = (vertices[0] + vertices[1] + vertices[2]) * (1f / 3f) * deformation;

        return vertices;
    }

    Polygon generatePentagon(Vector3[][] zoneVectors, int zoneIndex)
    {
        float reduction = radiusReduction(0);
        Vector3 center = GlobeTopology.GetVertex(zoneIndex);

        Polygon pentagon = new Polygon(5);

        for (int i = 0; i < 5; i++)
        {
            Vector3 zoneCenter = zoneVectors[GlobeTopology.GetZoneFromVertex(zoneIndex, i)][3];
            pentagon.SetVertex(i, Vector3.Lerp(center, zoneCenter, reduction));
        }

        pentagon.Parent = zoneIndex;

        return pentagon;
    }

    Polygon[] generateBorderHexagons(Vector3[][] zoneVectors, int bandIndex)
    {
        if (BandSize < 1)
        {
            return new Polygon[0];
        }

        int zi1 = GlobeTopology.GetZoneFromBand(bandIndex, 0);
        int zi2 = GlobeTopology.GetZoneFromBand(bandIndex, 1);
        int d1 = GlobeTopology.GetDirectionFromBand(bandIndex, 0);
        int d2 = GlobeTopology.GetDirectionFromBand(bandIndex, 1);

        float reduction1 = radiusReduction(0);
        float reduction2 = radiusReduction(1);

        Vector3[] vertices = new Vector3[8];

        vertices[0] = Vector3.Lerp(zoneVectors[zi1][(d1 + 1) % 3], zoneVectors[zi1][3], reduction1);
        vertices[1] = Vector3.Lerp(zoneVectors[zi1][d1], zoneVectors[zi1][3], reduction1);
        vertices[2] = Vector3.Lerp(zoneVectors[zi2][d2], zoneVectors[zi2][3], reduction1);
        vertices[3] = Vector3.Lerp(zoneVectors[zi2][(d2 + 1) % 3], zoneVectors[zi2][3], reduction1);

        vertices[4] = Vector3.Lerp(zoneVectors[zi1][(d1 + 1) % 3], zoneVectors[zi1][3], reduction2);
        vertices[5] = Vector3.Lerp(zoneVectors[zi1][d1], zoneVectors[zi1][3], reduction2);
        vertices[6] = Vector3.Lerp(zoneVectors[zi2][d2], zoneVectors[zi2][3], reduction2);
        vertices[7] = Vector3.Lerp(zoneVectors[zi2][(d2 + 1) % 3], zoneVectors[zi2][3], reduction2);

        Polygon[] hexagons = new Polygon[BandSize];

        for (int i = 0; i < BandSize; i++)
        {
            float t1 = (float)i / BandSize;
            float t2 = (float)(i + 1) / BandSize;
            float t3 = (float)i / (max(BandSize, 2) - 1);
            hexagons[i] = new Polygon(6);
            hexagons[i].SetVertex(0, Vector3.Lerp(vertices[0], vertices[1], t1));
            hexagons[i].SetVertex(1, Vector3.Lerp(vertices[4], vertices[5], t3));
            hexagons[i].SetVertex(2, Vector3.Lerp(vertices[0], vertices[1], t2));
            hexagons[i].SetVertex(5, Vector3.Lerp(vertices[2], vertices[3], t1));
            hexagons[i].SetVertex(4, Vector3.Lerp(vertices[6], vertices[7], t3));
            hexagons[i].SetVertex(3, Vector3.Lerp(vertices[2], vertices[3], t2));

            hexagons[i].Parent = i % 2 == 0 || BandSize % 2 == 0 ? -1 : (BandSize - 1) / 2 * bandIndex + (i - 1) / 2 + 12;
        }

        return hexagons;
    }

    Polygon[] generateZoneHexagons(Vector3[][] zoneVectors, int zone)
    {

        if (BandSize < 2)
        {
            return new Polygon[0];
        }

        float reduction1 = radiusReduction(0);
        float reduction2 = radiusReduction(1);

        Vector3[] vertices = new Vector3[6];

        vertices[0] = Vector3.Lerp(zoneVectors[zone][0], zoneVectors[zone][3], reduction1);
        vertices[1] = Vector3.Lerp(zoneVectors[zone][0], zoneVectors[zone][3], reduction2);
        vertices[2] = Vector3.Lerp(zoneVectors[zone][2], zoneVectors[zone][3], reduction1);
        vertices[3] = Vector3.Lerp(zoneVectors[zone][2], zoneVectors[zone][3], reduction2);
        vertices[4] = Vector3.Lerp(zoneVectors[zone][1], zoneVectors[zone][3], reduction1);
        vertices[5] = Vector3.Lerp(zoneVectors[zone][1], zoneVectors[zone][3], reduction2);

        int hexagonCount = BandSize * (BandSize - 1) / 2;

        Polygon[] hexagons = new Polygon[hexagonCount];

        Vector3[] borderVertices = new Vector3[8];

        for (int i = 0, hexIndex = 0; i < BandSize - 1; i++)
        {
            float t1 = (float)(i + 1) / BandSize;
            float t2 = (float)(i + 2) / BandSize;
            float t3 = (float)i / (BandSize - 1);
            float t4 = (float)(i + 1) / (BandSize - 1);

            borderVertices[0] = Vector3.Lerp(vertices[1], vertices[3], t3);
            borderVertices[1] = Vector3.Lerp(vertices[1], vertices[5], t3);
            borderVertices[2] = Vector3.Lerp(vertices[0], vertices[2], t1);
            borderVertices[3] = Vector3.Lerp(vertices[0], vertices[4], t1);
            borderVertices[4] = Vector3.Lerp(vertices[1], vertices[3], t4);
            borderVertices[5] = Vector3.Lerp(vertices[1], vertices[5], t4);
            borderVertices[6] = Vector3.Lerp(vertices[0], vertices[2], t2);
            borderVertices[7] = Vector3.Lerp(vertices[0], vertices[4], t2);

            for (int j = 0; j < i + 1; j++, hexIndex++)
            {
                float t5 = (float)j / max(1, i);
                float t6 = (float)(j + 1) / (i + 1);
                float t7 = (float)(j + 1) / (i + 2);
                float t8 = (float)j / (i + 1);

                hexagons[hexIndex] = new Polygon(6);

                hexagons[hexIndex].SetVertex(0, Vector3.Lerp(borderVertices[0], borderVertices[1], t5));
                hexagons[hexIndex].SetVertex(1, Vector3.Lerp(borderVertices[2], borderVertices[3], t6));
                hexagons[hexIndex].SetVertex(2, Vector3.Lerp(borderVertices[4], borderVertices[5], t6));
                hexagons[hexIndex].SetVertex(3, Vector3.Lerp(borderVertices[6], borderVertices[7], t7));
                hexagons[hexIndex].SetVertex(4, Vector3.Lerp(borderVertices[4], borderVertices[5], t8));
                hexagons[hexIndex].SetVertex(5, Vector3.Lerp(borderVertices[2], borderVertices[3], t8));

                if (i % 2 == 1 || j % 2 == 0)
                {
                    hexagons[hexIndex].Parent = -1;
                }
                else
                {
                    int parentBandSize = (BandSize - 1) / 2;
                    int parentZoneSize = parentBandSize * (parentBandSize - 1) / 2;
                    int ii = (i - 1) / 2;
                    int iii = ii * (ii + 1) / 2;
                    hexagons[hexIndex].Parent = iii + j / 2 + 12 + parentBandSize * 30 + parentZoneSize * zone;
                }
            }
        }

        return hexagons;
    }

    void generateEdges()
    {
        if (BandSize == 0)
        {
            generatePentagonEdges();
        }
        else
        {
            for (int bandIndex = 0; bandIndex < 30; bandIndex++)
            {
                for (int polygonPosition = 0; polygonPosition < BandSize; polygonPosition++)
                {
                    generateBandHexEdges(bandIndex, polygonPosition);
                }
            }

            for (int zoneIndex = 0; zoneIndex < 20; zoneIndex++)
            {
                for (int polygonRow = 0; polygonRow < BandSize - 1; polygonRow++)
                {
                    for (int polygonColumn = 0; polygonColumn < polygonRow + 1; polygonColumn++)
                    {
                        generateZoneHexEdges(zoneIndex, polygonRow, polygonColumn);
                    }
                }
            }
        }       
    }

    void generatePentagonEdges()
    {
        for (int edgeIndex = 0; edgeIndex < 30; edgeIndex++)
        {
            int zone = GlobeTopology.GetZoneFromBand(edgeIndex, 0);
            int direction = GlobeTopology.GetDirectionFromBand(edgeIndex, 0);
            int pentagon1 = GlobeTopology.GetVertexFromZone(zone, direction);
            int pentagon2 = GlobeTopology.GetVertexFromZone(zone, (direction + 1) % 3);
            int d1 = (GlobeTopology.GetDirectionFromVertex(pentagon1, zone) + 4) % 5;
            int d2 = GlobeTopology.GetDirectionFromVertex(pentagon2, zone);
            generateEdge(pentagon1, pentagon2, d1, d2);
        }
    }

    void generateBandHexEdges(int bandIndex, int i)
    {
        int zi1 = GlobeTopology.GetZoneFromBand(bandIndex, 0);
        int zi2 = GlobeTopology.GetZoneFromBand(bandIndex, 1);
        int d1 = GlobeTopology.GetDirectionFromBand(bandIndex, 0);
        int d2 = GlobeTopology.GetDirectionFromBand(bandIndex, 1);
        int polygonIndex = 12 + bandIndex * BandSize + i;

        int zoneSize = BandSize * (BandSize - 1) / 2;
        int z1FirstPolygonIndex = 12 + 30 * BandSize + zi1 * zoneSize;
        int z1LastPolygonIndex = z1FirstPolygonIndex + zoneSize;
        int z2FirstPolygonIndex = 12 + 30 * BandSize + zi2 * zoneSize;
        int z2LastPolygonIndex = z2FirstPolygonIndex + zoneSize;

        int neighborIndex = -1;
        int neighborDir = -1;

        if (i == 0)
        {
            int neighborBand = GlobeTopology.GetBandFromZone(zi1, (d1 + 1) % 3);
            neighborIndex = 12 + neighborBand * BandSize + (GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi1 ? BandSize - 1 : 0);
            neighborDir = GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi1 ? 1 : 4;
        }
        else if (d1 == 0)
        {
            neighborIndex = z1FirstPolygonIndex + (BandSize - i) * (BandSize - i + 1) / 2 - 1;
            neighborDir = 0;
        }
        else if (d1 == 1)
        {
            neighborIndex = z1LastPolygonIndex - BandSize + i;
            neighborDir = 2;
        }
        else if (d1 == 2)
        {
            neighborIndex = z1FirstPolygonIndex + i * (i - 1) / 2;
            neighborDir = 4;
        }
        generateEdge(polygonIndex, neighborIndex, 0, neighborDir);

        if (i == BandSize - 1)
        {
            int neighborBand = GlobeTopology.GetBandFromZone(zi1, (d1 + 2) % 3);
            neighborIndex = 12 + neighborBand * BandSize + (GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi1 ? 0 : BandSize - 1);
            neighborDir = GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi1 ? -1 : 3;
        }
        else if (d1 == 0)
        {
            neighborIndex = z1FirstPolygonIndex + (BandSize - i - 1) * (BandSize - i) / 2 - 1;
            neighborDir = 1;
        }
        else if (d1 == 1)
        {
            neighborIndex = z1LastPolygonIndex - BandSize + i + 1;
            neighborDir = 3;
        }
        else if (d1 == 2)
        {
            neighborIndex = z1FirstPolygonIndex + i * (i + 1) / 2;
            neighborDir = 5;
        }
        generateEdge(polygonIndex, neighborIndex, 1, neighborDir);

        if (i == BandSize - 1)
        {
            int neighborBand = GlobeTopology.GetBandFromZone(zi2, (d2 + 1) % 3);
            neighborIndex = 12 + neighborBand * BandSize + (GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi2 ? BandSize - 1 : 0);
            neighborDir = GlobeTopology.GetZoneFromBand(neighborBand, 0) == zi2 ? -1 : 4;
        }
        else if (d2 == 0)
        {
            neighborIndex = z2FirstPolygonIndex + (i + 1) * (i + 2) / 2 - 1;
            neighborDir = 0;
        }
        else if (d2 == 1)
        {
            neighborIndex = z2LastPolygonIndex - i - 1;
            neighborDir = 2;
        }
        else if (d2 == 2)
        {
            neighborIndex = z2FirstPolygonIndex + (BandSize - i - 1) * (BandSize - i - 2) / 2;
            neighborDir = 4;
        }
        generateEdge(polygonIndex, neighborIndex, 3, neighborDir);

        if (i == 0)
        {
            neighborDir = -1;
        }
        else if (d2 == 0)
        {
            neighborIndex = z2FirstPolygonIndex + i * (i + 1) / 2 - 1;
            neighborDir = 1;
        }
        else if (d2 == 1)
        {
            neighborIndex = z2LastPolygonIndex - i;
            neighborDir = 3;
        }
        else if (d2 == 2)
        {
            neighborIndex = z2FirstPolygonIndex + (BandSize - i - 1) * (BandSize - i) / 2;
            neighborDir = 5;
        }
        generateEdge(polygonIndex, neighborIndex, 4, neighborDir);

        neighborIndex = i < BandSize - 1 ? polygonIndex + 1 : GlobeTopology.GetVertexFromZone(zi1, d1);
        neighborDir = i < BandSize - 1 ? 5 : ((GlobeTopology.GetDirectionFromVertex(neighborIndex, zi1) + 4) % 5);
        generateEdge(polygonIndex, neighborIndex, 2, neighborDir);

        if (i == 0)
        {
            neighborIndex = GlobeTopology.GetVertexFromZone(zi1, (d1 + 1) % 3);
            neighborDir = GlobeTopology.GetDirectionFromVertex(neighborIndex, zi1);
            generateEdge(polygonIndex, neighborIndex, 5, neighborDir);
        }
    }

    void generateZoneHexEdges(int zoneIndex, int i, int j)
    {
        int zoneSize = BandSize * (BandSize - 1) / 2;
        int h0 = 12 + 30 * BandSize + zoneIndex * zoneSize;
        int polygonIndex = h0 + i * (i + 1) / 2 + j;

        if (j < i)
        {
            int neighborIndex = h0 + i * (i - 1) / 2 + j;
            generateEdge(polygonIndex, neighborIndex, 0, 3);

            neighborIndex = h0 + i * (i + 1) / 2 + j + 1;
            generateEdge(polygonIndex, neighborIndex, 1, 4);
        }

        if (i < BandSize - 2)
        {
            int neighborIndex = h0 + (i + 1) * (i + 2) / 2 + j + 1;
            generateEdge(polygonIndex, neighborIndex, 2, 5);
        }
    }

    void generateEdge(int polygonIndex, int neighborIndex, int direction, int neighborDir)
    {
        if (neighborDir < 0)
        {
            return;
        }
        Edge edge = new Edge(polygonIndex, neighborIndex);
        polygons[polygonIndex].SetEdge(direction, edges.Count);
        polygons[neighborIndex].SetEdge(neighborDir, edges.Count);
        edges.Add(edge);
    }

    float radiusReduction(int step)
    {
        float b = 0.5f / sin(PI * 36f / 180f);
        float reduction = (b + step) / (b + BandSize);
        return reduction;
    }

    PolygonData generatePolygonData(int polygonIndex, IHeightGenerator heightGenerator)
    {
        PolygonData data = new PolygonData();
        data.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.25f, 1f, 1f, 1f);
        data.height = heightGenerator.GenerateHeight(polygons[polygonIndex].Center);
        Polygon polygon = polygons[polygonIndex];

        if (polygon.Parent > -1)
        {
            data.region = polygon.Parent;
        }
        else
        {
            int[] neighborParents = new int[2];
            for (int direction = 0, n = 0; direction < polygon.Sides; direction++)
            {
                Polygon neighbor = polygons[getNeighbor(polygonIndex, direction)];
                if (neighbor.Parent > -1)
                {
                    neighborParents[n++] = neighbor.Parent;
                }
            }
            data.region = neighborParents[UnityEngine.Random.Range(0f, 1f) < 0.5f ? 0 : 1];
        }

        return data;
    }

    EdgeData generateEdgeData(IHeightGenerator heightGenerator)
    {
        EdgeData data = new EdgeData { ridge = heightGenerator.GenerateRidge() };
        return data;
    }
}
