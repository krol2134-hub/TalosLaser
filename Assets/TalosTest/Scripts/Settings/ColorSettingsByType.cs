using System;
using TalosTest.Interactables;
using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Settings
{
    [Serializable]
    public struct ColorSettingsByType
    {
        [SerializeField] private ColorType colorType;
        [SerializeField] private Color laserSphereMainColor;
        [SerializeField] private Color laserSphereAdditionalColor;

        public ColorType ColorType => colorType;
        public Color LaserSphereMainColor => laserSphereMainColor;
        public Color LaserSphereAdditionalColor => laserSphereAdditionalColor;
    }
}