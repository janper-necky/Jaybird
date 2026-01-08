using System.Collections.Generic;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Jaybird;

// GRAPH DATA STRUCTURE (GH_Graph)
// Grasshopper-compatible graph implementation using adjacency list representation.
//
// GRAPH THEORY FUNDAMENTALS:
// A graph is a mathematical structure used to model relationships between objects.
// It consists of:
// - NODES (also called vertices): The objects or points in the graph
// - EDGES: The connections or relationships between nodes
//
// DIRECTED GRAPH:
// This implementation represents a DIRECTED GRAPH where each edge has a direction:
// an edge goes FROM one node TO another node (not necessarily bidirectional).
//
// UNIDIRECTIONAL EDGES:
// Each edge is UNIDIRECTIONAL (one-way), meaning it only goes in one direction.
// If you want a bidirectional (two-way) connection between nodes A and B, you need TWO edges:
// - One edge from A to B
// - One edge from B to A
//
// This design allows modeling both:
// - Directed relationships (one-way streets, directed flows)
// - Undirected relationships (two-way streets) by adding edges in both directions
//
// REPRESENTATION - ADJACENCY LIST:
// We use an ADJACENCY LIST representation, which stores for each node a list of its
// outgoing edges. This is efficient for sparse graphs (graphs with relatively few edges).
//
// Advantages:
// - Space efficient: O(V + E) where V = nodes, E = edges
// - Fast neighbor lookup: O(degree of node)
// - Efficient for pathfinding algorithms
//
// NODE STRUCTURE:
// Nodes are identified by their index (0, 1, 2, ...) in the adjacency list.
// Node positions are NOT stored - they are derived from edge geometries when needed.
// Each edge's Polyline geometry contains the full path information.
//
// EDGE STRUCTURE:
// Each edge connects two nodes and stores:
// - ToNodeIdx: the index of the destination node (where the edge points to)
// - Length: the distance/weight/cost of traversing this edge
// - Geometry: Polyline representing the actual path geometry
//
// DATA STORAGE:
// _nodeEdges[i] = a HashSet of all outgoing edges from node i
//
// EXAMPLE:
// Node 0 connects to node 1:
// _nodeEdges[0] = { Edge { ToNodeIdx = 1, Length = 1.0, Geometry = Polyline([pt0, pt1]) } }
// _nodeEdges[1] = { }  (no outgoing edges from node 1)
//
// For a bidirectional connection:
// _nodeEdges[0] = { Edge { ToNodeIdx = 1, Length = 1.0, Geometry = Polyline([pt0, pt1]) } }
// _nodeEdges[1] = { Edge { ToNodeIdx = 0, Length = 1.0, Geometry = Polyline([pt1, pt0]) } }
//
// VALIDATION:
// The graph tracks validity state (_isValid, _invalidReason) to handle:
// - Null input data
// - Invalid edge references
// - Serialization errors
//
// SERIALIZATION:
// Implements Write() and Read() methods for saving/loading graphs in Grasshopper files.
// Uses Grasshopper's GH_IWriter/GH_IReader for persistence.
//
// GRASSHOPPER INTEGRATION:
// Implements IGH_Goo interface to work as a native Grasshopper data type:
// - Can be passed between components via wires
// - Supports duplication, validation, and type conversion
// - Displays summary information in Grasshopper UI

public class GH_Graph : IGH_Goo
{
    private int _nodeCount;
    private int _edgeCount;

    private HashSet<Edge>[] _nodeEdges;

    private bool _isValid;
    private string _invalidReason;

    public GH_Graph()
    {
        _nodeEdges = [];
        _isValid = false;
        _invalidReason = "The graph is uninitialized";
    }

    public GH_Graph(IReadOnlyList<HashSet<Edge>> nodeEdges)
    {
        if (nodeEdges == null)
        {
            _nodeCount = 0;
            _edgeCount = 0;
            _nodeEdges = [];
            _isValid = false;
            _invalidReason = "Edges collection is null";
            return;
        }

        _nodeCount = nodeEdges.Count;

        _nodeEdges = new HashSet<Edge>[nodeEdges.Count];
        for (int i = 0; i < nodeEdges.Count; i++)
        {
            if (nodeEdges[i] == null)
            {
                _nodeCount = 0;
                _edgeCount = 0;
                _nodeEdges = [];
                _isValid = false;
                _invalidReason = $"Edge set at node {i} is null";
                return;
            }
            _nodeEdges[i] = new HashSet<Edge>(nodeEdges[i]);
        }

        _edgeCount = 0;
        foreach (var edges in nodeEdges)
        {
            _edgeCount += edges.Count;
        }

        _isValid = true;
        _invalidReason = string.Empty;
    }

    public bool IsValid => _isValid;

    public string TypeName => "Graph";

    public string TypeDescription => "A graph structure suitable for path finding";

    public string IsValidWhyNot => _isValid ? string.Empty : _invalidReason;

    public HashSet<Edge>[] NodeEdges => _nodeEdges;

