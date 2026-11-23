using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Stage
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LayeredTerrainBlock : MonoBehaviour
    {
        private const string TopLayerObjectName = "TopLayer";
        private const string MiddleLayerObjectName = "MiddleLayer";
        private const float MinDimension = 0.01f;

        [SerializeField] private Sprite topSprite;
        [SerializeField, Min(0f)] private float topLayerHeight = 1f;
        [SerializeField] private Sprite middleSprite;
        [SerializeField, Min(0f)] private float middleLayerHeight = 1f;
        [SerializeField] private Sprite bottomSprite;
        [SerializeField] private Vector2 blockSize = Vector2.one;

        private SpriteRenderer baseRenderer;
        private BoxCollider2D boxCollider;
        private bool isApplyingRendererSizeInternally;
        private bool isApplyingColliderSizeInternally;

        private void Reset()
        {
            CacheComponents();
            ApplyBottomSprite();
            EnsureSizeInitialized(true);
            UpdateLayers();
        }

        private void OnEnable()
        {
            CacheComponents();
            EnsureSizeInitialized();
            UpdateLayers();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= DeferredValidate;
            EditorApplication.delayCall += DeferredValidate;
        }

        private void DeferredValidate()
        {
            if (this == null)
            {
                return;
            }

            CacheComponents();
            ApplyBottomSprite();
            EnsureSizeInitialized();
            UpdateLayers();
        }
#endif

        private void CacheComponents()
        {
            if (baseRenderer == null)
            {
                TryGetComponent(out baseRenderer);
            }

            if (boxCollider == null)
            {
                TryGetComponent(out boxCollider);
            }
        }

        private void ApplyBottomSprite()
        {
            if (baseRenderer != null && bottomSprite != null)
            {
                baseRenderer.sprite = bottomSprite;
            }
        }

        private void LateUpdate()
        {
            bool sizeChanged = TryConsumeTransformScale();
            sizeChanged |= TryApplyColliderSizeChange();

#if UNITY_EDITOR
            sizeChanged |= TryApplyRendererSizeChange();
#endif

            if (sizeChanged)
            {
                UpdateLayers();
            }
        }

        public void UpdateLayers()
        {
            if (baseRenderer == null || boxCollider == null)
            {
                return;
            }

            blockSize = SanitizeSize(blockSize);
            float width = blockSize.x;
            float totalHeight = blockSize.y;
            ApplyColliderSize(width, totalHeight);

            float offsetY = boxCollider.offset.y;

            ConfigureBaseRenderer(width, totalHeight);

            float appliedTopHeight = Mathf.Min(topLayerHeight, totalHeight);
            float remainingHeight = Mathf.Max(totalHeight - appliedTopHeight, 0f);
            float appliedMiddleHeight = Mathf.Min(middleLayerHeight, remainingHeight);

            ConfigureLayer(
                TopLayerObjectName,
                topSprite,
                width,
                appliedTopHeight,
                offsetY + (totalHeight * 0.5f) - (appliedTopHeight * 0.5f),
                2);

            ConfigureLayer(
                MiddleLayerObjectName,
                middleSprite,
                width,
                appliedMiddleHeight,
                offsetY + (totalHeight * 0.5f) - appliedTopHeight - (appliedMiddleHeight * 0.5f),
                1);
        }

        private void ConfigureBaseRenderer(float width, float height)
        {
            var rendererTransform = baseRenderer.transform;
            Vector3 originalLocalPosition = rendererTransform.localPosition;
            Quaternion originalLocalRotation = rendererTransform.localRotation;
            Vector3 originalLocalScale = rendererTransform.localScale;

            isApplyingRendererSizeInternally = true;
            baseRenderer.drawMode = SpriteDrawMode.Tiled;
            baseRenderer.size = new Vector2(width, height);
            isApplyingRendererSizeInternally = false;

            if (rendererTransform.localPosition != originalLocalPosition)
            {
                rendererTransform.localPosition = originalLocalPosition;
            }

            if (rendererTransform.localRotation != originalLocalRotation)
            {
                rendererTransform.localRotation = originalLocalRotation;
            }

            if (rendererTransform.localScale != originalLocalScale)
            {
                rendererTransform.localScale = originalLocalScale;
            }

            if (bottomSprite != null)
            {
                baseRenderer.sprite = bottomSprite;
            }
        }

        private void ConfigureLayer(string childName, Sprite sprite, float width, float height, float centerY, int sortingOrderDelta)
        {
            var renderer = EnsureChildRenderer(childName, sortingOrderDelta);

            if (sprite == null || height <= 0f)
            {
                renderer.gameObject.SetActive(false);
                return;
            }

            renderer.gameObject.SetActive(true);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sprite = sprite;
            renderer.size = new Vector2(Mathf.Max(width, MinDimension), Mathf.Max(height, MinDimension));
            renderer.transform.localPosition = new Vector3(0f, centerY, 0f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one;
        }

        private void ApplyColliderSize(float width, float height)
        {
            if (boxCollider == null)
            {
                return;
            }

            isApplyingColliderSizeInternally = true;
            boxCollider.size = new Vector2(width, height);
            isApplyingColliderSizeInternally = false;
        }

        private void EnsureSizeInitialized(bool overwriteWithCollider = false)
        {
            Vector2 targetSize = blockSize;

            if (overwriteWithCollider || !IsValidSize(blockSize))
            {
                if (boxCollider != null && IsValidSize(boxCollider.size))
                {
                    targetSize = boxCollider.size;
                }
                else if (baseRenderer != null && IsValidSize(baseRenderer.size))
                {
                    targetSize = baseRenderer.size;
                }
                else
                {
                    targetSize = Vector2.one;
                }
            }

            blockSize = SanitizeSize(targetSize);
        }

        private Vector2 SanitizeSize(Vector2 size)
        {
            float width = Mathf.Max(Mathf.Abs(size.x), MinDimension);
            float height = Mathf.Max(Mathf.Abs(size.y), MinDimension);
            return new Vector2(width, height);
        }

        private static bool IsValidSize(Vector2 size)
        {
            return size.x > 0f && size.y > 0f && !float.IsNaN(size.x) && !float.IsNaN(size.y);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
        }

        private bool TryConsumeTransformScale()
        {
            Vector3 localScale = transform.localScale;
            float scaleX = Mathf.Abs(localScale.x);
            float scaleY = Mathf.Abs(localScale.y);

            if (Mathf.Approximately(scaleX, 1f) && Mathf.Approximately(scaleY, 1f))
            {
                return false;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && EditorGUIUtility.editingTextField)
            {
                return false;
            }
#endif

            blockSize = SanitizeSize(blockSize);

            Vector2 newSize = new Vector2(
                blockSize.x * (Mathf.Approximately(scaleX, 0f) ? 1f : scaleX),
                blockSize.y * (Mathf.Approximately(scaleY, 0f) ? 1f : scaleY));
            newSize = SanitizeSize(newSize);

            ResetTransformScale(localScale);

            if (Approximately(newSize, blockSize))
            {
                return false;
            }

            blockSize = newSize;
            return true;
        }

        private bool TryApplyColliderSizeChange()
        {
            if (boxCollider == null || isApplyingColliderSizeInternally)
            {
                return false;
            }

            Vector2 colliderSize = SanitizeSize(boxCollider.size);

            if (Approximately(colliderSize, blockSize))
            {
                return false;
            }

            blockSize = colliderSize;
            return true;
        }

#if UNITY_EDITOR
        private bool TryApplyRendererSizeChange()
        {
            if (Application.isPlaying || baseRenderer == null || isApplyingRendererSizeInternally)
            {
                return false;
            }

            Vector2 rendererSize = SanitizeSize(baseRenderer.size);

            if (Approximately(rendererSize, blockSize))
            {
                return false;
            }

            blockSize = rendererSize;
            return true;
        }
#endif

        private void ResetTransformScale(Vector3 currentScale)
        {
            float signX = Mathf.Sign(currentScale.x);
            float signY = Mathf.Sign(currentScale.y);
            float signZ = Mathf.Sign(currentScale.z);

            transform.localScale = new Vector3(
                signX == 0f ? 1f : signX,
                signY == 0f ? 1f : signY,
                signZ == 0f ? 1f : signZ);
            transform.hasChanged = false;
        }

        private SpriteRenderer EnsureChildRenderer(string childName, int sortingOrderDelta)
        {
            Transform childTransform = transform.Find(childName);
            if (childTransform == null)
            {
                var child = new GameObject(childName)
                {
                    layer = gameObject.layer
                };
                childTransform = child.transform;
                childTransform.SetParent(transform, false);
            }

            var renderer = childTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = childTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sortingLayerID = baseRenderer.sortingLayerID;
            renderer.sortingOrder = baseRenderer.sortingOrder + sortingOrderDelta;
            renderer.color = baseRenderer.color;
            renderer.sharedMaterial = baseRenderer.sharedMaterial;

            return renderer;
        }
    }
}
