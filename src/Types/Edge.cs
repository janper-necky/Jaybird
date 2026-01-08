using System;
using System.Diagnostics.CodeAnalysis;
using Rhino.Geometry;

namespace Jaybird;

// EDGE STRUCTURE
// Represents a UNIDIRECTIONAL (one-way) connection between two nodes in a graph.
//
// PURPOSE:
// Stores the information needed to represent a directed edge:
// - Where it goes (destination node index)
// - How much it costs to traverse (edge weight/length)
// - The actual geometry of the path (polyline)
//
// UNIDIRECTIONAL BEHAVIOR:
// This edge only represents movement FROM the source node TO the destination node.
// The source node is implicit (determined by which node's adjacency list contains this edge).
// It does NOT imply you can travel back in the opposite direction.
//
// For bidirectional connections, you need TWO separate edges:
// - Edge in node A's list: A → B
// - Edge in node B's list: B → A
//
// FIELDS:
//
// ToNodeIdx (int):
// - The index of the destination node (where this edge points to)
// - Must be a valid index in the graph's node array
// - Example: If ToNodeIdx = 5, this edge goes to node 5
//
// Length (double):
// - The weight/cost/distance of traversing this edge
// - Used by pathfinding algorithms to calculate shortest paths
// - Must be >= 0 (negative weights can break Dijkstra's algorithm)
// - Typically matches the polyline's total length
//
// Geometry (Polyline):
// - The actual path geometry for this edge (always present)
// - For simple point-to-point edges: 2-point polyline [start, end]
// - For merged road segments: multi-point polyline preserving the full path
// - Contains all spatial information - node positions are derived from this
//
// STORAGE:
// Edges are stored in HashSet<Edge>[] in the graph, where:
// - Array index = source node index
// - HashSet contents = all outgoing edges from that node
//
// This allows HashSet to detect and prevent duplicate edges between the same nodes.
// Important: The source node is not part of equality because it's implicit from
// the adjacency list structure.

public struct Edge
{
    public int ToNodeIdx;
    public double Length;
    public Polyline Geometry;

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        if (!base.Equals(obj))
        {
            return false;
        }

        var other = (Edge)obj;

        return other.ToNodeIdx == ToNodeIdx && other.Length == Length;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(ToNodeIdx, Length);
    }
}
