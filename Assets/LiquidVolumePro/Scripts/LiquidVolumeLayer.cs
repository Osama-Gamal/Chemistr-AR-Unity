using UnityEngine;

namespace LiquidVolumeFX {

    [ExecuteInEditMode, RequireComponent(typeof(LiquidVolume))]
    public class LiquidVolumeLayer : MonoBehaviour {

        public int layerNumber;
        public LiquidVolume.LiquidLayer layer;

        LiquidVolume lv;
        int currentLayer = -1;

        private void OnEnable() {
            lv = GetComponent<LiquidVolume>();
        }

        private void OnValidate() {
            if (lv != null && lv.liquidLayers != null && layerNumber < lv.liquidLayers.Length && currentLayer != layerNumber) {
                currentLayer = layerNumber;
                layer = lv.liquidLayers[layerNumber];
            }
        }

        void OnDidApplyAnimationProperties() {
            if (lv != null && layerNumber < lv.liquidLayers.Length) {
                lv.liquidLayers[layerNumber] = layer;
                lv.UpdateLayers(true);
            }
        }

    }
}