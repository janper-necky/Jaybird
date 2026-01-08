using System.Collections.Generic;

namespace Jaybird;

public static class GraphUtilities
{
    /// <summary>
    /// Finds all connected components (islands) in a directed graph.
    /// Uses BFS to identify weakly connected components (treating edges as undirected).
    /// A connected component is a maximal subgraph where any two vertices have a path
    /// between them (i.e., a disconnected "island" within the graph).
    /// </summary>
    /// <param name="nodeEdges">Adjacency list representing the graph's edges</param>
    /// <returns>
    /// List of connected components, where each component is a list of node indices
    /// </returns>
    public static List<List<int>> FindConnectedComponents(HashSet<Edge>[] nodeEdges)
    {
        var incomingEdges = new List<int>[nodeEdges.Length];
        for (int i = 0; i < nodeEdges.Length; i++)
        {
            incomingEdges[i] = new List<int>();
        }

        for (int i = 0; i < nodeEdges.Length; i++)
        {
            foreach (var edge in nodeEdges[i])
            {
                incomingEdges[edge.ToNodeIdx].Add(i);
            }
        }

        var visited = new bool[nodeEdges.Length];
        var components = new List<List<int>>();

        // Main loop: iterate through all nodes to find unvisited starting points
        // Each unvisited node represents a potential new connected component
        for (int i = 0; i < nodeEdges.Length; i++)
        {
            if (!visited[i])
            {
                var component = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;
                component.Add(i);

                // BFS traversal: explore all nodes reachable from the current component
                // Continues until no more connected nodes are found
                while (queue.TryDequeue(out var currentNodeIdx))
                {
                    // First nested loop: follow outgoing edges
                    // Visit all nodes that the current node points to
                    foreach (var edge in nodeEdges[currentNodeIdx])
                    {
                        if (!visited[edge.ToNodeIdx])
                        {
                            visited[edge.ToNodeIdx] = true;
                            queue.Enqueue(edge.ToNodeIdx);
                            component.Add(edge.ToNodeIdx);
                        }
                    }

                    // Second nested loop: follow incoming edges
                    // Visit all nodes that point to the current node
                    // This ensures bidirectional connectivity for component detection
                    foreach (var incomingNodeIdx in incomingEdges[currentNodeIdx])
                    {
                        if (!visited[incomingNodeIdx])
                        {
                            visited[incomingNodeIdx] = true;
                            queue.Enqueue(incomingNodeIdx);
                            component.Add(incomingNodeIdx);
                        }
                    }
                }

                components.Add(component);
            }
        }

        return components;
    }
}
