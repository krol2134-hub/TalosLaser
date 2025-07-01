using System.Collections.Generic;
using System.Linq;
using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.PathGenerator
{
    public class LaserPathGenerator
    {
        private readonly LayerMask _layerMaskObstacle;
        private readonly PathInterceptionGenerator _pathInterceptionGenerator;
        
        private readonly Dictionary<(Vector3, Vector3), CollisionInfo> _collisionCache = new();

        private readonly HashSet<LaserInteractable> _blockedNodes = new();
        private readonly HashSet<LaserSegment> _blockedSegments = new();

        public LaserPathGenerator(LayerMask layerMaskObstacle)
        {
            _layerMaskObstacle = layerMaskObstacle;
            _pathInterceptionGenerator = new PathInterceptionGenerator();
        }
        
        public Dictionary<Generator, List<LaserPath>> FindAllPaths(Generator[] generators, out HashSet<LaserInteractable> blockedInteractables, out HashSet<LaserSegment> blockedSegments)
        {
            _collisionCache.Clear();
            _blockedNodes.Clear();
            _blockedSegments.Clear();
            
            var allPathsByGenerator = GenerateAllPaths(generators);

            FilterPath(allPathsByGenerator);
            ProcessPathInterception(allPathsByGenerator);
            FindPathConflicts(allPathsByGenerator);

            blockedInteractables = _blockedNodes;
            blockedSegments = _blockedSegments;
            
            return allPathsByGenerator;
        }

        private Dictionary<Generator, List<LaserPath>> GenerateAllPaths(Generator[] generators)
        {
            var allPathsByGenerator = new Dictionary<Generator, List<LaserPath>>();

            foreach (var generator in generators)
            {
                var paths = new List<LaserPath>();
                FindPathSegmentsFromGenerator(generator, paths);
                allPathsByGenerator[generator] = paths;
            }

            return allPathsByGenerator;
        }

        private void FindPathSegmentsFromGenerator(Generator startGenerator, List<LaserPath> paths)
        {
            var queue = new Queue<List<LaserInteractable>>();
            queue.Enqueue(new List<LaserInteractable> { startGenerator });

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var currentNode = path.Last();

                var laserPath = BuildSegmentPath(startGenerator, path);

                if (laserPath.Segments.Count > 0)
                {
                    paths.Add(laserPath);
                }

                var isIncompletePath = laserPath.Type != PathType.Complete && laserPath.Type != PathType.Blocked;
                if (!isIncompletePath)
                {
                    continue;
                }
                
                foreach (var next in currentNode.InputConnections.Concat(currentNode.OutputConnections))
                {
                    if (!path.Contains(next))
                    {
                        var newPath = new List<LaserInteractable>(path) { next };
                        queue.Enqueue(newPath);
                    }
                }
            }
        }

        private LaserPath BuildSegmentPath(Generator startGenerator, List<LaserInteractable> path)
        {
            List<LaserSegment> segments = new();
            
            var color = startGenerator.LaserColor;
            
            var pathType = PathType.Incomplete;
            var blockReason = BlockReason.None;

            for (var i = 0; i < path.Count - 1; i++)
            {
                var startConnection = path[i];
                var endConnection = path[i + 1];
                var startPosition = startConnection.LaserPoint;
                var endPosition = endConnection.LaserPoint;

                if (CheckPhysicCollision(startPosition, endPosition, out var hitInfo))
                {
                    blockReason = BlockReason.PhysicalObstacle;
                    pathType = PathType.Blocked;
                    
                    segments.Add(new LaserSegment(startConnection, endConnection, ConnectionState.PhysicalBlocker, color,
                        hitInfo.HitPoint, hitInfo.Distance));
                    
                    break;
                }

                segments.Add(new LaserSegment(startConnection, endConnection, ConnectionState.Free, color));
            }

            if (pathType != PathType.Blocked)
            {
                var lastNode = path.Last();
                var isComplete = lastNode is Receiver || (lastNode is Generator generator && generator != startGenerator);
                pathType = isComplete ? PathType.Complete : PathType.Incomplete;
            }

            return new LaserPath(segments, pathType, startGenerator, blockReason);
        }

        private bool CheckPhysicCollision(Vector3 start, Vector3 end, out CollisionInfo collisionInfo)
        {
            var collisionPosition = (start, end);
            if (_collisionCache.TryGetValue(collisionPosition, out var cachedInfo))
            {
                collisionInfo = cachedInfo;
                return cachedInfo.IsHit;
            }

            var direction = (end - start).normalized;
            var maxDistance = Vector3.Distance(start, end);

            if (Physics.Raycast(start, direction, out var hit, maxDistance, _layerMaskObstacle))
            {
                var distanceToHit = Vector3.Distance(start, hit.point);
                collisionInfo = new CollisionInfo(true, null, hit.point, distanceToHit);
                _collisionCache[collisionPosition] = collisionInfo;
                
                return true;
            }

            collisionInfo = default;
            return false;
        }

        private static void FilterPath(Dictionary<Generator, List<LaserPath>> allPaths)
        {
            foreach (var key in allPaths.Keys.ToList())
            {
                var paths = allPaths[key];
                var completePaths = new List<LaserPath>();

                foreach (var path in paths)
                {
                    if (path.Type == PathType.Complete)
                    {
                        completePaths.Add(path);
                    }
                }

                var filteredPaths = new List<LaserPath>();
                foreach (var path in paths)
                {
                    if (path.Type != PathType.Incomplete)
                    {
                        filteredPaths.Add(path);
                        continue;
                    }

                    var isChainFound = false;
                    foreach (var completePath in completePaths)
                    {
                        if (CheckSamePathChainIn(completePath.Segments, path.Segments))
                        {
                            isChainFound = true;
                            break;
                        }
                    }

                    if (!isChainFound)
                    {
                        filteredPaths.Add(path);
                    }
                }

                filteredPaths.Sort((a, b) => a.Type.CompareTo(b.Type));
                allPaths[key] = filteredPaths;
            }
        }

        private static bool CheckSamePathChainIn(List<LaserSegment> currentPath, List<LaserSegment> otherPath)
        {
            if (otherPath.Count == 0)
            {
                return true;
            }

            if (currentPath.Count < otherPath.Count)
            {
                return false;
            }

            for (var i = 0; i <= currentPath.Count - otherPath.Count; i++)
            {
                var isMatched = true;

                for (var j = 0; j < otherPath.Count; j++)
                {
                    var currentSegment = currentPath[i + j];
                    var otherSegment = otherPath[j];
                    
                    if (!currentSegment.CheckMatchingBySides(otherSegment))
                    {
                        isMatched = false;
                        break;
                    }
                }

                if (isMatched)
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessPathInterception(Dictionary<Generator, List<LaserPath>> allPaths)
        {
            var interceptionsBySegments = _pathInterceptionGenerator.ProcessPathInterceptions(allPaths);
            foreach (var ((start, end), interceptionInfo) in interceptionsBySegments)
            {
                foreach (var (_, paths) in allPaths)
                {
                    foreach (var laserPath in paths)
                    {
                        foreach (var otherSegment in laserPath.Segments)
                        {
                            if (otherSegment.CheckMatchingBySides(start, end))
                            {
                                laserPath.Type = PathType.Blocked;
                                laserPath.BlockReason = BlockReason.LineIntersection;
                                otherSegment.UpdateBlockState(ConnectionState.LineInception, interceptionInfo.Point, interceptionInfo.Distance);
                            }
                        }
                    }
                }   
            }
        }

        private void FindPathConflicts(Dictionary<Generator, List<LaserPath>> allGeneratorPaths)
        {
            var completePaths = GetPathForConflicts(allGeneratorPaths);

            foreach (var generatorPaths in completePaths)
            {
                var paths = generatorPaths.Value;

                foreach (var path in paths)
                {
                    var segments = path.Segments;

                    if (segments.Count == 0)
                    {
                        continue;
                    }

                    var midIndex = (segments.Count - 1) / 2;
                    var hasBlockedSegment = false;
                    var hasBlockedNode = false;

                    foreach (var segment in segments)
                    {
                        if (IsSegmentBlocked(segment))
                        {
                            hasBlockedSegment = true;
                            break;
                        }

                        if (_blockedNodes.Contains(segment.Start) || _blockedNodes.Contains(segment.End))
                        {
                            hasBlockedNode = true;
                            break;
                        }
                    }

                    if (segments.Count % 2 != 0)
                    {
                        if (hasBlockedSegment || hasBlockedNode)
                        {
                            continue;
                        }

                        var midSegment = segments[midIndex];
                        path.UpdateBlockStatus(BlockReason.LogicalConflict);
                        _blockedSegments.Add(midSegment);
                    }
                    else
                    {
                        var midNode = segments[midIndex].End;

                        if (_blockedNodes.Contains(midNode) || hasBlockedSegment || hasBlockedNode)
                        {
                            continue;
                        }

                        path.UpdateBlockStatus(BlockReason.LogicalBlock);
                        _blockedNodes.Add(midNode);
                    }
                }
            }
        }

        private static Dictionary<Generator, List<LaserPath>> GetPathForConflicts(Dictionary<Generator, List<LaserPath>> allGeneratorPaths)
        {
            var allChains = new Dictionary<Generator, List<LaserPath>>();

            foreach (var (generator, paths) in allGeneratorPaths)
            {
                foreach (var path in paths)
                {
                    if (path.Type is PathType.Blocked or PathType.Incomplete)
                    {
                        continue;
                    }
                    
                    if (path.Segments.Count == 0)
                    {
                        continue;
                    }
                    
                    if (!allChains.ContainsKey(generator))
                    {
                        allChains[generator] = new List<LaserPath>();
                    }
                    
                    allChains[generator].Add(path);
                }
            }

            return allChains;
        }

        private bool IsSegmentBlocked(LaserSegment segment)
        {
            return _blockedSegments.Any(s => s.CheckMatchingBySides(segment));
        }
    }
}