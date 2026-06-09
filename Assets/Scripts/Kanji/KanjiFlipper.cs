using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace KanjiFlipGame.Kanji
{
    /// <summary>
    /// フリップ全体を管理するクラス（ローカル管理版）
    /// 出題者が組み立て、完了時にデータを送信します
    /// </summary>
    public class KanjiFlipper : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private int _maxKanjiCount = 8;
        [SerializeField] private GameObject _kanjiElementPrefab;
        [SerializeField] private RectTransform _flipCanvas;
        [SerializeField] private TMP_FontAsset _kanjiFontAsset; // NotoSansJP_Full SDF を設定

        private List<KanjiElement> _kanjiElements = new List<KanjiElement>();

        public KanjiElement AddKanji(string kanji)
        {
            if (_kanjiElements.Count >= _maxKanjiCount)
            {
                Debug.LogWarning("これ以上漢字を追加できません（最大" + _maxKanjiCount + "文字）");
                return null;
            }

            if (_kanjiElementPrefab == null)
            {
                Debug.LogError("_kanjiElementPrefabが設定されていません！");
                return null;
            }

            // ローカルで生成
            GameObject kanjiObj = Instantiate(_kanjiElementPrefab, _flipCanvas);
            KanjiElement kanjiElement = kanjiObj.GetComponent<KanjiElement>();

            if (kanjiElement != null)
            {
                // フォントを先に設定してから文字をセット（順序重要）
                if (_kanjiFontAsset != null) kanjiElement.SetFont(_kanjiFontAsset);
                kanjiElement.SetKanji(kanji);

                // 中央付近にランダム配置
                float randomX = Random.Range(-200f, 200f);
                float randomY = Random.Range(-150f, 150f);
                kanjiElement.Position = new Vector2(randomX, randomY);

                _kanjiElements.Add(kanjiElement);
                Debug.Log("漢字「" + kanji + "」をローカルで追加しました");
            }


            return kanjiElement;
        }

        public void RemoveKanji(KanjiElement element)
        {
            if (_kanjiElements.Contains(element))
            {
                _kanjiElements.Remove(element);
                Destroy(element.gameObject);
                Debug.Log("漢字を削除しました");
            }
        }

        public void ClearAll()
        {
            foreach (var element in _kanjiElements)
            {
                if (element != null) Destroy(element.gameObject);
            }
            _kanjiElements.Clear();
            Debug.Log("フリップをクリアしました");
        }

        public bool CanAddKanji() => _kanjiElements.Count < _maxKanjiCount;
        public int CurrentKanjiCount => _kanjiElements.Count;
        public int MaxKanjiCount => _maxKanjiCount;
        public List<KanjiElement> GetAllKanjiElements() => new List<KanjiElement>(_kanjiElements);

        public void DeselectAll()
        {
            foreach (var element in _kanjiElements)
            {
                if (element != null) element.Deselect();
            }
        }

        public void DeselectAllExcept(KanjiElement except)
        {
            foreach (var element in _kanjiElements)
            {
                if (element != null && element != except)
                {
                    element.Deselect();
                }
            }
        }

        public FlipData GetFlipData()
        {
            FlipData data = new FlipData();
            data.kanjiDataList = new List<KanjiData>();
            foreach (var element in _kanjiElements)
            {
                if (element != null)
                {
                    data.kanjiDataList.Add(new KanjiData {
                        kanji = element.GetKanji(),
                        positionX = element.Position.x,
                        positionY = element.Position.y,
                        rotation = element.Rotation,
                        fontSize = element.FontSize
                    });
                }
            }
            return data;
        }

        public void LoadFlipData(FlipData data)
        {
            ClearAll();
            if (data == null || data.kanjiDataList == null) return;
            foreach (var kanjiData in data.kanjiDataList)
            {
                KanjiElement element = AddKanji(kanjiData.kanji);
                if (element != null)
                {
                    element.Position = new Vector2(kanjiData.positionX, kanjiData.positionY);
                    element.Rotation = kanjiData.rotation;
                    element.FontSize = kanjiData.fontSize;
                }
            }
        }
    }

    [System.Serializable]
    public class FlipData { public List<KanjiData> kanjiDataList; }

    [System.Serializable]
    public class KanjiData
    {
        public string kanji;
        public float positionX;
        public float positionY;
        public float rotation;
        public float fontSize;
    }
}
