using System.Collections.Generic;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Jaybird;

public class GH_Graph : IGH_Goo
{
    private int _nodeCount;
    private int _edgeCount;

    private Point3d[] _nodePoints;
    private HashSet<Edge>[] _nodeEdges;

    private bool _isValid;
    private string _invalidReason;

    public GH_Graph()
    {
        _nodePoints = [];
        _nodeEdges = [];
        _isValid = false;
        _invalidReason = "The graph is uninitialized";
    }

    public GH_Graph(IReadOnlyList<Point3d> nodePoints, IReadOnlyList<HashSet<Edge>> nodeEdges)
    {
        if (nodePoints == null || nodeEdges == null)
        {
            _nodeCount = 0;
            _edgeCount = 0;
            _nodePoints = [];
            _nodeEdges = [];
            _isValid = false;
            _invalidReason = "Node points or edges collection is null";
            return;
        }

        if (nodePoints.Count != nodeEdges.Count)
        {
            _nodeCount = 0;
            _edgeCount = 0;
            _nodePoints = [];
            _nodeEdges = [];
            _isValid = false;
            _invalidReason = "Node points and edges arrays have mismatched lengths";
            return;
        }

        _nodeCount = nodePoints.Count;

        _nodePoints = new Point3d[_nodeCount];
        for (int i = 0; i < _nodeCount; i++)
        {
            _nodePoints[i] = nodePoints[i];
        }

        _nodeEdges = new HashSet<Edge>[nodeEdges.Count];
        for (int i = 0; i < nodeEdges.Count; i++)
        {
            if (nodeEdges[i] == null)
            {
                _nodeCount = 0;
                _edgeCount = 0;
                _nodePoints = [];
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

    public Point3d[] NodePoints => _nodePoints;

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
        return new GH_Graph(_nodePoints, _nodeEdges);
    }

    public IGH_GooProxy? EmitProxy()
    {
        return null;
    }

    public bool Write(GH_IWriter writer)
    {
        writer.SetInt32("NodeCount", _nodeCount);
        writer.SetInt32("EdgeCount", _edgeCount);
        for (int i = 0; i < _nodePoints.Length; i++)
        {
            writer.SetPoint3D(
                $"NodePoint_{i}",
                new GH_Point3D(_nodePoints[i].X, _nodePoints[i].Y, _nodePoints[i].Z)
            );
        }
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
            _nodePoints = [];
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
                _nodePoints = [];
                _nodeEdges = [];
                _isValid = false;
                _invalidReason = "Node count is negative in serialized data";
                return false;
            }

            _nodePoints = new Point3d[_nodeCount];
            _nodeEdges = new HashSet<Edge>[_nodeCount];

            for (int i = 0; i < _nodeCount; i++)
            {
                var pointKey = $"NodePoint_{i}";
                if (reader.ItemExists(pointKey))
                {
                    var ghPoint = reader.GetPoint3D(pointKey);
                    _nodePoints[i] = new Point3d(ghPoint.x, ghPoint.y, ghPoint.z);
                }
                else
                {
                    _nodePoints[i] = Point3d.Origin;
                }
            }

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
                                    edges.Add(new Edge { ToNodeIdx = toNodeIdx, Length = length });
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
            _nodePoints = [];
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
