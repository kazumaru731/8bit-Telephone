using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace KanjiFlipGame.Kanji
{
    /// <summary>
    /// モバイル操作に最適化された漢字要素コンポーネント
    /// シングルタッチ: 移動 / ダブルタップ: 選択切替 / ピンチ: 拡大縮小 / ツイスト: 回転
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class KanjiElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI参照")]
        [SerializeField] private TextMeshProUGUI _kanjiText;
        [SerializeField] private GameObject _editControlPanel;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _rotateClockwiseButton;
        [SerializeField] private Button _rotateCounterButton;

        [Header("設定")]
        [SerializeField] private float _minFontSize = 30f;
        [SerializeField] private float _maxFontSize = 400f;
        [SerializeField] private float _rotationStep = 15f;
        [SerializeField] private TMP_FontAsset _kanjiFontAsset; // Inspectorで漢字対応フォントを設定

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private Vector2 _dragOffset;
        private bool _isSelected = false;
        private float _currentFontSize = 80f;
        private KanjiFlipper _flipper;

        // モバイルマルチタッチ用
        private float _initialDistance;
        private float _initialFontSize;
        private float _initialRotation;
        private float _initialTouchAngle;

        void Awake()
        {
            // Ensure the GameObject has a RectTransform for UI positioning.
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.sizeDelta = new Vector2(100, 100);
            }
            _parentCanvas = GetComponentInParent<Canvas>();
            _flipper = GetComponentInParent<KanjiFlipper>();

            // 名前で正確に KanjiText を取得（複数TMP存在時の誤取得を防ぐ）
            if (_kanjiText == null)
            {
                var kanjiTextObj = transform.Find("KanjiText");
                if (kanjiTextObj != null)
                    _kanjiText = kanjiTextObj.GetComponent<TextMeshProUGUI>();
            }
            // それでも見つからない場合はフォールバック
            if (_kanjiText == null)
                _kanjiText = GetComponentInChildren<TextMeshProUGUI>();

            if (_kanjiText != null)
                Debug.Log($"KanjiElement Awake: _kanjiText='{_kanjiText.gameObject.name}', font='{(_kanjiText.font != null ? _kanjiText.font.name : "null")}'");
            else
                Debug.LogWarning("KanjiElement Awake: _kanjiText が見つかりません！");

            EnsureMobileUI();
            UpdateFontSize();
        }


        private void EnsureMobileUI()
        {
            if (_editControlPanel == null)
            {
                var panelGo = new GameObject("MobileEditPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                panelGo.transform.SetParent(transform, false);
                _editControlPanel = panelGo;
                var rt = panelGo.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 100); // 指で隠れないよう少し高めに
                rt.sizeDelta = new Vector2(200, 60); // モバイル向けに大きめ
                
                var hlg = panelGo.GetComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = hlg.childControlHeight = true;
                hlg.padding = new RectOffset(10, 10, 10, 10);
                hlg.spacing = 15;

                _rotateCounterButton = CreateButton(panelGo.transform, "↺", Color.white);
                _rotateClockwiseButton = CreateButton(panelGo.transform, "↻", Color.white);
                _deleteButton = CreateButton(panelGo.transform, "×", new Color(1f, 0.4f, 0.4f));
            }

            if (_rotateCounterButton != null) _rotateCounterButton.onClick.AddListener(() => Rotate(-_rotationStep));
            if (_rotateClockwiseButton != null) _rotateClockwiseButton.onClick.AddListener(() => Rotate(_rotationStep));
            if (_deleteButton != null) _deleteButton.onClick.AddListener(DeleteSelf);
            
            _editControlPanel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string label, Color color)
        {
            var btnGo = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<Image>().color = color;
            
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 24;
            txt.color = Color.black;
            txt.alignment = TextAlignmentOptions.Center;
            
            var rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            
            return btnGo.GetComponent<Button>();
        }

        void Update()
        {
            HandleMultiTouch();
        }

        private void HandleMultiTouch()
        {
            if (!_isSelected || Input.touchCount < 2) return;

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // ピンチ・ツイスト開始時
            if (touch1.phase == TouchPhase.Began)
            {
                _initialDistance = Vector2.Distance(touch0.position, touch1.position);
                _initialFontSize = _currentFontSize;
                
                Vector2 direction = touch1.position - touch0.position;
                _initialTouchAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _initialRotation = _rectTransform.eulerAngles.z;
            }
            // 変形中
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                // ピンチによる拡大縮小
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                if (_initialDistance > 0)
                {
                    float scaleFactor = currentDistance / _initialDistance;
                    FontSize = _initialFontSize * scaleFactor;
                }

                // ツイストによる回転
                Vector2 direction = touch1.position - touch0.position;
                float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float angleDelta = currentAngle - _initialTouchAngle;
                _rectTransform.eulerAngles = new Vector3(0, 0, _initialRotation + angleDelta);
            }
        }

        public void SetKanji(string kanji)
        {
            if (_kanjiText != null)
            {
                _kanjiText.text = kanji;
                Debug.Log($"KanjiElement SetKanji: '{kanji}', font='{(_kanjiText.font != null ? _kanjiText.font.name : "null")}', color={_kanjiText.color}");
            }
            else
            {
                Debug.LogWarning($"KanjiElement SetKanji: _kanjiText が null です！漢字='{kanji}'");
            }
        }
        public string GetKanji() => _kanjiText != null ? _kanjiText.text : string.Empty;
        public void SetFont(TMP_FontAsset fontAsset) 
        { 
            if (_kanjiText != null && fontAsset != null) 
            {
                _kanjiText.font = fontAsset;
                // フォント変更後にテキストを再設定してTMPに再描画を促す
                var current = _kanjiText.text;
                _kanjiText.text = "";
                _kanjiText.text = current;
                Debug.Log("KanjiElement SetFont: フォントを設定しました: " + fontAsset.name);
            }
            else
            {
                Debug.LogWarning($"KanjiElement SetFont: _kanjiText={_kanjiText != null}, fontAsset={fontAsset != null}");
            }
        }

        #region UIイベント
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Input.touchCount > 1) return;
            _rectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out _dragOffset);
            _dragOffset = (Vector2)_rectTransform.localPosition - _dragOffset;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Input.touchCount > 1) return;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
                _rectTransform.localPosition = localPoint + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            // モバイルではタップで選択切替
            if (!eventData.dragging) ToggleSelection();
        }

        private void ToggleSelection()
        {
            _isSelected = !_isSelected;
            if (_isSelected && _flipper != null) _flipper.DeselectAllExcept(this);
            if (_editControlPanel != null) _editControlPanel.SetActive(_isSelected);
            
            // 選択フィードバック
            if (_kanjiText != null) _kanjiText.color = _isSelected ? Color.yellow : Color.black;
        }

        public void Deselect()
        {
            _isSelected = false;
            if (_editControlPanel != null) _editControlPanel.SetActive(false);
            if (_kanjiText != null) _kanjiText.color = Color.black;
        }

        private void Rotate(float angle) { RectTransform.Rotate(0, 0, angle); }
        private void DeleteSelf() { if (_flipper != null) _flipper.RemoveKanji(this); else Destroy(gameObject); }
        #endregion

        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        public Vector2 Position { get => RectTransform.anchoredPosition; set => RectTransform.anchoredPosition = value; }
        public float Rotation { get => RectTransform.eulerAngles.z; set => RectTransform.eulerAngles = new Vector3(0, 0, value); }
        public float FontSize { get => _currentFontSize; set { _currentFontSize = Mathf.Clamp(value, _minFontSize, _maxFontSize); UpdateFontSize(); } }

        private void UpdateFontSize() { if (_kanjiText != null) _kanjiText.fontSize = _currentFontSize; }
    }
}
