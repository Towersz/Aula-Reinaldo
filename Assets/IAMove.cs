using UnityEngine;

public class GridNode
{
    public int x;
    public int y;

    public int cost;

    public GridNode(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class IAMove : MonoBehaviour
{
    public float nodeSize = 1f;
    public Vector2Int size;
    private GridNode[,] nodes;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nodes = new GridNode[size.x, size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                nodes[x, y] = new GridNode(x, y);
                nodes[x, y].cost = IsWalkable(new Vector3(x * nodeSize, 0, y * nodeSize)) ? 1 : int.MaxValue;
                //Debug.Log($"Node ({x}, {y}) cost: {nodes[x, y].cost}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(WorldToNode(transform.position).x + " " + WorldToNode(transform.position).y);

        DebugControl();

    }

    void DebugControl()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Vector2Int currentNode = new(WorldToNode(transform.position).x, WorldToNode(transform.position).y);
            if (currentNode.x + 1 < size.x && nodes[currentNode.x + 1, currentNode.y].cost != int.MaxValue)
            {
                transform.position += new Vector3(nodeSize, 0, 0);
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Vector2Int currentNode = new(WorldToNode(transform.position).x, WorldToNode(transform.position).y);
            if (currentNode.x - 1 >= 0 && nodes[currentNode.x - 1, currentNode.y].cost != int.MaxValue)
            {
                transform.position += new Vector3(-nodeSize, 0, 0);
            }
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Vector2Int currentNode = new(WorldToNode(transform.position).x, WorldToNode(transform.position).y);
            if (currentNode.y + 1 < size.y && nodes[currentNode.x, currentNode.y + 1].cost != int.MaxValue)
            {
                transform.position += new Vector3(0, 0, nodeSize);
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Vector2Int currentNode = new(WorldToNode(transform.position).x, WorldToNode(transform.position).y);
            if (currentNode.y - 1 >= 0 && nodes[currentNode.x, currentNode.y - 1].cost != int.MaxValue)
            {
                transform.position += new Vector3(0, 0, -nodeSize);
            }
        }

    }

    public GridNode WorldToNode(Vector3 world)
    {
        Vector3 local = world;
        int x = Mathf.FloorToInt(local.x / nodeSize);
        int y = Mathf.FloorToInt(local.z / nodeSize);

        x = Mathf.Clamp(x, 0, size.x - 1);
        y = Mathf.Clamp(y, 0, size.y - 1);
        return nodes[x, y];
    }

    [SerializeField] LayerMask obstacleMask;
    private float obstacleHeight;

    bool IsWalkable(Vector3 center)
    {
        Vector3 halfExtents = new(
            nodeSize * .45f,
            obstacleHeight * .5f,
            nodeSize * .45f);

        return !Physics.CheckBox(
            center, halfExtents,
            Quaternion.identity,
            obstacleMask);
    }


}
