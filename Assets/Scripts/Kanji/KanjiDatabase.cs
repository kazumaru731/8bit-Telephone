using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace KanjiFlipGame.Kanji
{
    /// <summary>
    /// 小学1、2年生で習う漢字のデータベース
    /// JSONから詳細データ（読み、画数等）を読み込み、検索・検証に使用します
    /// </summary>
    public class KanjiDatabase : MonoBehaviour
    {
        [Serializable]
        public class KanjiInfo
        {
            public string @char;
            public string[] readings;
            public int strokeCount;
            public int grade;
        }

        [Serializable]
        private class KanjiDictionaryData
        {
            public List<KanjiInfo> kanjiList;
        }

        private static KanjiDatabase _instance;
        public static KanjiDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<KanjiDatabase>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("KanjiDatabase");
                        _instance = go.AddComponent<KanjiDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private List<KanjiInfo> _kanjiDictionary = new List<KanjiInfo>();
        private HashSet<char> _allAllowedKanjiSet = new HashSet<char>();
        private bool _isInitialized = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            TextAsset jsonAsset = Resources.Load<TextAsset>("KanjiData/KanjiDictionary");
            if (jsonAsset != null)
            {
                try
                {
                    KanjiDictionaryData data = JsonUtility.FromJson<KanjiDictionaryData>(jsonAsset.text);
                    _kanjiDictionary = data.kanjiList;
                    foreach (var info in _kanjiDictionary)
                    {
                        if (!string.IsNullOrEmpty(info.@char))
                        {
                            _allAllowedKanjiSet.Add(info.@char[0]);
                        }
                    }
                    Debug.Log("KanjiDatabase initialized: " + _kanjiDictionary.Count + " kanji");
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse kanji data: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("KanjiDictionary.json not found in Resources/KanjiData/");
            }

            _isInitialized = true;
        }

        public bool IsAllowedKanji(char character)
        {
            if (!_isInitialized) Initialize();
            return _allAllowedKanjiSet.Contains(character);
        }

        public bool AreAllKanjiAllowed(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                if (!IsAllowedKanji(c)) return false;
            }
            return true;
        }

        public List<KanjiInfo> SearchKanji(string reading, bool sortByStrokeCount = false)
        {
            if (!_isInitialized) Initialize();

            List<KanjiInfo> results;
            if (string.IsNullOrEmpty(reading))
            {
                results = _kanjiDictionary.ToList();
                sortByStrokeCount = true; // 空文字のときは強制的に画数順にする
            }
            else
            {
                // 読み（ひらがな/カタカナ）の部分一致検索
                results = _kanjiDictionary.Where(k => 
                    k.readings.Any(r => r.Contains(reading))
                ).ToList();
            }

            if (sortByStrokeCount)
            {
                results = results.OrderBy(k => k.strokeCount).ThenBy(k => k.@char).ToList();
            }
            else
            {
                // デフォルトは読みの昇順（最初の読みで比較）
                results = results.OrderBy(k => k.readings.FirstOrDefault() ?? "").ToList();
            }

            return results;
        }

        public List<char> GetAllAllowedKanji()
        {
            if (!_isInitialized) Initialize();
            return _allAllowedKanjiSet.ToList();
        }

        public List<KanjiInfo> GetKanjiByGrade(int grade)
        {
            if (!_isInitialized) Initialize();
            return _kanjiDictionary.Where(k => k.grade == grade).ToList();
        }
    }
}
