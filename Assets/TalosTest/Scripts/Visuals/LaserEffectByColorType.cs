using System;
using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Visuals
{
    [Serializable]
    public struct LaserEffectByColorType
    {
        [SerializeField] private ColorType colorType;
        [SerializeField] private LaserEffect laserEffectPrefab;

        public readonly ColorType ColorType => colorType;
        public readonly LaserEffect LaserEffectPrefab => laserEffectPrefab;

        public LaserEffectByColorType(ColorType newColorType, LaserEffect newLaserEffectPrefab)
        {
            colorType = newColorType;
            laserEffectPrefab = newLaserEffectPrefab;
        }
    }
}