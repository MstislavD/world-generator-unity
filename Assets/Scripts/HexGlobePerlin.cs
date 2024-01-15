using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGlobePerlin : MonoBehaviour
{
    enum NormalType { Polyhedron, Sphere }

    const int debugSeed = -1;

    int seed;
    bool initiateMeshing = false;
    bool initiateRecoloring = false;
    PolygonSphere sphere;
    PerlinHeightGenerator heightGenerator;
    float seaHeight = 0.5f;
    bool preciseSeaLevel = true;

    float seaLevelOld;
    int sphereSizeOld;

    [SerializeField]
    MeshFilter polygonMesh;

    [SerializeField]
    MeshCollider sphereCollider;

    [SerializeField, Range(0f, 1f)]
    float seaLevel = 0.7f;

    [SerializeField]
    NormalType normalsType = NormalType.Polyhedron;   

    [SerializeField]
    bool smoothPolygons = true;

    [SerializeField]
    int sphereSize = 10;

    [SerializeField, Min(1)]
    public int frequency;

    [SerializeField, Range(1, 6)]
    public int octaves;

    [SerializeField, Range(2, 4)]
    public int lacunarity;

    [SerializeField, Range(0f, 1f)]
    public float persistence;

    private void Awake()
    {
        seaLevelOld = seaLevel;
        sphereSizeOld = sphereSize;

        Noise.Settings settings = Noise.Settings.Default;
        settings.frequency = frequency;
        settings.octaves = octaves;
        settings.lacunarity = lacunarity;
        settings.persistence = persistence;
        heightGenerator = new PerlinHeightGenerator(settings);

        sphere = new PolygonSphere(sphereSize, heightGenerator);
        initiateMeshing = true;
        Regenerate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Regenerate();
        }

        if (initiateMeshing)
        {
            polygonMesh.mesh = generateSphereMesh();
            sphereCollider.sharedMesh = polygonMesh.mesh;
            initiateMeshing = false;
            initiateRecoloring = true;
        }

        if (initiateRecoloring)
        {
            recolorMesh();
            initiateRecoloring = false;
        }
    }

    private void OnValidate()
    {
        if (sphereSizeOld != sphereSize && heightGenerator != null)
        {
            sphereSizeOld = sphereSize;
            sphere = new PolygonSphere(sphereSize, heightGenerator);
        }

        if (heightGenerator != null)
        {
            heightGenerator.SetNoiseParameters(frequency, octaves, lacunarity, persistence);
            sphere.RecalculateData();
        }

        if (seaLevelOld != seaLevel)
        {
            seaLevelOld = seaLevel;
            initiateRecoloring = true;
            initiateMeshing = smoothPolygons ? true : initiateMeshing;
        }
        else
        {
            initiateMeshing = true;
        }
    }

    public void Regenerate()
    {
        seed = debugSeed < 0 ? Random.Range(0, int.MaxValue) : debugSeed;
        Random.InitState(seed);
        sphere.RegenerateData();
        initiateRecoloring = true;
        initiateMeshing = smoothPolygons ? true : initiateMeshing;
    }

    private void calculateSeaHeight()
    {
        seaHeight = 0.5f;
        float seaUpper = 1f;
        float seaLower = 0f;
        float tolerance = 0.02f;
        int runs = 0;
        bool modifySeaHeight = true;

        while (modifySeaHeight)
        {
            runs++;
            int seaCount = 0;

            for (int index = 0; index < sphere.PolygonCount; index++)
            {
                if (sphere.GetPolygonData(index).height < seaHeight)
                {
                    seaCount++;
                }
            }

            float seaPercent = (float)seaCount / sphere.PolygonCount;

            if (modifySeaHeight = Mathf.Abs(seaPercent - seaLevel) > tolerance)
            {
                if (seaPercent > seaLevel)
                {
                    seaUpper = seaHeight;
                }
                else
                {
                    seaLower = seaHeight;
                }
                seaHeight = (seaUpper + seaLower) / 2;
            }

            Debug.Log("Run " + runs + ". Sea regions: " + seaCount + " (" + Mathf.Floor((float)seaCount / sphere.PolygonCount * 100) + "%)");
        }    
    }

    Mesh generateSphereMesh()
    {
        SphereMeshGeneratorSmoothed.RegionBorderCheck smoothing = terrainBorder;
        SphereMeshGenerator meshGenerator = smoothPolygons ?
            new SphereMeshGeneratorSmoothed(sphere, smoothing) : new SphereMeshGenerator(sphere);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        for (int i = 0; i < sphere.PolygonCount; i++)
        {
            triangles.AddRange(meshGenerator.GetPolygonTriangles(vertices.Count, sphere.GetSides(i)));
            vertices.AddRange(meshGenerator.GetPolygonVertices(i));
            normals.AddRange(normalsType == NormalType.Polyhedron ? meshGenerator.NormalsFlat(i) : meshGenerator.NormalsSphere(i));
        }

        UnityEngine.Rendering.IndexFormat indexFormat = sphere.PolygonCount < 3000 ?
            UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;

        Mesh mesh = new Mesh { name = "Pentagonal sphere", indexFormat = indexFormat };
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        return mesh;
    }

    bool terrainBorder(int p1, int p2)
    {
        PolygonData d1 = sphere.GetPolygonData(p1);
        PolygonData d2 = sphere.GetPolygonData(p2);
        return d1.height < seaLevel != d2.height < seaLevel;
    }

    void recolorMesh()
    {
        if (preciseSeaLevel)
        {
            calculateSeaHeight();
        }
        else
        {
            seaHeight = seaLevel;
        }      

        List<Color> colors = new List<Color>();

        for (int i = 0; i < sphere.PolygonCount; i++)
        {
            PolygonData data = sphere.GetPolygonData(i);
            Color color = data.height < seaHeight ? Color.blue : Color.green;
            colors.AddRange(color.Populate(sphere.GetSides(i) + 1));
        }

        polygonMesh.mesh.colors = colors.ToArray();
    }
}
