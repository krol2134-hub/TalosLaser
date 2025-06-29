using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserPathGenerator
    {
        private readonly List<LaserInteractable> _currentChain = new();
        private readonly HashSet<LaserInteractable> _blockedNodes = new();
        private readonly HashSet<LaserSegment> _blockedSegments = new();
        private readonly Dictionary<Generator, List<List<LaserSegment>>> _allPaths = new();
        private readonly Dictionary<Generator, List<List<LaserSegment>>> _allGeneratorPaths = new();
        
        private readonly LayerMask _layerMaskObstacle;

        public LaserPathGenerator(LayerMask layerMaskObstacle)
        {
            _layerMaskObstacle = layerMaskObstacle;
        }

        public Dictionary<Generator, List<List<LaserSegment>>> FindAllPaths(Generator[] generators)
        {
            _blockedNodes.Clear();
            _blockedSegments.Clear();
            _allPaths.Clear();

            foreach (var generator in generators)
            {
                var pathToEndPoint = new List<List<LaserSegment>>();
                var allPaths = new List<List<LaserSegment>>();
                
                FindPathSegmentsFromGeneratorToEnd(generator, generator, pathToEndPoint, allPaths);
                
                _allGeneratorPaths[generator] = pathToEndPoint;
                _allPaths[generator] = allPaths;
            }

            ProcessPathBlockers(_allGeneratorPaths);
            ProcessFreeEndPointPathBranches(_allGeneratorPaths);
            
            return _allGeneratorPaths;
        }

        private void FindPathSegmentsFromGeneratorToEnd(Generator startGenerator, LaserInteractable root,
            List<List<LaserSegment>> allPathSegments, List<List<LaserSegment>> allPaths)
        {
            var queue = new Queue<List<LaserInteractable>>();
            queue.Enqueue(new List<LaserInteractable> { root });

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var currentConnection = path.Last();

                if (path.Count > 1)
                {
                    SavePath(startGenerator, path, allPaths);
                }

                var isEnd = currentConnection is Receiver || (currentConnection is Generator generator && generator != startGenerator);
                if (isEnd)
                {
                    if (path.Count > 1)
                    {
                        SavePath(startGenerator, path, allPathSegments);
                    }
                    
                    continue;
                }

                //TODO Merge connection in Interactable
                foreach (var next in currentConnection.InputConnections)
                {
                    if (!path.Contains(next))
                    {
                        var newPath = new List<LaserInteractable>(path) { next };
                        queue.Enqueue(newPath);
                    }
                }

                foreach (var next in currentConnection.OutputConnections)
                {
                    if (!path.Contains(next))
                    {
                        var newPath = new List<LaserInteractable>(path) { next };
                        queue.Enqueue(newPath);
                    }
                }
            }
        }

        private static void SavePath(Generator startGenerator, List<LaserInteractable> path, List<List<LaserSegment>> pathsToSave)
        {
            var segments = new List<LaserSegment>();
            for (var i = 0; i < path.Count - 1; i++)
            {
                segments.Add(new LaserSegment(path[i], path[i + 1], ConnectionState.Free, startGenerator.LaserColor));
            }
                        
            pathsToSave.Add(segments);
        }

        private void ProcessPathBlockers(Dictionary<Generator, List<List<LaserSegment>>> allGeneratorPaths)
        {
            if (_allGeneratorPaths.Count == 0)
            {
                return;
            }
            
            Dictionary<Generator, List<List<LaserSegment>>> allGeneratorPathsTemp = new(allGeneratorPaths);
            foreach (var generator1 in allGeneratorPathsTemp.Keys)
            {
                foreach (var generator2 in allGeneratorPathsTemp.Keys)
                {
                    if (generator1 == generator2 || generator1.LaserColor == generator2.LaserColor)
                    {
                        continue;
                    }

                    foreach (var path1 in allGeneratorPathsTemp[generator1])
                    {
                        foreach (var path2 in allGeneratorPathsTemp[generator2])
                        {
                            if (path1.Count != path2.Count)
                            {
                                continue;
                            }
                            
                            var isValidChain = BuildChain(path1, path2);

                            var isValidPath = isValidChain && _currentChain is { Count: > 1 };
                            if (!isValidPath)
                            {
                                continue;
                            }

                            LaserInteractable blockedNode = null;
                            LaserSegment blockedSegment = null;

                            FindBlockers(_currentChain, ref blockedNode, ref blockedSegment);

                            UpdatePathWithBlockers(generator1, path1, _currentChain, blockedNode, blockedSegment);
                            UpdatePathWithBlockers(generator2, path2, _currentChain, blockedNode, blockedSegment);
                        }
                    }
                }
            }
        }

        private void ProcessFreeEndPointPathBranches(Dictionary<Generator, List<List<LaserSegment>>> allPathByGenerators)
        {
            var prioritySegments = new HashSet<(LaserInteractable, LaserInteractable)>();

            foreach (var generatorPaths in allPathByGenerators.Values)
            {
                foreach (var path in generatorPaths)
                {
                    foreach (var segment in path)
                    {
                        prioritySegments.Add((segment.Start, segment.End));
                    }
                }
            }

            foreach (var generator in _allPaths.Keys)
            {
                var usedSegments = new HashSet<(LaserInteractable, LaserInteractable)>();
                var nonPriorityPaths = new List<List<LaserSegment>>();

                foreach (var path in _allPaths[generator])
                {
                    var isPathBlocked = CheckPathBlocked(path);
                    if (isPathBlocked)
                    {
                        continue;
                    }

                    foreach (var segment in path)
                    {
                        var orderedSegment = (segment.Start, segment.End);
                        if (prioritySegments.Contains(orderedSegment) || usedSegments.Contains(orderedSegment))
                        {
                            continue;
                        }
                        
                        nonPriorityPaths.Add(new List<LaserSegment> { segment });
                        usedSegments.Add(orderedSegment);
                    }
                }

                if (nonPriorityPaths.Any())
                {
                    allPathByGenerators[generator].AddRange(nonPriorityPaths);
                }
            }
        }

        private bool CheckPathBlocked(List<LaserSegment> path)
        {
            foreach (var segment in path)
            {
                if (_blockedNodes.Contains(segment.Start) || _blockedNodes.Contains(segment.End))
                {
                    return true;
                }
                        
                foreach (var currentSegment in _blockedSegments)
                {
                    if (currentSegment.CheckMatchingBySides(segment.Start, segment.End))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool BuildChain(List<LaserSegment> path1, List<LaserSegment> path2)
        {
            _currentChain.Clear();
            
            var isSamePath = true;

            //TODO Generate chain with path generation before
            for (var i = 0; i < path1.Count; i++)
            {
                var segment = path1[i];
                var otherSegment = path2[path2.Count - 1 - i];

                if (segment.Start != otherSegment.End || segment.End != otherSegment.Start)
                {
                    isSamePath = false;
                    break;
                }

                _currentChain.Add(segment.Start);

                if (i == path1.Count - 1)
                {
                    _currentChain.Add(segment.End);
                }
            }

            return isSamePath;
        }

        private void FindBlockers(List<LaserInteractable> chain, ref LaserInteractable blockedNode, ref LaserSegment blockedSegment)
        {
            var midIndex = chain.Count / 2;

            var existingBlockedNode = chain.FirstOrDefault(node => _blockedNodes.Contains(node));
            if (existingBlockedNode != null)
            {
                blockedNode = existingBlockedNode;
            }
            else
            {
                for (var i = 0; i < chain.Count - 1; i++)
                {
                    var isFoundBrokenSegment = false;

                    var startChain = chain[i];
                    var endChain = chain[i + 1];
                    foreach (var currentSegment in _blockedSegments)
                    {
                        if (currentSegment.CheckMatchingBySides(startChain, endChain))
                        {
                            blockedSegment = new LaserSegment(startChain, endChain, ConnectionState.Conflict);
                            isFoundBrokenSegment = true;
                            break;
                        }
                    }
                    
                    if (isFoundBrokenSegment)
                    {
                        break;
                    }
                }

                if (blockedSegment != null)
                {
                    return;
                }
                
                var start = chain[midIndex - 1];
                var end = chain[midIndex];
                    
                if (chain.Count % 2 != 0)
                {
                    blockedNode = end;
                    _blockedNodes.Add(blockedNode);
                }
                else
                {
                    blockedSegment = new LaserSegment(start, end, ConnectionState.Conflict);
                    _blockedSegments.Add(blockedSegment);
                }
            }
        }

        private void UpdatePathWithBlockers(Generator gen1, List<LaserSegment> path1, 
            List<LaserInteractable> chain, LaserInteractable blockedNode, LaserSegment blockedSegment)
        {
            var midIndex = chain.Count / 2;
            
            var adjustedPath1 = AdjustPath(path1, chain, blockedNode, blockedSegment, midIndex, gen1.LaserColor);

            var updatedPaths = new List<List<LaserSegment>>();
            foreach (var p in _allGeneratorPaths[gen1])
            {
                updatedPaths.Add(p == path1 ? adjustedPath1 : p);
            }

            _allGeneratorPaths[gen1] = updatedPaths;
        }

        private List<LaserSegment> AdjustPath(List<LaserSegment> path, List<LaserInteractable> chain,
            LaserInteractable blockedNode, LaserSegment blockedSegment, int midIndex, ColorType color)
        {
            var adjustedPath = new List<LaserSegment>();

            foreach (var segment in path)
            {
                var isBlockedConnection = blockedNode != null && (segment.Start == blockedNode || segment.End == blockedNode);
                
                var isBlockedSegment = false;
                if (blockedSegment != null)
                {
                    isBlockedSegment =  segment.CheckMatchingBySides(blockedSegment.Start, blockedSegment.End);
                }

                var isConflictSegment = false;
                var isMidSegment = chain.Count % 2 == 0 && chain.Count > 0;
                if (isMidSegment)
                {
                    var nodeBeforeMid = chain[midIndex - 1];
                    var nodeAtMid = chain[midIndex];
                    isConflictSegment = segment.CheckMatchingBySides(nodeBeforeMid, nodeAtMid);
                }

                if (isBlockedConnection)
                {
                    adjustedPath.Add(new LaserSegment(segment.Start, segment.End, ConnectionState.Blocker));
                    break;
                }

                if (isBlockedSegment || isConflictSegment)
                {
                    adjustedPath.Add(new LaserSegment(segment.Start, segment.End, ConnectionState.Conflict));
                    break;
                }

                adjustedPath.Add(new LaserSegment(segment.Start, segment.End, ConnectionState.Free, color));
            }

            return adjustedPath;
        }
    }
}