using UnityEngine;

namespace TalosTest.Tool
{
    public interface IConnectionPoint
    {
        public string GetSelectText();
        public Transform GetPoint();
    }
}