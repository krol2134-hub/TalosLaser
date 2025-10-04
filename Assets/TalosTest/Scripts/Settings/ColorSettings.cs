using TalosTest.Interactables;
using TalosTest.Tool;
using UnityEngine;

namespace TalosTest.Settings
{
    [CreateAssetMenu(fileName = "ColorSettings", menuName = "TalosTest/Settings/ColorSettings", order = 1)]
    public class ColorSettings : ScriptableObject
    {
        [SerializeField] private ColorSettingsByType[] colorSettingsByTypes;

        public ColorSettingsByType GetColorSettingsByType(ColorType colorType)
        {
            foreach (var colorSettings in colorSettingsByTypes)
            {
                if (colorSettings.ColorType == colorType)
                {
                    return colorSettings;
                }
            }

            return default;
        }
    }
}
