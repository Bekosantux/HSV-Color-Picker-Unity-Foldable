using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace HSVPicker
{
    [RequireComponent(typeof(ColorPicker))]
    public class ColorPickerFoldController : MonoBehaviour
    {
        public ColorPicker picker;

        [Header("Fold UI")]
        [Tooltip("クリック対象となる色見本(Image)。未設定ならこのオブジェクトのImageを使用")]
        [SerializeField]
        private Image _button;

        [Tooltip("このRectTransform外をクリックしたら閉じる。未設定ならこのオブジェクトのRectTransformを使用")]
        [SerializeField]
        private RectTransform _pickerRoot;

        [Tooltip("Screen Space - Camera / World Space の場合のUIカメラ。未設定なら親CanvasのworldCameraを使用")]
        [SerializeField]
        private Camera _uiCamera;

        [SerializeField]
        private bool _startOpen = true;

        [SerializeField]
        private bool _closeOnOutsideClick = true;

        [Tooltip("折りたたみ対象のオブジェクト群")]
        [SerializeField]
        private GameObject[] _foldTargets;

        [Header("Topmost (Optional)")]
        [Tooltip("展開時に、このRectTransformを最前面描画にする。未設定なら_pickerRootを使う")]
        [SerializeField]
        private RectTransform _topmostRoot;

        [Tooltip("展開中だけ Canvas.overrideSorting を有効にして、sortingOrder を上げて最前面表示する")]
        [SerializeField]
        private bool _overrideSortingWhileOpen = true;

        [Tooltip("展開中の sortingOrder。前面にしたいCanvasより大きい値にする")]
        [SerializeField]
        private int _sortingOrderWhileOpen = 100;

        private bool _isOpen = true;

        private Canvas _parentCanvas;
        private Canvas _topmostCanvas;
        private bool _savedOverrideSorting;
        private int _savedSortingOrder;

        void OnValidate()
        {
            picker = GetComponent<ColorPicker>();

            if (_pickerRoot == null)
            {
                _pickerRoot = transform as RectTransform;
            }

            if (_topmostRoot == null)
            {
                _topmostRoot = _pickerRoot;
            }

            if (_startOpen){Open();}
            else {Close();}
        }

        private void Awake()
        {
            if (picker == null)
            {
                picker = GetComponent<ColorPicker>();
            }

            if (_pickerRoot == null)
            {
                _pickerRoot = transform as RectTransform;
            }

            if (_topmostRoot == null)
            {
                _topmostRoot = _pickerRoot;
            }

            _parentCanvas = GetComponentInParent<Canvas>();
            _isOpen = _startOpen;
        }

        private void Start()
        {
            UpdateFolderContent();
        }

        private void Update()
        {
            if (!TryGetPointerDownPosition(out Vector2 screenPos)) return;

            // 1) 色見本クリックでトグル（開く/閉じる）
            if (IsTopmostSwatchClick(screenPos))
            {
                Toggle();
                return;
            }

            // 2) 外側クリックで閉じる
            if (_closeOnOutsideClick && _isOpen && !IsPointerInside(screenPos))
            {
                Close();
            }
        }

        private void UpdateFolderContent()
        {
            bool show = _isOpen;
            if (_foldTargets == null) return;

            for (int i = 0; i < _foldTargets.Length; i++)
            {
                GameObject target = _foldTargets[i];
                if (target == null) continue;
                target.SetActive(show);
            }

            if (show)
            {
                ApplyTopmostIfNeeded();
            }
            else
            {
                RestoreTopmostIfNeeded();
            }
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            UpdateFolderContent();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            UpdateFolderContent();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            UpdateFolderContent();
        }

        private bool IsPointerInside(Vector2 screenPos)
        {
            if (_pickerRoot == null) return true;
            Camera cam = GetUiCamera();
            return RectTransformUtility.RectangleContainsScreenPoint(_pickerRoot, screenPos, cam);
        }

        private bool IsPointerOnSwatch(Vector2 screenPos)
        {
            if (_button == null) return false;
            RectTransform swatch = _button.rectTransform;
            if (swatch == null) return false;
            Camera cam = GetUiCamera();
            return RectTransformUtility.RectangleContainsScreenPoint(swatch, screenPos, cam);
        }

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(16);

        /// <summary>
        /// Raycast結果で最前面ヒットが自分の色見本の場合のみトグル
        /// </summary>
        private bool IsTopmostSwatchClick(Vector2 screenPos)
        {
            if (!IsPointerOnSwatch(screenPos)) return false;

            // EventSystemが無い構成ではフォールバック
            EventSystem es = EventSystem.current;
            if (es == null) return true;

            _raycastResults.Clear();
            var ped = new PointerEventData(es) { position = screenPos };
            es.RaycastAll(ped, _raycastResults);
            if (_raycastResults.Count == 0) return true;

            // 先頭が最前面のヒット（通常はソート済み）
            GameObject top = null;
            for (int i = 0; i < _raycastResults.Count; i++)
            {
                if (_raycastResults[i].gameObject == null) continue;
                top = _raycastResults[i].gameObject;
                break;
            }
            if (top == null) return true;
            if (_button == null) return false;

            // 自分の色見本（またはその子）にヒットしている場合のみ反応
            return top.transform == _button.transform || top.transform.IsChildOf(_button.transform);
        }

        private Camera GetUiCamera()
        {
            if (_uiCamera != null) return _uiCamera;

            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>();
            }

            if (_parentCanvas == null) return null;

            // ScreenSpaceOverlay は null でOK
            return _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _parentCanvas.worldCamera;
        }

        private void ApplyTopmostIfNeeded()
        {
            if (_topmostRoot == null) return;

            if (!_overrideSortingWhileOpen) return;

            if (_topmostCanvas == null)
            {
                _topmostCanvas = _topmostRoot.GetComponent<Canvas>();
                if (_topmostCanvas == null)
                {
                    _topmostCanvas = _topmostRoot.gameObject.AddComponent<Canvas>();
                }

                _savedOverrideSorting = _topmostCanvas.overrideSorting;
                _savedSortingOrder = _topmostCanvas.sortingOrder;
            }

            _topmostCanvas.overrideSorting = true;
            _topmostCanvas.sortingOrder = _sortingOrderWhileOpen;

            // Raycasterが無いとクリックが抜けるケースがあるため、必要なら追加
            if (_topmostRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                _topmostRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        private void RestoreTopmostIfNeeded()
        {
            if (_topmostCanvas == null) return;
            if (!_overrideSortingWhileOpen) return;

            _topmostCanvas.overrideSorting = _savedOverrideSorting;
            _topmostCanvas.sortingOrder = _savedSortingOrder;
        }

        private static bool TryGetPointerDownPosition(out Vector2 screenPos)
        {
            // Mouse
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }

            // Touch
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    screenPos = t.position;
                    return true;
                }
            }

            screenPos = default;
            return false;
        }
    }
}