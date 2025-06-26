using UnityEngine;

namespace TalosTest.VFX
{
    public class LaserSphereVFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem shockwaveParticle;
        [SerializeField] private ParticleSystem darknessParticle;

        public void UpdateColor(Color mainColor, Color additionalColor)
        {
            var shockwaveParticleMain = shockwaveParticle.main;
            var darknessMain = darknessParticle.main;
            
            shockwaveParticleMain.startColor = mainColor;
            darknessMain.startColor = additionalColor;
        }
    }
}
