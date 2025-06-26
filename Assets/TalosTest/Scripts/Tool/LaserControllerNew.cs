using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                            laserVFXController.DisplayLaserEffectConnection(generator.LaserColor, segment.From.LaserPoint, blockInfo.CollisionPoint);
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
        }

        private void ConnectLaser(LaserInteractable target, ColorType currentColor)
        {
            target.AddInputColor(currentColor);
            _currentFrameInteractables.Add(target);
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
