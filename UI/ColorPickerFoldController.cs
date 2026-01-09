using UnityEngine;
using UnityEngine.UI;

namespace HSVPicker
{
    [RequireComponent(typeof(ColorPicker))]
    public class ColorPickerFoldController : MonoBehaviour
    {
        public ColorPicker picker;

        [Header("Fold UI")]
        [Tooltip("クリック対象となる色見本(Image)。このオブジェクトに付ける場合は未設定でOK。")]
        [SerializeField]
        private Image _button;

        [Tooltip("このRectTransform内を" + "\"内側\"" + "とみなして外側クリックで閉じる。未設定ならこのGameObjectのRectTransformを使用")]
        [SerializeField]
        private RectTransform _pickerRoot;

        [Tooltip("Screen Space - Camera / World Space の場合のUIカメラ。未設定なら親CanvasのworldCameraを使用")]
        [SerializeField]
        private Camera _uiCamera;

        [SerializeField]
        private bool _startOpen = true;

        [SerializeField]
        private bool _closeOnOutsideClick = true;

        [Tooltip("折りたたみ対象のオブジェクト群。閉じると全て非表示、開くと全て表示になります")]
        [SerializeField]
        private GameObject[] _foldTargets;

        private bool _isOpen = true;

        private Canvas _parentCanvas;

        void OnValidate()
        {
            picker = GetComponent<ColorPicker>();

            if (_pickerRoot == null)
            {
                _pickerRoot = transform as RectTransform;
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
            if (IsPointerOnSwatch(screenPos))
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