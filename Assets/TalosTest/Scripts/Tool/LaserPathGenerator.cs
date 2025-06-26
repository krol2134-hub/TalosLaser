using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TalosTest.Tool
{
    public class LaserPathGenerator
    {
        private readonly LayerMask _layerMaskObstacle;

        private readonly HashSet<LaserInteractable> _checked = new();
        private readonly HashSet<(LaserInteractable, LaserInteractable)> _usedConnections = new();
        private readonly HashSet<LaserInteractable> _usedInteractable = new();
        private readonly HashSet<(LaserInteractable from, LaserInteractable to)> _exploredConnections = new();

        private readonly List<LaserInteractable> _currentPath = new();
        private readonly List<List<LaserInteractable>> _allPaths = new();
        private readonly List<(LaserInteractable, List<LaserInteractable>)> _allBranches = new();

        public LaserPathGenerator(LayerMask layerMaskObstacle)
        {
            _layerMaskObstacle = layerMaskObstacle;
        }

        public List<List<LaserSegment>> FindAllPathSegments(LaserInteractable start)
        {
            _checked.Clear();
            _currentPath.Clear();

            var allPaths = new List<List<LaserSegment>>();
            FindPathSegments(start, allPaths);
            return allPaths;
        }

        private void FindPathSegments(LaserInteractable current, List<List<LaserSegment>> allPathSegments)
        {
            _checked.Add(current);
            _currentPath.Add(current);

            var nextConnections = current.InputConnections
                .Concat(current.OutputConnections)
                .Where(conn => !_checked.Contains(conn))
                .ToList();

            if (current is Receiver || nextConnections.Count == 0)
            {
                if (_currentPath.Count > 1)
                {
                    var segments = new List<LaserSegment>();
                    for (var i = 0; i < _currentPath.Count - 1; i++)
                    {
                        segments.Add(new LaserSegment(_currentPath[i], _currentPath[i + 1]));
                    }

                    allPathSegments.Add(segments);
                }
            }
            else
            {
                foreach (var next in nextConnections)
                {
                    FindPathSegments(next, allPathSegments);
                }
            }

            _checked.Remove(current);
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }

        public List<List<LaserInteractable>> FindAllPathsBetweenGenerators(LaserInteractable start,
            LaserInteractable target)
        {
            _checked.Clear();
            _currentPath.Clear();
            _allPaths.Clear();

            FindPathByDfs(start, target);

            return _allPaths;
        }

        private void FindPathByDfs(LaserInteractable current, LaserInteractable target)
        {
            _checked.Add(current);
            _currentPath.Add(current);

            if (current == target)
            {
                _allPaths.Add(new List<LaserInteractable>(_currentPath));
            }
            else
            {
                foreach (var connection in current.InputConnections.Concat(current.OutputConnections))
                {
                    if (!_checked.Contains(connection))
                    {
                        FindPathByDfs(connection, target);
                    }
                }
            }

            _checked.Remove(current);
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }

        public List<(LaserInteractable from, List<LaserInteractable> branch)> FindOutsidePathBranches(
            List<List<LaserInteractable>> allPaths)
        {
            _usedConnections.Clear();
            _usedInteractable.Clear();
            _exploredConnections.Clear();
            _allBranches.Clear();
            
            foreach (var path in allPaths)
            {
                for (var i = 0; i < path.Count - 1; i++)
                {
                    _usedConnections.Add((path[i], path[i + 1]));
                    _usedConnections.Add((path[i + 1], path[i]));
                }

                foreach (var node in path)
                {
                    _usedInteractable.Add(node);
                }
            }
            
            foreach (var entryPoint in _usedInteractable)
            {
                foreach (var connection in entryPoint.InputConnections.Concat(entryPoint.OutputConnections))
                {
                    var edge = (entryPoint, connection);
                    if (_usedConnections.Contains(edge) || _exploredConnections.Contains(edge))
                    {
                        continue;
                    }
                    
                    var branchNodes = new List<LaserInteractable> { entryPoint };

                    _checked.Clear();
                    ExploreOutsidePathBranches(connection, branchNodes);

                    if (branchNodes.Count > 0)
                    {
                        _allBranches.Add((entryPoint, branchNodes));
                    }

                    _exploredConnections.Add(edge);
                }
            }

            return _allBranches;
        }

        private void ExploreOutsidePathBranches(LaserInteractable current, List<LaserInteractable> result)
        {
            var stack = new Stack<(LaserInteractable node, bool cameFromUsed)>();
            stack.Push((current, false));

            while (stack.Count > 0)
            {
                var (connection, cameFromUsed) = stack.Pop();

                if (_usedInteractable.Contains(connection))
                {
                    continue;
                }

                if (cameFromUsed && _checked.Contains(connection))
                {
                    continue;
                }

                _checked.Add(connection);
                result.Add(connection);

                foreach (var laserInteractable in connection.InputConnections.Concat(connection.OutputConnections))
                {
                    var edge = (node: connection, laserInteractable);
                    if (!_usedConnections.Contains(edge) && !_usedInteractable.Contains(laserInteractable))
                    {
                        var nextCameFromUsed = cameFromUsed || _checked.Contains(connection);
                        stack.Push((laserInteractable, nextCameFromUsed));
                    }
                }
            }
        }


        private void FindPaths(LaserInteractable current)
        {
            _checked.Add(current);
            _currentPath.Add(current);

            foreach (var connection in current.InputConnections.Concat(current.OutputConnections))
            {
                if (_checked.Contains(connection))
                {
                    continue;
                }

                FindPathWithoutFinishTarget(connection);
            }

            var isOpenPath =  _currentPath.Count > 0;
            if (isOpenPath)
            {
                _allPaths.Add(new List<LaserInteractable>(_currentPath));
            }

            _checked.Remove(current);
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }
        
        
        
        public List<List<LaserInteractable>> FindAllOpenPaths(LaserInteractable start)
        {
            _checked.Clear();
            _currentPath.Clear();
            _allPaths.Clear();

            FindPathWithoutFinishTarget(start);

            return _allPaths;
        }

        private void FindPathWithoutFinishTarget(LaserInteractable current)
        {
            _checked.Add(current);
            _currentPath.Add(current);

            var hasUncheckedConnections = false;
            foreach (var connection in current.InputConnections.Concat(current.OutputConnections))
            {
                if (_checked.Contains(connection))
                {
                    continue;
                }

                hasUncheckedConnections = true;
                FindPathWithoutFinishTarget(connection);
            }

            var isOpenPath = !hasUncheckedConnections && _currentPath.Count > 1;
            if (isOpenPath)
            {
                _allPaths.Add(new List<LaserInteractable>(_currentPath));
            }

            _checked.Remove(current);
            _currentPath.RemoveAt(_currentPath.Count - 1);
        }

        public bool IsLaserBlocked(Vector3 start, Vector3 end, out Vector3 hitPoint)
        {
            if (Physics.Linecast(start, end, out var hit, _layerMaskObstacle))
            {
                hitPoint = hit.point;
                return true;
            }

            hitPoint = Vector3.zero;
            return false;
        }
    }
}