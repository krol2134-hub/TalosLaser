using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

namespace TalosTest.Tool
{
    public class LaserControllerNew : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private LaserVfxController laserVFXController;

#if UNITY_EDITOR
        [SerializeField] private int pathCountDebug;
        [SerializeField, Range(-1, 10)] private int maxPathCountDebug = -1;
        [SerializeField, Range(-1, 10)] private int onlyPathTargetDebug = -1;
#endif

        private Generator[] _generators;
        private Receiver[] _receivers;
        private Connector[] _connectors;

        private readonly HashSet<LaserInteractable> _previousFrameInteractables = new();
        private readonly HashSet<LaserInteractable> _currentFrameInteractables = new();
        private readonly HashSet<LaserInteractable> _pathBlockers = new();
        private readonly HashSet<(LaserInteractable, LaserInteractable)> _pathConflictBlockers = new();

        private LaserPathGenerator _laserPathGenerator;

        public Dictionary<Generator, List<List<LaserSegment>>> _segmentsbyGenerator = new();
        
        private void Awake()
        {
            _generators = FindObjectsOfType<Generator>();
            _receivers = FindObjectsOfType<Receiver>();
            _connectors = FindObjectsOfType<Connector>();

            foreach (var connector in _generators)
            {
                _segmentsbyGenerator.Add(connector, new List<List<LaserSegment>>());
            }

            _laserPathGenerator = new LaserPathGenerator(obstacleMask);
        }

        private void OnEnable()
        {
            foreach (var connector in _connectors)
            {
                connector.OnPickUp += OnPickUp;
                connector.OnDrop += OnPickUp;
            }
        }

        private void OnDisable()
        {
            foreach (var connector in _connectors)
            {
                connector.OnPickUp -= OnPickUp;
                connector.OnDrop -= OnPickUp;
            }
        }

        private void ResetAll()
        {
            foreach (var laser in _previousFrameInteractables)
            {
                if (!_currentFrameInteractables.Contains(laser))
                {
                    laser.Reset();
                }
            }

            _previousFrameInteractables.Clear();
            foreach (var laser in _currentFrameInteractables)
            {
                _previousFrameInteractables.Add(laser);
            }
            
            foreach (var kvp in _segmentsbyGenerator)
            {
                foreach (var list in kvp.Value)
                {
                    list.Clear();
                }
                
                kvp.Value.Clear();
            }
            
            laserVFXController.Clear();
            _currentFrameInteractables.Clear();
        }

        private void Update()
        {
            UpdateGeneratorLasers();
        }

        private void OnPickUp(Connector connector)
        {
            UpdateGeneratorLasers();
        }

        private void UpdateGeneratorLasers()
        {
            ResetAll();

            DetectColorConflictsBetweenGenerators();

            foreach (var generator in _generators)
            {
                var paths = _laserPathGenerator.FindAllPathSegments(generator);
                _segmentsbyGenerator[generator] = paths;
            }

            var allSegments = new List<(Generator, LaserSegment)>();
            foreach (var kvp in _segmentsbyGenerator)
            {
                var generator = kvp.Key;
                foreach (var path in kvp.Value)
                {
                    foreach (var segment in path)
                    {
                        allSegments.Add((generator, segment));
                    }
                }
            }

            var blockedSegmentInfos = MathematicsHelper.GetLaserSegmentBlockers(allSegments);
            CheckPhysicalBlockers(allSegments, blockedSegmentInfos);
            
            foreach (var (generator, paths) in _segmentsbyGenerator)
            {
                foreach (var segments in paths)
                {
                    foreach (var segment in segments)
                    {
                        var from = segment.From;
                        var to = segment.To;

                        if (blockedSegmentInfos.TryGetValue(segment, out var blockInfo))
                        {
                            laserVFXController.DisplayLaserEffect(generator.LaserColor, segment.From.LaserPoint, blockInfo.CollisionPoint);
                            _currentFrameInteractables.Add(segment.From);
                            break;
                        }
                        
                        if (_pathBlockers.Contains(segment.To))
                        {
                            laserVFXController.DisplayLaserEffect(generator.LaserColor, segment.From.LaserPoint, segment.To.LaserPoint);
                            _currentFrameInteractables.Add(segment.From);
                            break;
                        }
                        
                        if (_pathConflictBlockers.Contains((segment.From, segment.To)) || _pathConflictBlockers.Contains((segment.To, segment.From)))
                        {
                            laserVFXController.DisplayConflictLaserEffect(generator.LaserColor, segment.From, segment.To);
                            _currentFrameInteractables.Add(segment.From);
                            break;
                        }
                        

                        ConnectLaser(to, generator.LaserColor);
                        laserVFXController.DisplayLaserEffectConnection(generator.LaserColor, from.LaserPoint, to.LaserPoint);
                    }
                }
            }

            foreach (var info in blockedSegmentInfos.Values)
            {
                laserVFXController.DisplayHitMark(info.CollisionPoint);
            }
            
            foreach (var info in _pathBlockers)
            {
                laserVFXController.DisplayHitMark(info.LaserPoint);
            }
        }

