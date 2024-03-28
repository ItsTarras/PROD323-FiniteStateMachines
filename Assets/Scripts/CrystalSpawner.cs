using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalSpawner : MonoBehaviour
{

    [SerializeField]
    [MinMaxSlider(0f, 200f)]
    protected MinMax spawnRadius = new MinMax(0f, 200f);

    [SerializeField]
    [MinMaxSlider(1, 50)]
    public MinMax separation = new MinMax(1, 5);

    float diskRadius = 10;

    [SerializeField]
    [Range(1, 20)]
    public int radius = 2;

    public int maxTries = 50;
    public Terrain terrain;
    public GameObject spawnCenter;
    public GameObject[] crystals;
    public GameObject parent;

    List<GameObject> activeList = new List<GameObject>();

    GameObject current;
    bool[,] diskGrid;
    int gridSize;

    void Start()
    {

        //Create grid to keep track of valid tiles to spawn crystal
        gridSize = (int) spawnRadius.Max * 2;
        gridSize = gridSize % 2 == 0 ? gridSize + 1 : gridSize; //make sure to be odd # for center tile
        diskGrid = new bool[gridSize, gridSize]; // representing terrain map tiles
        GenerateCrystals();
    }

    public void GenerateCrystals()
    {
        activeList.Clear();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                diskGrid[i, j] = false;
            }
        }

        activeList.Add(spawnCenter);

        while (activeList.Count > 0)
        {
            bool success = false;
            current = activeList[activeList.Count-1];

            for (int i = 0; i < maxTries; i++)
            {
                float angle = Random.Range(-180, 180);

                float r = diskRadius + Random.Range(separation.Min, separation.Max);
                float xoffset = r * Mathf.Cos(angle);
                float zoffset = r * Mathf.Sin(angle);
                float xcoord = current.transform.position.x + xoffset;
                float zcoord = current.transform.position.z + zoffset;
                Vector3 pos = new Vector3(xcoord, 0, zcoord);
                //Debug.Log(Vector3.Distance(pos, spawnCenter.transform.position) + ", " + spawnRadius.Min);
                if (Vector3.Distance(pos, spawnCenter.transform.position) > spawnRadius.Min && Vector3.Distance(pos, spawnCenter.transform.position) < spawnRadius.Max)
                {
                    success = PlaceDiskSimple(xcoord, zcoord);
                }

                if (success)
                    break;
            }

            if (!success)
            {
                activeList.RemoveAt(activeList.Count - 1);
                if (activeList.Count == 0)
                {
                    break;
                }
            }
        }
    }



    bool PlaceDiskSimple(float xc, float zc)
    {
        bool valid = true;

        int offset = (int)(gridSize - 1) / 2;
        int x = (int) (xc + offset) / (int) diskRadius;
        int y = (int) (zc + offset) / (int) diskRadius;

        x = x < 0 ? 0 : x;
        x = x > gridSize - 1 ? gridSize - 1 : x;
        y = y < 0 ? 0 : y;
        y = y > gridSize - 1 ? gridSize - 1 : y;

        if (diskGrid[x, y])
        {
            //Debug.Log(x + ", " + y + " " + diskGrid[x, y]);
            valid = false;
        }

        if (valid)
        {
            if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            {
                diskGrid[x, y] = true;

                int type = Random.Range(0, crystals.Length);

                GameObject go = Instantiate(crystals[type], new Vector3(xc, terrain.SampleHeight(new Vector3(xc, 0, zc)), zc), Quaternion.identity, transform);
                activeList.Add(go);

                for (int i = x - radius; i < x + radius; i++)
                {
                    for (int j = y - radius; j < y + radius; j++)
                    {
                        if (i >= 0 && i < gridSize && j >= 0 && j < gridSize)
                        {
                            if (i >= 0 && i < gridSize && j >= 0 && j < gridSize)
                                diskGrid[i, j] = true;
                        }
                    }
                }
            }

        }

        return valid;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            //Debug.Log("Space key is pressed");
            GenerateCrystals();
        }
    }
}
