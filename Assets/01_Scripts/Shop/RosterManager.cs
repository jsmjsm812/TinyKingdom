using System;
using System.Collections.Generic;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.Shop
{
    /// <summary>소환 명부: 배치 대기 중인 수호자 목록. GDD.md 9번: 최대 8기, 스크롤.</summary>
    public class RosterManager : MonoBehaviour
    {
        private const int Capacity = 8;

        public IReadOnlyList<RosterEntry> Entries => _entries;
        private readonly List<RosterEntry> _entries = new List<RosterEntry>();

        public bool HasSpace => _entries.Count < Capacity;

        public event Action OnChanged;

        public RosterEntry Add(GuardianData data, int starLevel = 1)
        {
            var entry = new RosterEntry(data, starLevel);
            _entries.Add(entry);
            OnChanged?.Invoke();
            return entry;
        }

        public void Remove(RosterEntry entry)
        {
            if (_entries.Remove(entry))
                OnChanged?.Invoke();
        }
    }
}
