using System.Collections.Generic;
using UnityEngine;


public class AIDijkstra : MonoBehaviour
{
    private class Node
    {
        public int x, y;
        public bool walkable;
        public int cost = 1;          // custo de entrar neste nó
        public int g = int.MaxValue;  // menor custo conhecido a partir da origem
        public Node parent;
    }

    [Header("Grade")]
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private Vector2Int size = new Vector2Int(40, 40);
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    [Tooltip("Se ligado, gridOrigin é o CENTRO da grade (recomendado).")]
    [SerializeField] private bool centerGrid = true;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleHeight = 2f;
    [Tooltip("Reconstrói a grade periodicamente (use se os obstáculos mudarem).")]
    [SerializeField] private bool rebuildGrid = false;
    [SerializeField] private float rebuildInterval = 1f;

    [Header("Jogador")]
    [SerializeField] private Transform player;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float updateInterval = 0.3f;
    [SerializeField] private float stopDistance = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool drawGrid = true;
    [SerializeField] private bool drawPath = true;

    private Node[,] nodes;
    private readonly List<Node> openNodes = new List<Node>();
    private readonly List<Node> pathNodes = new List<Node>();
    private float timer;
    private float rebuildTimer;

    private Vector3 BottomLeft =>
        centerGrid
            ? gridOrigin - new Vector3(size.x * nodeSize * 0.5f, 0f, size.y * nodeSize * 0.5f)
            : gridOrigin;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (player == null)
            Debug.LogWarning($"[AIDijkstra] {name}: nenhum jogador encontrado. " +
                             "Arraste o jogador no campo 'Player' ou marque-o com a tag 'Player'.");

        CreateGrid();

        // Espalha o recálculo entre os inimigos para não pesar num único frame
        timer = Random.Range(0f, updateInterval);
    }

    void Update()
    {
        if (player == null || nodes == null) return;

        if (rebuildGrid)
        {
            rebuildTimer -= Time.deltaTime;
            if (rebuildTimer <= 0f)
            {
                rebuildTimer = rebuildInterval;
                CreateGrid();
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = updateInterval;
            CalculatePath(transform.position, player.position);
        }

        if (Vector3.Distance(transform.position, player.position) <= stopDistance)
        {
            FaceTowards(player.position);
            return;
        }

        FollowPath();
    }

    // ---------------- Grade ----------------

    void CreateGrid()
    {
        nodes = new Node[size.x, size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                nodes[x, y] = new Node
                {
                    x = x,
                    y = y,
                    walkable = IsWalkable(NodeToWorld(x, y)),
                    cost = 1
                };
            }
        }
    }

    bool IsWalkable(Vector3 center)
    {
        // Sobe a caixa para não colidir com o chão
        Vector3 boxCenter = center + Vector3.up * (obstacleHeight * 0.5f + 0.05f);
        Vector3 halfExtents = new Vector3(nodeSize * 0.45f, obstacleHeight * 0.5f, nodeSize * 0.45f);
        return !Physics.CheckBox(boxCenter, halfExtents, Quaternion.identity, obstacleMask);
    }

    Vector3 NodeToWorld(int x, int y)
    {
        return BottomLeft + new Vector3((x + 0.5f) * nodeSize, 0f, (y + 0.5f) * nodeSize);
    }

    Node WorldToNode(Vector3 world)
    {
        Vector3 local = world - BottomLeft;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / nodeSize), 0, size.x - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(local.z / nodeSize), 0, size.y - 1);
        return nodes[x, y];
    }

    Node NearestWalkable(Node from)
    {
        if (from.walkable) return from;
        for (int r = 1; r < Mathf.Max(size.x, size.y); r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    int nx = from.x + dx, ny = from.y + dy;
                    if (nx < 0 || ny < 0 || nx >= size.x || ny >= size.y) continue;
                    if (nodes[nx, ny].walkable) return nodes[nx, ny];
                }
            }
        }
        return null;
    }

    // ---------------- Dijkstra ----------------

    void CalculatePath(Vector3 startPosition, Vector3 targetPosition)
    {
        openNodes.Clear();
        pathNodes.Clear();
        ResetNodes();

        Node start = NearestWalkable(WorldToNode(startPosition));
        Node target = NearestWalkable(WorldToNode(targetPosition));
        if (start == null || target == null || start == target) return;

        start.g = 0;
        openNodes.Add(start);

        while (openNodes.Count > 0)
        {
            Node current = GetLowestCostNode();
            openNodes.Remove(current);

            if (current == target)
            {
                CreatePath(target);
                return;
            }

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (!neighbor.walkable) continue;

                int newCost = current.g + neighbor.cost;   // relaxamento da aresta
                if (newCost < neighbor.g)
                {
                    neighbor.g = newCost;
                    neighbor.parent = current;
                    if (!openNodes.Contains(neighbor)) openNodes.Add(neighbor);
                }
            }
        }
    }

    void ResetNodes()
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                nodes[x, y].g = int.MaxValue;
                nodes[x, y].parent = null;
            }
    }

    Node GetLowestCostNode()
    {
        Node best = null;
        foreach (Node node in openNodes)
            if (best == null || node.g < best.g) best = node;
        return best;
    }

    IEnumerable<Node> GetNeighbors(Node node)
    {
        if (node.x > 0) yield return nodes[node.x - 1, node.y];
        if (node.x + 1 < size.x) yield return nodes[node.x + 1, node.y];
        if (node.y > 0) yield return nodes[node.x, node.y - 1];
        if (node.y + 1 < size.y) yield return nodes[node.x, node.y + 1];
    }

    void CreatePath(Node target)
    {
        Node current = target;
        while (current != null)
        {
            pathNodes.Add(current);
            current = current.parent;
        }
        pathNodes.Reverse();
    }

    // ---------------- Movimento ----------------

    void FollowPath()
    {
        if (pathNodes.Count <= 1) return;

        Node nextNode = pathNodes[1];
        Vector3 destination = NodeToWorld(nextNode.x, nextNode.y);
        destination.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position, destination, moveSpeed * Time.deltaTime);

        // Chegou no nó: avança para o próximo sem esperar o recálculo
        if (Vector3.Distance(transform.position, destination) < 0.05f)
            pathNodes.RemoveAt(0);

        FaceTowards(destination);
    }

    void FaceTowards(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction.normalized),
            rotationSpeed * Time.deltaTime);
    }

    // ---------------- Gizmos ----------------

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = centerGrid
            ? gridOrigin
            : gridOrigin + new Vector3(size.x * nodeSize * 0.5f, 0f, size.y * nodeSize * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(size.x * nodeSize, 0.1f, size.y * nodeSize));

        if (nodes == null) return;

        if (drawGrid)
        {
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                {
                    if (nodes[x, y].walkable) continue;
                    Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
                    Gizmos.DrawCube(NodeToWorld(x, y), new Vector3(nodeSize * 0.9f, 0.1f, nodeSize * 0.9f));
                }
        }

        if (drawPath && pathNodes.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 1; i < pathNodes.Count; i++)
                Gizmos.DrawLine(NodeToWorld(pathNodes[i - 1].x, pathNodes[i - 1].y),
                                NodeToWorld(pathNodes[i].x, pathNodes[i].y));
        }
    }
}