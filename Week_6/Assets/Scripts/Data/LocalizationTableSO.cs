using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "BusJam/Data/Localization Table")]
public class LocalizationTableSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        [TextArea(1, 3)] public string vi;
        [TextArea(1, 3)] public string en;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, Entry> _lookup;

    public IReadOnlyList<Entry> Entries => entries;

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public void RebuildLookup()
    {
        if (_lookup == null) _lookup = new Dictionary<string, Entry>(StringComparer.Ordinal);
        else _lookup.Clear();

        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry != null && !string.IsNullOrEmpty(entry.key) && !_lookup.ContainsKey(entry.key))
            {
                _lookup.Add(entry.key, entry);
            }
        }
    }

    public string Get(string key, bool vietnamese)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (_lookup == null || _lookup.Count != entries.Count) RebuildLookup();

        if (_lookup != null && _lookup.TryGetValue(key, out Entry entry))
        {
            string val = vietnamese ? entry.vi : entry.en;
            return string.IsNullOrEmpty(val) ? (vietnamese ? entry.en : entry.vi) : val;
        }
        return key;
    }

    public string Get(string key)
    {
        return Get(key, SettingsController.IsVietnameseSaved);
    }

    public string Format(string key, bool vietnamese, params object[] args)
    {
        string template = Get(key, vietnamese);
        if (args == null || args.Length == 0) return template;
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string Format(string key, params object[] args)
    {
        return Format(key, SettingsController.IsVietnameseSaved, args);
    }
}
