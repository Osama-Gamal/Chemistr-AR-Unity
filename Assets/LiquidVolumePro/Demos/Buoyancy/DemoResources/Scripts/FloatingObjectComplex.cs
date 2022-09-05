using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidVolumeFX {

    public class FloatingObjectComplex : MonoBehaviour {

        public LiquidVolume liquidVolume;

        void Update() {
            liquidVolume.MoveToLiquidSurface(transform, BuoyancyEffect.PositionAndRotation, transform.parent);
        }

    }
}
