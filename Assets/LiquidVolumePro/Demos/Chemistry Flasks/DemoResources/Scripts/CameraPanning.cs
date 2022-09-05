using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidVolumeFX {

    public class CameraPanning : MonoBehaviour {

        public float speed = 1f;

        void Update() {
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        }

    }
}