        private void ConnectLaser(LaserInteractable target, ColorType currentColor)
        {
            target.AddInputColor(currentColor);
            _currentFrameInteractables.Add(target);
        }

        private void CheckPhysicalBlockers(List<(Generator, LaserSegment)> allSegments, Dictionary<LaserSegment, BlockInfo> blockedSegmentInfos)
        {
            foreach (var (_, segment) in allSegments)
            {
                var from = segment.From.LaserPoint;
                var to = segment.To.LaserPoint;
                var direction = (to - from).normalized;
                var distance = Vector3.Distance(from, to);

                if (!Physics.Raycast(from, direction, out var hit, distance, obstacleMask))
                {
                    continue;
                }
                
                if (blockedSegmentInfos.TryGetValue(segment, out var blockInfo))
                {
                    var distanceToIntersection = Vector3.Distance(from, blockInfo.CollisionPoint);
                    var distanceToHit = Vector3.Distance(from, hit.point);
                    
                    if (distanceToHit > distanceToIntersection + 0.01f)
                    {
                        continue;
                    }
                    else
                    {
                        blockedSegmentInfos.Remove(blockInfo.ConflictingSegment);
                        blockedSegmentInfos.Remove(blockInfo.OtherConflictingSegment);
                    }
                }

                //TODO Add other struct
                blockedSegmentInfos[segment] = new BlockInfo(hit.point, null, null);
            }
        }

        private void DetectColorConflictsBetweenGenerators()
        {
            _pathBlockers.Clear();
            _pathConflictBlockers.Clear();

            for (var i = 0; i < _generators.Length; i++)
            {
                for (var j = i + 1; j < _generators.Length; j++)
                {
                    var generatorA = _generators[i];
                    var generatorB = _generators[j];

                    if (generatorA.LaserColor == generatorB.LaserColor)
                        continue;

                    var paths = _laserPathGenerator.FindAllPathsBetweenGenerators(generatorA, generatorB);
                    foreach (var path in paths)
                    {
                        var connectors = path.OfType<Connector>().ToList();
                        if (connectors.Count == 0)
                        {
                            continue;
                        }

                        if (connectors.Count % 2 == 1)
                        {
                            var mid = connectors[connectors.Count / 2];
                            _pathBlockers.Add(mid);
                        }
                        else
                        {
                            var mid1 = connectors[(connectors.Count / 2) - 1];
                            var mid2 = connectors[connectors.Count / 2];
                            _pathConflictBlockers.Add((mid1, mid2));
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        private bool DebugCheckPathIteration(int index)
        {
            if (onlyPathTargetDebug > 0 && index != onlyPathTargetDebug - 1)
                return true;

            if (maxPathCountDebug > 0 && index > maxPathCountDebug - 1)
                return true;

            return false;
        }
#endif
    }
}