    public bool CastFrom(object source)
    {
        return false;
    }

    public bool CastTo<T>(out T target)
    {
        target = default!;
        return false;
    }

    public IGH_Goo Duplicate()
    {
        return new GH_Graph(_nodeEdges);
    }

    public IGH_GooProxy? EmitProxy()
    {
        return null;
    }

    public bool Write(GH_IWriter writer)
    {
        writer.SetInt32("NodeCount", _nodeCount);
        writer.SetInt32("EdgeCount", _edgeCount);
        for (int i = 0; i < _nodeEdges.Length; i++)
        {
            var edgeSet = _nodeEdges[i];
            var edges = new Edge[edgeSet.Count];
            edgeSet.CopyTo(edges);
            writer.SetInt32($"Node_{i}_EdgeCount", edges.Length);
            for (int j = 0; j < edges.Length; j++)
            {
                writer.SetInt32($"Node_{i}_Edge_{j}_ToNodeIdx", edges[j].ToNodeIdx);
                writer.SetDouble($"Node_{i}_Edge_{j}_Length", edges[j].Length);

                // Serialize polyline geometry (always present)
                var polyline = edges[j].Geometry;
                writer.SetInt32($"Node_{i}_Edge_{j}_GeometryCount", polyline.Count);
                for (int k = 0; k < polyline.Count; k++)
                {
                    writer.SetPoint3D(
                        $"Node_{i}_Edge_{j}_Geometry_{k}",
                        new GH_Point3D(polyline[k].X, polyline[k].Y, polyline[k].Z)
                    );
                }
            }
        }
        return true;
    }

    public bool Read(GH_IReader reader)
    {
        if (!reader.ItemExists("NodeCount") || !reader.ItemExists("EdgeCount"))
        {
            _nodeCount = 0;
            _edgeCount = 0;
            _nodeEdges = [];
            _isValid = false;
            _invalidReason = "Missing NodeCount or EdgeCount in serialized data";
            return false;
        }

        try
        {
            _nodeCount = reader.GetInt32("NodeCount");
            _edgeCount = reader.GetInt32("EdgeCount");

            if (_nodeCount < 0)
            {
                _nodeCount = 0;
                _edgeCount = 0;
                _nodeEdges = [];
                _isValid = false;
                _invalidReason = "Node count is negative in serialized data";
                return false;
            }

            _nodeEdges = new HashSet<Edge>[_nodeCount];

            for (int i = 0; i < _nodeCount; i++)
            {
                var edges = new HashSet<Edge>();
                var edgeCountKey = $"Node_{i}_EdgeCount";

                if (reader.ItemExists(edgeCountKey))
                {
                    var edgeCount = reader.GetInt32(edgeCountKey);

                    if (edgeCount >= 0)
                    {
                        for (int j = 0; j < edgeCount; j++)
                        {
                            var toNodeKey = $"Node_{i}_Edge_{j}_ToNodeIdx";
                            var lengthKey = $"Node_{i}_Edge_{j}_Length";

                            if (reader.ItemExists(toNodeKey) && reader.ItemExists(lengthKey))
                            {
                                var toNodeIdx = reader.GetInt32(toNodeKey);
                                var length = reader.GetDouble(lengthKey);

                                if (toNodeIdx >= 0 && toNodeIdx < _nodeCount && length >= 0)
                                {
                                    // Deserialize polyline geometry
                                    Polyline geometry;
                                    var geometryCountKey = $"Node_{i}_Edge_{j}_GeometryCount";
                                    if (reader.ItemExists(geometryCountKey))
                                    {
                                        var geometryCount = reader.GetInt32(geometryCountKey);
                                        var points = new List<Point3d>();
                                        for (int k = 0; k < geometryCount; k++)
                                        {
                                            var geometryKey = $"Node_{i}_Edge_{j}_Geometry_{k}";
                                            if (reader.ItemExists(geometryKey))
                                            {
                                                var ghPoint = reader.GetPoint3D(geometryKey);
                                                points.Add(
                                                    new Point3d(ghPoint.x, ghPoint.y, ghPoint.z)
                                                );
                                            }
                                        }
                                        geometry = new Polyline(points);
                                    }
                                    else
                                    {
                                        // Cannot create edge without geometry
                                        continue;
                                    }

                                    edges.Add(
                                        new Edge
                                        {
                                            ToNodeIdx = toNodeIdx,
                                            Length = length,
                                            Geometry = geometry,
                                        }
                                    );
                                }
                            }
                        }
                    }
                }
                _nodeEdges[i] = edges;
            }
            _isValid = true;
            _invalidReason = string.Empty;
            return true;
        }
        catch
        {
            _nodeCount = 0;
            _edgeCount = 0;
            _nodeEdges = [];
            _isValid = false;
            _invalidReason = "Exception occurred while reading serialized data";
            return false;
        }
    }

    public object ScriptVariable()
    {
        return this;
    }

    public override string ToString()
    {
        return $"Graph with {_nodeCount} nodes and {_edgeCount} edges";
    }
}
