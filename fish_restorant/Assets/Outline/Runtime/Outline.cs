using UnityEngine;

//  OutlineFx © NullTale - https://x.com/NullTale/
namespace OutlineFx
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public abstract class Outline : MonoBehaviour
    {
        internal Renderer _renderer;

        // cached child renderers (used when this GameObject has no Renderer)
        private Renderer[] _childRenderers;

        public abstract Color Color { get; set; }

        // =======================================================================
        private void OnEnable()
        {
            _CacheRenderers();
        }

        private void OnDisable()
        {
            _childRenderers = null;
        }

        /// <summary>
        /// Call this if the child hierarchy changes at runtime.
        /// </summary>
        public void RefreshRenderers()
        {
            _CacheRenderers();
        }

        private void _CacheRenderers()
        {
            _renderer = GetComponent<Renderer>();

            if (_renderer == null)
                _childRenderers = GetComponentsInChildren<Renderer>(false);
            else
                _childRenderers = null;
        }

        // LateUpdate runs every frame regardless of whether there is a Renderer,
        // so it works on empty GameObjects, Blender empties, etc.
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isEditor)
                _CacheRenderers();
#endif

            // original path: renderer is on the same GameObject
            if (_renderer != null)
            {
                OutlineFxFeature.Render(this);
                return;
            }

            // no renderer on this object -> register all child renderers
            if (_childRenderers == null || _childRenderers.Length == 0)
                return;

            var color = Color;
            for (int i = 0; i < _childRenderers.Length; i++)
            {
                var r = _childRenderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                // skip children that have their own Outline component
                if (r.TryGetComponent<Outline>(out _))
                    continue;

                OutlineFxFeature.RenderEntry(r, color);
            }
        }
    }
}